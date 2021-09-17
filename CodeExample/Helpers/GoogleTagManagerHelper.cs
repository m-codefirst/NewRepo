using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using Hephaestus.Commerce.Cart.Services;
using Hephaestus.Commerce.Helpers;
using Hephaestus.Commerce.Product.ProductService;
using Mediachase.Commerce;
using Newtonsoft.Json.Linq;
using TRM.Shared.Constants;
using TRM.Web.Extentions;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.Cart;

namespace TRM.Web.Helpers
{
    public class GoogleTagManagerHelper : IAmGoogleTagManagerHelper
    {
        private readonly IAmEntryHelper _entryHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IAmStoreHelper _storeHelper;
        private readonly ICartService _cartService;
        private readonly IAmReferenceConverter _referenceConverter;
        private readonly IAmOrderHelper _orderHelper;
        private readonly IAmContactHelper _contactHelper;
        private readonly IAmCartHelper _cartHelper;

        public GoogleTagManagerHelper(
            IAmEntryHelper entryHelper,
            IContentLoader contentLoader,
            IAmStoreHelper storeHelper,
            ICartService cartService,
            IAmReferenceConverter referenceConverter,
            IAmOrderHelper orderHelper,
            IAmContactHelper contactHelper, 
            IAmCartHelper cartHelper)
        {
            _entryHelper = entryHelper;
            _contentLoader = contentLoader;
            _storeHelper = storeHelper;
            _cartService = cartService;
            _referenceConverter = referenceConverter;
            _orderHelper = orderHelper;
            _contactHelper = contactHelper;
            _cartHelper = cartHelper;
        }

        public JObject GetEntryObject(ContentReference contentReference, decimal quantity = decimal.Zero)
        {
            var entryContent = _contentLoader.Get<EntryContentBase>(contentReference);
            if (entryContent == null) return null;
            var categoryContentReference = _entryHelper.GetCategoryContentReference(contentReference);
            var category = categoryContentReference != null ? _contentLoader.Get<TrmCategoryBase>(categoryContentReference) : null;
            var entryCategoryName = category != null ? (string.IsNullOrEmpty(category.DisplayName) ? category?.Name : category.DisplayName) : string.Empty;

            var entryPriceString = string.Empty;
            var entryCurrency = string.Empty;
            var merchandising = entryContent as IControlMyMerchandising;
            if (merchandising != null && merchandising.Sellable)
            {
                var discountPrice = _storeHelper.GetDiscountPrice(entryContent.ContentLink, 1);
                if (discountPrice != null)
                {
                    entryPriceString = _storeHelper.GetPriceAsString(discountPrice.Money.Amount, discountPrice.Money.Currency);
                    entryCurrency = discountPrice.Money.Currency;
                }
            }

            var brand = entryContent as IControlBrand;

            var json = JObject.FromObject(new
            {
                sku = entryContent.Code,
                name = _entryHelper.GetDisplayName(entryContent.ContentLink).StripHtmlTagsAndSpecialChars(),
                category = entryCategoryName,
                price = entryPriceString,
                currency = entryCurrency,
                brand = brand?.BrandDisplayName
            });

            if (quantity > decimal.Zero)
            {
                json["quantity"] = quantity;
            }

            return json;
        }

        public JObject GetLineItemObject(ILineItem lineItem, Currency currency)
        {
            var refConvLink = _referenceConverter.GetContentLink(lineItem.Code);
            if (ContentReference.IsNullOrEmpty(refConvLink)) return null;
            var entryContent = _contentLoader.Get<EntryContentBase>(refConvLink);
            if (entryContent == null) return null;
            var categoryContentReference = _entryHelper.GetCategoryContentReference(refConvLink);
            var category = categoryContentReference != null ? _contentLoader.Get<TrmCategoryBase>(categoryContentReference) : null;
            var entryCategoryName = category != null ? (string.IsNullOrEmpty(category.DisplayName) ? category?.Name : category.DisplayName) : string.Empty;

            var lineItemPrice = _cartHelper.GetLivePrice(lineItem, currency);
            var entryPriceString = _storeHelper.GetPriceAsString(lineItemPrice, currency);
            var brand = entryContent as IControlBrand;

            var json = JObject.FromObject(new
            {
                sku = entryContent.Code,
                name = lineItem.DisplayName,
                category = entryCategoryName,
                price = entryPriceString,
                currency = currency,
                brand = brand?.BrandDisplayName,
                quantity = lineItem.Quantity
            });

            return json;
        }

        public JObject GetCartObjectForGtm()
        {
            var defaultCart = _cartService.LoadCart(_cartService.DefaultCartName);
            var bullionCart = _cartService.LoadCart(DefaultCartNames.DefaultBullionCartName);

            var defaultCartHasItems = defaultCart != null && defaultCart.Forms != null && defaultCart.GetAllLineItems().Any();
            var bullionCartHasItems = bullionCart != null && bullionCart.Forms != null && bullionCart.GetAllLineItems().Any();

            //neither cart has loaded. try a buy now cart (there will only be one of these)
            if (!defaultCartHasItems && !bullionCartHasItems)
            {
                defaultCart = _cartService.LoadCart(DefaultCartNames.DefaultBuyNowCartName);
            }

            //both carts have loaded. this is a mixed checkout
            if (defaultCartHasItems && bullionCartHasItems)
            {
                return GetOrderGroupObjectForGtm(defaultCart, bullionCart);
            }

            //bullion only checkout
            if (!defaultCartHasItems && bullionCartHasItems)
            {
                return GetOrderGroupObjectForGtm(bullionCart);
            }

            return GetOrderGroupObjectForGtm(defaultCart);
        }

        public JObject GetPurchaseOrderForGtm(bool mixedcheckout = false)
        {
            var customer = _contactHelper.GetCheckoutCustomerContact();
            if (customer == null) return null;

            var pk = customer.PrimaryKeyId.GetValueOrDefault(); ;
            IPurchaseOrder po = null;
            var bullionCart = _cartService.LoadCart(DefaultCartNames.DefaultBullionCartName) ??
                              _cartService.LoadCart(DefaultCartNames.DefaultBuyNowCartName);
           
            if ((bullionCart == null || !bullionCart.GetAllLineItems().Any()) && !mixedcheckout)
            {
                po = _orderHelper.GetLastOrder(pk);
            }

            var retailPurchaseOrder = customer.DeserializeProperty<PurchaseOrderViewModel>(StringConstants.CustomFields.ConsumerPayment);

            var jObject = bullionCart == null || !bullionCart.GetAllLineItems().Any()
                ? GetOrderGroupObjectForGtm(po)
                : retailPurchaseOrder == null || po?.OrderNumber != retailPurchaseOrder.OriginalOrderNumber
                    ? GetOrderGroupObjectForGtm(bullionCart)
                    : GetOrderGroupObjectForGtm(po, bullionCart);

            if (jObject != null)
            {
                jObject["FirstName"] = customer.FirstName;
                jObject["LastName"] = customer.LastName;
                jObject["EmailAddress"] = customer.Email;
            }

            return jObject;
        }

        private JObject GetOrderGroupObjectForGtm(IOrderGroup defaultOrderGroup, IOrderGroup secondOrderGroup = null)
        {
            var orderForm = defaultOrderGroup?.GetFirstForm();
            if (orderForm == null) return null;

            var lineItems = orderForm.GetAllLineItems().ToList();
            if (!lineItems.Any()) return null;
            var lineItemsAsJson = lineItems.Select(x => GetLineItemObject(x, defaultOrderGroup.Currency));

            var cartSubTotal = defaultOrderGroup.GetSubTotal().Amount;
            var cartShipping = defaultOrderGroup.GetShippingSubTotal().Amount;
            var promotionCodes = string.Join(",", defaultOrderGroup.GetFirstForm()?.CouponCodes ?? new List<string>());
            var cartIds = defaultOrderGroup.OrderLink.OrderGroupId.ToString();
            var transactionIds = string.Empty;

            if (secondOrderGroup != null)
            {
                cartSubTotal += secondOrderGroup.GetSubTotal().Amount;
                cartShipping += secondOrderGroup.GetShippingSubTotal().Amount;
                var extraPromotionCodes = string.Join(",", defaultOrderGroup.GetFirstForm()?.CouponCodes ?? new List<string>());
                if (!string.IsNullOrWhiteSpace(extraPromotionCodes))
                {
                    promotionCodes = $"{promotionCodes},{extraPromotionCodes}";
                }

                lineItems = secondOrderGroup.GetAllLineItems().ToList();
                if (lineItems.Any())
                {
                    lineItemsAsJson = lineItemsAsJson.Concat(lineItems.Select(x => GetLineItemObject(x, defaultOrderGroup.Currency))).ToList();
                }

                cartIds = $"{cartIds}/{secondOrderGroup.OrderLink.OrderGroupId}";

                var secondPayment = secondOrderGroup.GetFirstForm()?.Payments?.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(secondPayment?.TransactionID))
                {
                    transactionIds = secondPayment.TransactionID;
                }
            }

            var json = JObject.FromObject(new
            {
                cartId = cartIds,
                cartSubTotal = _storeHelper.GetPriceAsString(cartSubTotal, defaultOrderGroup.GetSubTotal().Currency),
                cartShipping = _storeHelper.GetPriceAsString(cartShipping, defaultOrderGroup.GetSubTotal().Currency),
                visitorType = HttpContext.Current.Request.IsAuthenticated ? "Existing" : "New",
                promotionCode = promotionCodes,
                cartProducts = lineItemsAsJson
            });

            var payment = orderForm.Payments.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(payment?.TransactionID))
            {
                transactionIds = $"{payment.TransactionID},{transactionIds}";
            }

            json["transactionId"] = transactionIds;
            return json;
        }

        public JObject GetNodeObject(ContentReference contentReference)
        {
            var category = _contentLoader.Get<TrmCategoryBase>(contentReference);
            var entryCategoryName = string.IsNullOrEmpty(category?.DisplayName) ? category?.Name : category.DisplayName;

            return JObject.FromObject(new
            {
                category = entryCategoryName
            });
        }


        public void CheckForCommercePages(JObject dataLayer, ContentData contentData, ActionExecutingContext filterContext)
        {
            if (contentData is BasketPage || contentData is IAmACheckoutPage)
            {
                if (IsOrderConfirmationPage(filterContext.ActionDescriptor))
                {
                    if (filterContext.ActionDescriptor.ControllerDescriptor.ControllerName == "MixedCheckoutPage")
                    {
                        dataLayer["CartContents"] = GetPurchaseOrderForGtm(true);
                    }
                    else
                    {
                        dataLayer["CartContents"] = GetPurchaseOrderForGtm();
                    }
                    
                }
                else
                {
                    dataLayer["CartContents"] = GetCartObjectForGtm();
                }
            }
        }

        private bool IsOrderConfirmationPage(System.Web.Mvc.ActionDescriptor actionDescriptor)
        {
            return ((actionDescriptor.ControllerDescriptor.ControllerName == "CheckoutPage" &&
                     actionDescriptor.ActionName == "CheckoutStep5") ||
                    (actionDescriptor.ControllerDescriptor.ControllerName == "MixedCheckoutPage" &&
                     actionDescriptor.ActionName == "Step4") ||
                    (actionDescriptor.ControllerDescriptor.ControllerName == "BullionOnlyCheckoutPage" &&
                     actionDescriptor.ActionName == "OrderConfirmation") ||
                    (actionDescriptor.ControllerDescriptor.ControllerName == "BullionQuickCheckoutPage" &&
                     actionDescriptor.ActionName == "Confirmation"));
        }
    }
}