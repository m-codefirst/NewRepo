using EPiServer;
using EPiServer.Business.Commerce.Payment.Mastercard.Mastercard;
using EPiServer.Business.Commerce.Payment.Mastercard.Mastercard.Orders;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Marketing.Internal;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Storage;
using EPiServer.Core;
using EPiServer.Find.Helpers;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Shared.Services;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Extensions;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Exceptions;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using TRM.IntegrationServices.Interfaces;
using TRM.Shared.DataAccess;
using TRM.Shared.Extensions;
using TRM.Shared.Helpers;
using TRM.Shared.Interfaces;
using TRM.Shared.Models.DTOs;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Business.Cart;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Services;
using TRM.Web.Services.Coupons;
using TRM.Web.Services.Metapack.Extensions;
using TrmShippingProvider.Gateway;
using static TRM.Shared.Constants.StringConstants;
using TransactionType = Mediachase.Commerce.Orders.TransactionType;

namespace TRM.Web.Helpers
{
    public class CartHelper : IAmCartHelper
    {
        private readonly DateTime _safeBeginningOfTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly IAddressBookService _addressBookService;
        private readonly IAmAssetHelper _assetHelper;
        private readonly IAmCreditHelper _creditHelper;
        private readonly ICurrencyService _currencyService;
        private readonly ICurrentMarket _currentMarket;
        private readonly IAmEntryHelper _entryHelper;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly ILineItemCalculator _lineItemCalculator;
        private readonly LocalizationService _localizationService;
        private readonly IAmOrderGroupAuditHelper _orderGroupAuditHelper;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IAmOrderHelper _orderHelper;
        private readonly IOrderRepository _orderRepository;
        private readonly IAmPaymentHelper _paymentHelper;
        private readonly IPromotionService _promotionService;
        private readonly ITrmCartService _trmCartService;
        private readonly PromotionEngineContentLoader _promotionEngine;
        private readonly CustomerContext _customerContext;
        private readonly MiniProfiler _miniProfiler;
        private readonly IShippingCalculator _shippingCalculator;
        private readonly IStorageRateHelper _storageRateHelper;
        private readonly IUserService _userService;
        private readonly IPrintzwareHelper _printzwareHelper;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly IThirdPartyTransactionRepository _thirdPartyTransactionRepository;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IContentLoader _contentLoader;
        private readonly Lazy<IExportTransactionsRepository> _exportTransactionsRepository = new Lazy<IExportTransactionsRepository>(() => ServiceLocator.Current.GetInstance<IExportTransactionsRepository>());
        private readonly IAmAPaymentMethodHelper _paymentMethodHelper;
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly ICouponService _couponService;

        public CartHelper(
            ITrmCartService trmCartService,
            IAmAssetHelper assetHelper,
            ICurrencyService currencyService,
            IPromotionService promotionService,
            ILineItemCalculator lineItemCalculator,
            IOrderGroupCalculator orderGroupCalculator,
            ICurrentMarket currentMarket,
            IAmEntryHelper entryHelper,
            LocalizationService localizationService,
            IAmInventoryHelper inventoryHelper,
            IOrderRepository orderRepository,
            IAmPaymentHelper paymentHelper,
            IAmOrderHelper orderHelper,
            IAddressBookService addressBookService,
            IAmOrderGroupAuditHelper orderGroupAuditHelper,
            IAmCreditHelper creditHelper,
            IOrderGroupFactory orderGroupFactory,
            PromotionEngineContentLoader promotionEngine,
            CustomerContext customerContext,
            IShippingCalculator shippingCalculator,
            IStorageRateHelper storageRateHelper,
            IUserService userService,
            IPrintzwareHelper printzwareHelper,
            IBullionPriceHelper bullionPriceHelper,
            IThirdPartyTransactionRepository thirdPartyTransactionRepository,
            IAmBullionContactHelper bullionContactHelper,
            IContentLoader contentLoader,
            IAmAPaymentMethodHelper paymentMethodHelper,
            IPaymentProcessor paymentProcessor,
            ICouponService couponService)
        {
            _trmCartService = trmCartService;
            _assetHelper = assetHelper;
            _currencyService = currencyService;
            _promotionService = promotionService;
            _lineItemCalculator = lineItemCalculator;
            _orderGroupCalculator = orderGroupCalculator;
            _currentMarket = currentMarket;
            _entryHelper = entryHelper;
            _localizationService = localizationService;
            _inventoryHelper = inventoryHelper;
            _orderRepository = orderRepository;
            _paymentHelper = paymentHelper;
            _orderHelper = orderHelper;
            _addressBookService = addressBookService;
            _orderGroupAuditHelper = orderGroupAuditHelper;
            _creditHelper = creditHelper;
            _orderGroupFactory = orderGroupFactory;
            _promotionEngine = promotionEngine;
            _customerContext = customerContext;
            _miniProfiler = MiniProfiler.Current;
            _printzwareHelper = printzwareHelper;
            _shippingCalculator = shippingCalculator;
            _storageRateHelper = storageRateHelper;
            _userService = userService;
            _bullionPriceHelper = bullionPriceHelper;
            _thirdPartyTransactionRepository = thirdPartyTransactionRepository;
            _bullionContactHelper = bullionContactHelper;
            _contentLoader = contentLoader;
            _paymentMethodHelper = paymentMethodHelper;
            _paymentProcessor = paymentProcessor;
            _couponService = couponService;
        }

        public MixedOrderGroupViewModel GetMixedLargeCart()
        {
            var defaultCartViewModel = GetLargeCartViewModel(_trmCartService.DefaultCartName);
            var bullionCartViewModel = GetLargeCartViewModel(_trmCartService.DefaultBullionCartName);

            return GetMixedOrderGroupViewModel(defaultCartViewModel, bullionCartViewModel);
        }

        public MixedOrderGroupViewModel GetMixedMiniCart()
        {
            var defaultCartViewModel = GetMiniCartViewModel(_trmCartService.DefaultCartName);
            var bullionCartViewModel = GetMiniCartViewModel(_trmCartService.DefaultBullionCartName);

            return GetMixedOrderGroupViewModel(defaultCartViewModel, bullionCartViewModel);
        }

        private MixedOrderGroupViewModel GetMixedOrderGroupViewModel(IOrderGroupViewModel defaultCartViewModel, IOrderGroupViewModel bullionCartViewModel)
        {
            var shipments = new List<ShipmentViewModel>();
            shipments.AddRange(defaultCartViewModel.Shipments);

            if (bullionCartViewModel == null) return (MixedOrderGroupViewModel)defaultCartViewModel;

            if (bullionCartViewModel.Shipments.Count >= 1)
                bullionCartViewModel.Shipments[0].ShipmentUniqueId = "BullionCart-Delivered";
            if (bullionCartViewModel.Shipments.Count >= 2)
                bullionCartViewModel.Shipments[1].ShipmentUniqueId = "BullionCart-Vaulted";

            shipments.AddRange(bullionCartViewModel.Shipments);

            var promotions = new List<PromotionViewModel>();
            promotions.AddRange(defaultCartViewModel.Promotions);
            promotions.AddRange(bullionCartViewModel.Promotions);

            var validations = new List<CartItemValidationViewModel>();

            if (defaultCartViewModel is LargeOrderGroupViewModel && bullionCartViewModel is LargeOrderGroupViewModel)
            {
                validations.AddRange((defaultCartViewModel as LargeOrderGroupViewModel).Validation);
                validations.AddRange((bullionCartViewModel as LargeOrderGroupViewModel).Validation);
            }

            var vatAmountWithoutDeliveryFee = GetInvestmentVatWithoutDeliveryFee(bullionCartViewModel);
            var fullBasketTotalAmountWithoutDeliveryFee = defaultCartViewModel.SubTotalDecimal +
                                                          bullionCartViewModel.SubTotalDecimal +
                                                          vatAmountWithoutDeliveryFee -
                                                          defaultCartViewModel.TotalOrderDiscountDecimal -
                                                          bullionCartViewModel.TotalOrderDiscountDecimal;

            var fullSavedAmountWithoutDeliveryDecimal = defaultCartViewModel.SavedAmountDecimal + bullionCartViewModel.SavedAmountDecimal;

            var startPage = default(object).GetAppropriateStartPageForSiteSpecificProperties();

            var intervalUpdateTime = (startPage?.MetalPriceSettingsPage != null) ?
                _contentLoader.Get<MetalPriceSettingsPage>(startPage.MetalPriceSettingsPage).IntervalUpdateTime :
                startPage?.IntervalUpdateTime;

            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(defaultCartViewModel.Id)) ids.Add(defaultCartViewModel.Id);
            if (!string.IsNullOrWhiteSpace(bullionCartViewModel.Id)) ids.Add(bullionCartViewModel.Id);

            var promotionCodes = new List<string>();
            if (!string.IsNullOrWhiteSpace(defaultCartViewModel.PromotionCode)) promotionCodes.Add(defaultCartViewModel.PromotionCode);
            if (!string.IsNullOrWhiteSpace(bullionCartViewModel.PromotionCode)) promotionCodes.Add(bullionCartViewModel.PromotionCode);

            //Merge cart view model
            return new MixedOrderGroupViewModel
            {
                Shipments = shipments,
                Promotions = promotions.DistinctBy(x => x.Code).ToList(),
                Validation = validations,

                ItemCount = defaultCartViewModel.ItemCount + bullionCartViewModel.ItemCount,
                SkuCount = defaultCartViewModel.SkuCount + bullionCartViewModel.SkuCount,

                RetailSubTotal = defaultCartViewModel.SubTotal,
                RetailTotal = defaultCartViewModel.Total,
                RetailDeliveryTotal = defaultCartViewModel.TotalDelivery,
                RetailDeliveryTotalWithoutDiscount = defaultCartViewModel.TotalDeliveryWithoutDiscount,
                IsFreeRetailDelivery = defaultCartViewModel.IsFreeDelivery,
                RetailDiscount = defaultCartViewModel.TotalDiscount,
                ConsumerPromotions = defaultCartViewModel.Promotions,
                ConsumerSavedAmount = defaultCartViewModel.SavedAmount,

                InvestmentSubTotal = bullionCartViewModel.SubTotal,
                InvestmentTotal = bullionCartViewModel.Total,
                InvestmentVat = bullionCartViewModel.TaxTotal,
                InvestmentVatWithoutDeliveryFee = (new Money(vatAmountWithoutDeliveryFee, _currencyService.GetCurrentCurrency())).ToString(),
                TotalInvestmentDelivery = bullionCartViewModel.TotalDelivery,
                IsFreeInvestmentDelivery = bullionCartViewModel.IsFreeDelivery,
                BullionPromotions = bullionCartViewModel.Promotions,
                BullionSavedAmount = bullionCartViewModel.SavedAmount,

                FullBasketTotalWithoutDeliveryFeeVat = (new Money(fullBasketTotalAmountWithoutDeliveryFee, _currencyService.GetCurrentCurrency())).ToString(),
                FullBasketSubTotal = new Money(defaultCartViewModel.SubTotalDecimal + bullionCartViewModel.SubTotalDecimal, _currencyService.GetCurrentCurrency()).ToString(),
                FullBasketTotal = new Money(defaultCartViewModel.TotalDecimal + bullionCartViewModel.TotalDecimal, _currencyService.GetCurrentCurrency()).ToString(),
                FullSavedAmountWithoutDelivery = new Money(fullSavedAmountWithoutDeliveryDecimal, _currencyService.GetCurrentCurrency()).ToString(),
                FullSavedAmount = new Money(defaultCartViewModel.TotalDiscountDecimal + fullSavedAmountWithoutDeliveryDecimal, _currencyService.GetCurrentCurrency()).ToString(),

                HasBasketSavingsWithoutDelivery = fullSavedAmountWithoutDeliveryDecimal > 0,

                Modified = defaultCartViewModel.Modified > bullionCartViewModel.Modified ? defaultCartViewModel.Modified : bullionCartViewModel.Modified,

                RefreshTime = intervalUpdateTime.HasValue ? intervalUpdateTime.Value : 1,
                LastUpdated = DateTime.Now,
                Ids = ids,
                PromotionCodes = promotionCodes
            };
        }

        private decimal GetInvestmentVatWithoutDeliveryFee(IOrderGroupViewModel orderGroup)
        {
            return orderGroup.Shipments
                .SelectMany(x => x.CartItems)
                .Where(x => x.BullionCartItem?.VatAmount != null)
                .Sum(x => x.BullionCartItem.VatAmount.Value);
        }

        public LargeOrderGroupViewModel GetLargeCartViewModel(string cartName)
        {
            return GetLargeCartViewModel(_trmCartService.LoadOrCreateCart(cartName));
        }

        private LargeOrderGroupViewModel GetLargeCartViewModel(ICart cart)
        {
            var results = new Dictionary<ILineItem, List<ValidationIssue>>();

            if (cart != null) results = _trmCartService.ValidateCart(cart, true);

            return GetLargeCartViewModel(cart, results);
        }

        public LargeOrderGroupViewModel GetLargeCartViewModel(IOrderGroup cart, Dictionary<ILineItem, List<ValidationIssue>> validationIssues)
        {
            var vm = new LargeOrderGroupViewModel();

            CreateOrderGroupViewModel(cart, vm);
            foreach (var issue in validationIssues.Where(i => !string.IsNullOrWhiteSpace(i.Key.Code)))
            {
                vm.Validation.Add(new CartItemValidationViewModel
                {
                    Code = issue.Key.Code,
                    DisplayName = issue.Key.DisplayName,
                    ImageUrl = issue.Key.Code,
                    PlacedPrice = new Money(issue.Key.PlacedPrice, _currencyService.GetCurrentCurrency()).ToString(),
                    Quantity = issue.Key.Quantity,
                    Reasons = GetValidationMessage(issue)
                });
            }

            return vm;
        }

        private MiniOrderGroupViewModel GetMiniCartViewModel(string cartName)
        {
            var cart = _trmCartService.LoadCart(cartName, true);

            return GetMiniCartViewModel(cart);
        }

        private MiniOrderGroupViewModel GetMiniCartViewModel(IOrderGroup cart)
        {
            var vm = new MiniOrderGroupViewModel();

            CreateOrderGroupViewModel(cart, vm);

            return vm;
        }

        public CartSummaryViewModel GetCartSummaryViewModel()
        {
            var defaultCart = _trmCartService.LoadCart(_trmCartService.DefaultCartName);
            var bullionCart = _trmCartService.LoadCart(_trmCartService.DefaultBullionCartName, true);

            if (defaultCart == null && bullionCart == null) return new CartSummaryViewModel { Modified = _safeBeginningOfTime };
            if (defaultCart != null && bullionCart == null) return new CartSummaryViewModel { Modified = defaultCart.Modified };
            if (defaultCart == null) return new CartSummaryViewModel { Modified = bullionCart.Modified };

            return new CartSummaryViewModel { Modified = defaultCart.Modified > bullionCart.Modified ? defaultCart.Modified : bullionCart.Modified };
        }

        private void CreateOrderGroupViewModel(IOrderGroup orderGroup, IOrderGroupViewModel vm, CustomerContact customerContact = null)
        {
            using (_miniProfiler.Step("Create Cart ViewModel"))
            {
                if (orderGroup == null || (!(orderGroup is IPurchaseOrder) && orderGroup.OrderStatus == OrderStatus.Completed))
                {
                    vm.Modified = _safeBeginningOfTime;
                    return;
                }

                var savedAmount = orderGroup.GetSavedAmountWithoutDelivery();
                if (savedAmount > decimal.Zero)
                {
                    vm.SavedAmountDecimal = savedAmount;
                    vm.SavedAmount = (new Money(savedAmount, orderGroup.Currency)).ToString();
                }

                var items = orderGroup.GetAllLineItems().ToList();
                var totals = _orderGroupCalculator.GetOrderGroupTotals(orderGroup);
                var shippingTotal = orderGroup.GetShippingTotal();
                var shippingDiscountTotal = orderGroup.GetShippingDiscountTotal();
                var totalOrderDiscount = _orderGroupCalculator.GetOrderDiscountTotal(orderGroup);
                var totalDiscount = totalOrderDiscount + shippingDiscountTotal;
                var totalWithoutRecurring = _paymentHelper.GetCartTotalWithoutRecuringItems(orderGroup);

                foreach (var shipment in orderGroup.GetFirstForm().Shipments)
                {
                    vm.Shipments.Add(CreateShipmentViewModel(orderGroup, shipment, totals, customerContact));

                    vm.Id = orderGroup?.OrderLink?.OrderGroupId != null ? orderGroup.OrderLink.OrderGroupId.ToString() : string.Empty;
                    vm.PromotionCode = orderGroup?.GetFirstForm()?.CouponCodes != null && orderGroup.GetFirstForm().CouponCodes.Any() ?
                                            string.Join(",", orderGroup.GetFirstForm().CouponCodes) : string.Empty;
                }

                vm.OrderDeliveryName = orderGroup.GetOrderDeliveryName();
                vm.SubTotal = totals.SubTotal.ToString();
                vm.SubTotalDecimal = totals.SubTotal.Amount;
                vm.TaxTotal = totals.TaxTotal.ToString();
                vm.TotalDelivery = shippingTotal.ToString();
                vm.IsFreeDelivery = shippingTotal == 0;

                // Always show original discount price on basket and checkout
                vm.TotalDeliveryWithoutDiscount = (shippingTotal + shippingDiscountTotal).ToString();
                vm.Total = totals.Total.ToString();
                vm.TotalDecimal = totals.Total.Amount;
                vm.TotalWithoutDeliveryDecimal = totals.Total.Amount - totals.ShippingTotal.Amount;
                vm.ItemCount = GetItemCount(vm.Shipments);
                vm.SkuCount = items.Count;
                vm.TotalOrderDiscountDecimal = totalOrderDiscount;
                vm.TotalDiscountDecimal = totalDiscount.Amount;
                vm.TotalDiscount = totalDiscount.ToString();
                vm.TotalWithoutRecurringDecimal = totalWithoutRecurring.Amount;
                vm.TotalWithoutRecurring = totalWithoutRecurring.ToString();
                vm.Promotions = orderGroup.GetOrderPromotionSummary();
                vm.Modified = orderGroup.Modified;
                vm.Status = _localizationService.GetStringByCulture(string.Format(StringResources.OrderStatus, orderGroup.OrderStatus),
                    orderGroup.GetOrderStatus(),
                    ContentLanguage.PreferredCulture);
            }
        }

        private decimal GetItemCount(List<ShipmentViewModel> shipments)
        {
            return shipments.SelectMany(x => x.CartItems).Sum(item =>
                item.BullionCartItem == null
                    ? item.Quantity
                    : (item.BullionCartItem.IsSignatureVariant ? 1 : item.Quantity));
        }

        private ShipmentViewModel CreateShipmentViewModel(IOrderGroup orderGroup, IShipment shipment, OrderGroupTotals totals, CustomerContact customerContact = null)
        {
            var shipmentModel = new ShipmentViewModel
            {
                ShipmentUniqueId = $"{orderGroup.Name}{shipment.ShipmentId}",
                ShipmentId = shipment.ShipmentId,
                Address = _addressBookService.ConvertToModel(shipment.ShippingAddress),
                CartItems = new List<CartItemViewModel>(),
                ShippingMethods = new List<ShippingMethodViewModel>()
            };
            shipmentModel.Address.Organization = shipment.ShippingAddress?.Organization;
            foreach (var lineItem in shipment.LineItems)
            {
                shipmentModel.CartItems.Add(CreateCartItemViewModel(lineItem, orderGroup, shipment, customerContact));
            }

            if (shipment.ShippingMethodId != Guid.Empty || shipmentModel.CartItems.Any())
            {
                Currency currency;
                IMarket currentMarket;
                if (customerContact != null)
                {
                    // If user is from Scheduled Job - for auto invest
                    currency = _currencyService.GetCurrentCurrency(_bullionContactHelper.GetDefaultCurrencyCode(customerContact));
                    currentMarket = customerContact.GetCurrentMarket();
                }
                else
                {
                    // Default behaviour
                    currency = _currencyService.GetCurrentCurrency();
                    currentMarket = _currentMarket.GetCurrentMarket();
                }
                // Add shipping method to view model
                if (shipment.ShippingMethodId != Guid.Empty)
                {
                    shipmentModel.ShippingMethodId = shipment.ShippingMethodId;

                    shipmentModel.ShippingMethods.Add(new ShippingMethodViewModel
                    {
                        Id = shipment.ShippingMethodId,
                        DisplayName = shipment.ShippingMethodName,
                        DeliverCost = _shippingCalculator.GetShippingCost(shipment, currentMarket, currency).ToString(),
                        Price = totals.ShippingTotal
                    });
                }

                // Estimated storage cost on cart
                if (shipmentModel.CartItems.Any())
                {
                    var estimatedStorageCost = shipmentModel.CartItems.Where(x => x.BullionCartItem != null)
                        .Sum(x => x.BullionCartItem.EstimatedStorageCost);
                    shipmentModel.EstimatedStorageCost = new Money(estimatedStorageCost, currency).ToString();
                }
            }

            return shipmentModel;
        }

        private CartItemViewModel CreateCartItemViewModel(ILineItem lineItem, IOrderGroup cart, IShipment shipment, CustomerContact customerContact = null)
        {
            using (_miniProfiler.Step("Create Cart Item ViewModel"))
            {
                var variant = lineItem.GetEntryContent() as TrmVariant;

                if (variant == null) return new CartItemViewModel { DisplayName = StringConstants.Cart.ItemNotFound };

                var contentRef = variant.ContentLink;

                //Get discounted price
                var discountedPrice = string.Empty;
                decimal discountedPriceDecimal = 0;
                var checkDiscount = lineItem.IsGift
                    ? new Money(0, cart.Currency)
                    : GetDiscountedPrice(cart, lineItem);

                if (checkDiscount.HasValue)
                {
                    discountedPrice = checkDiscount.Value.ToString();
                    discountedPriceDecimal = checkDiscount.Value.Amount;
                }

                //Get discounted unit price
                var discountedUnitPrice = string.Empty;
                decimal discountedUnitPriceDecimal = 0;
                var checkDiscountUnit = GetDiscountedUnitPrice(cart, lineItem);
                if (checkDiscountUnit.HasValue)
                {
                    discountedUnitPrice = checkDiscountUnit.Value.ToString();
                    discountedUnitPriceDecimal = checkDiscountUnit.Value.Amount;
                }

                var stockSummary = _inventoryHelper.GetStockSummary(variant.ContentLink, customerContact);

                //if the item has a shipping message overwrite the stocksummary used on orders to preserve the message displayed prior to inventory allowcation
                if (!string.IsNullOrWhiteSpace(lineItem.Properties[CustomFields.ShippingMessageFieldName]?.ToString()))
                {
                    stockSummary.ShippingMessage = lineItem.Properties[CustomFields.ShippingMessageFieldName].ToString();
                }

                var placedPrice = GetPlacedPrice(lineItem);
                var hasBeenPersonalised = lineItem.Properties[CustomFields.PersonalisationUniqueId]?.ToString() != null;

                if (hasBeenPersonalised)
                {
                    var newPlacedPrice = GetPlacedPrice(lineItem);

                    placedPrice = newPlacedPrice;
                }

                var vm = new CartItemViewModel
                {
                    Code = lineItem.Code,
                    DisplayName = string.IsNullOrEmpty(lineItem.DisplayName) ? variant.SubDisplayName : lineItem.DisplayName,
                    SubTitle = variant.SubDisplayName,
                    ImageUrl = GetLineItemThumbnailImageUrl(lineItem, variant),
                    Url = contentRef.GetExternalUrl_V2(),
                    DiscountedPrice = discountedPrice,
                    DiscountedPriceDecimal = discountedPriceDecimal,
                    PlacedPrice = new Money(placedPrice, cart.Currency).ToString(),
                    PlacedPriceDecimal = placedPrice,
                    Quantity = lineItem.Quantity,
                    Entry = contentRef,
                    IsAvailable = true,
                    DiscountedUnitPrice = discountedUnitPrice,
                    DiscountedUnitPriceDecimal = discountedUnitPriceDecimal,
                    IsGift = variant.IsGifting,
                    ShipmentId = shipment.ShipmentId,
                    RecurringDetailsMessage = variant.RecurringBasketMessage ?? string.Empty,
                    RecurringCheckoutMessage = variant.RecurringCheckoutMessage ?? string.Empty,
                    RecurringConfirmationMessage = variant.RecurringOrderConfirmationMessage ?? "",
                    RecurrenceType = variant.RecurrenceType,
                    Subscribed = GetSubscribed(lineItem),
                    StockSummary = GetStockSummary(lineItem, variant),
                    TagMessage = _entryHelper.GetTagMessage(contentRef),
                    SupplierMessage = variant.SupplierMessage ?? string.Empty,
                    FulfilledBy = GetFulfilledByMessage(variant),
                    IsAgeRestricted = variant.IsAgeRestricted,
                    //Personalisation properties
                    HasbeenPersonalised = hasBeenPersonalised,
                    UniqueId = lineItem.Properties[CustomFields.PersonalisationUniqueId]?.ToString(),
                    CanBePersonalised = variant.CanBePersonalised == Shared.Constants.Enums.CanBePersonalised.Yes,
                    PersonalisationType = variant.PersonalisationType,
                    PersonalisationCharge = variant.PersonalisationPrice,
                    PrintzwareVariantId = variant.PrintzwareVariantId,
                    PrintzwareEditUrl = _printzwareHelper.OpenUrl(lineItem.Properties[Shared.Constants.StringConstants.CustomFields.PersonalisationUniqueId]?.ToString(), variant.PrintzwareVariantId, false),
                    BrandDisplayName = variant.BrandDisplayName,
                    CategoryName = variant.Category,
                };

                vm.BullionCartItem = PopulateBullionCartItemViewModel(lineItem, cart, shipment, variant, customerContact);

                // Original prices for consumer products
                if (vm.BullionCartItem == null)
                {
                    var originalPrice = lineItem.Quantity * placedPrice;
                    vm.OriginalPrice = new Money(originalPrice, cart.Currency).ToString();
                    vm.OriginalPriceDecimal = originalPrice;
                }

                return vm;
            }
        }

        private bool GetSubscribed(ILineItem lineItem)
        {
            return lineItem.Properties[CustomFields.SubscribedFieldName] != null &&
                   lineItem.Properties[CustomFields.SubscribedFieldName].Equals(true);
        }

        private string GetFulfilledByMessage(TrmVariant variant)
        {
            var fulfilledResourceKey = $"{StringResources.FulfilledBy}/{variant.FulfilledBy}".ToLowerInvariant();
            return _localizationService.GetStringByCulture(fulfilledResourceKey, ContentLanguage.PreferredCulture);
        }

        private string GetLineItemThumbnailImageUrl(IExtendedProperties lineItem, IContent variant)
        {
            var imageUrl = _assetHelper.GetDefaultAssetUrl(variant.ContentLink);

            var pwEpiImageId = lineItem.Properties[CustomFields.PersonalisationImageId]?.ToString();
            if (!string.IsNullOrWhiteSpace(pwEpiImageId))
            {
                imageUrl = $"/mvcApi/cart/getimage?id={pwEpiImageId}";
            }

            return imageUrl;
        }

        private BullionCartItemViewModel PopulateBullionCartItemViewModel(ILineItem lineItem, IOrderGroup cart, IShipment shipment, TrmVariant variant, CustomerContact customerContact = null)
        {
            var customer = customerContact ?? _customerContext.CurrentContact;
            var currency = customerContact != null ?
                _currencyService.GetCurrentCurrency(_bullionContactHelper.GetDefaultCurrencyCode(customerContact)) :
                _currencyService.GetCurrentCurrency();

            var investmentVariant = variant as VirtualVariantBase;

            var placedPrice = new Money(lineItem.GetCustomLineItemPlacedPrice(), currency);
            var checkDiscountedPrice = GetDiscountedPrice(cart, lineItem);

            if (investmentVariant != null)
                return new BullionCartItemViewModel
                {
                    IsSignatureVariant = true,
                    PricePerUnit = placedPrice / investmentVariant.TroyOzWeightConfiguration,
                    PricePerUnitString = (placedPrice / investmentVariant.TroyOzWeightConfiguration).ToString(),
                    LivePrice = checkDiscountedPrice ?? GetLivePrice(lineItem, cart.Currency),
                    Weight = Math.Round(lineItem.Quantity * investmentVariant.TroyOzWeightConfiguration, 3),
                    VatAmount = GetVatAmountForLineItem(lineItem, cart, shipment),
                    RequestedInvestment = new Money(lineItem.GetPropertyValue<decimal>(CustomFields.BullionSignatureValueRequested), currency),
                    RequestedInvestmentString = new Money(lineItem.GetPropertyValue<decimal>(CustomFields.BullionSignatureValueRequested), currency).ToString(),
                    CanDeliver = false,
                    CanVault = true,
                    EstimatedStorageCost = GetEstimatedStoreCostForLineItem(lineItem, customer, investmentVariant)
                };

            var physicalVariant = variant as PhysicalVariantBase;
            if (physicalVariant != null)
                return new BullionCartItemViewModel
                {
                    IsSignatureVariant = false,
                    PricePerUnit = placedPrice,
                    PricePerUnitString = placedPrice.ToString(),
                    LivePrice = checkDiscountedPrice ?? GetLivePrice(lineItem, cart.Currency),
                    Quantity = lineItem.Quantity,
                    VatAmount = GetVatAmountForLineItem(lineItem, cart, shipment),
                    CanDeliver = physicalVariant.CanDeliver && !_bullionContactHelper.IsSippContact(customer),
                    CanVault = physicalVariant.CanVault,
                    EstimatedStorageCost = GetEstimatedStoreCostForLineItem(lineItem, customer, physicalVariant)
                };

            return null;
        }

        public Money GetLivePrice(ILineItem lineItem, Currency currency)
        {
            decimal adjustedTotalPriceFromPampQuote;
            if (decimal.TryParse(lineItem.Properties[CustomFields.BullionAdjustedTotalPriceIncludePremiums]?.ToString(), out adjustedTotalPriceFromPampQuote) && adjustedTotalPriceFromPampQuote > decimal.Zero)
            {
                return new Money(Math.Max(decimal.Zero, adjustedTotalPriceFromPampQuote), currency);
            }
            return new Money(lineItem.GetCustomLineItemPlacedPrice(), currency);
        }

        private Money? GetVatAmountForLineItem(ILineItem lineItem, IOrderGroup cart, IShipment shipment)
        {
            return lineItem.GetSalesTax(_currentMarket.GetCurrentMarket(), cart.Currency, shipment.ShippingAddress, _lineItemCalculator);
        }

        private StockSummaryDto GetStockSummary(IExtendedProperties lineItem, IContent variant)
        {
            var stockSummary = _inventoryHelper.GetStockSummary(variant.ContentLink);

            //if the item has a shipping message overwrite the stocksummary used on orders to preserve the message displayed prior to inventory allowcation
            if (!string.IsNullOrWhiteSpace(lineItem.Properties[CustomFields.ShippingMessageFieldName]?.ToString()))
            {
                stockSummary.ShippingMessage = lineItem.Properties[CustomFields.ShippingMessageFieldName].ToString();
            }

            return stockSummary;
        }

        private decimal GetPlacedPrice(ILineItem lineItem)
        {

            if (string.IsNullOrWhiteSpace(lineItem.Properties[CustomFields.PersonalisationCharge]?.ToString())) return lineItem.PlacedPrice;

            var personalisationPrice = 0m;

            if (decimal.TryParse(lineItem.Properties[CustomFields.PersonalisationCharge]?.ToString(), out personalisationPrice))

            {
                var price = lineItem.PlacedPrice + personalisationPrice;
                return new Money(Math.Max(decimal.Zero, price), _currencyService.GetCurrentCurrency());
            }

            return lineItem.PlacedPrice;
        }

        private Money? GetDiscountedUnitPrice(IOrderGroup cart, ILineItem lineItem)
        {
            if (lineItem.Quantity == decimal.Zero) return new Money(decimal.Zero, cart.Currency);
            var discountedPrice = GetDiscountedPrice(cart, lineItem) / lineItem.Quantity;
            return discountedPrice.GetValueOrDefault().Amount < lineItem.PlacedPrice ? discountedPrice : null;
        }

        private Money? GetDiscountedPrice(IOrderGroup cart, ILineItem lineItem)
        {
            using (_miniProfiler.Step("Get Price"))
            {
                if (!cart.Name.Equals(_trmCartService.DefaultWishListName))
                    return lineItem.GetDiscountedPrice(cart.Currency, _lineItemCalculator);

                var currency = cart.Currency;
                var discountedPrice = _promotionService.GetDiscountPrice(new CatalogKey(lineItem.Code), cart.MarketId, currency);
                return discountedPrice?.UnitPrice;
            }
        }

        private string ValidationReason(ValidationIssue issue)
        {
            switch (issue)
            {
                case ValidationIssue.None:
                    return StringConstants.TranslationFallback.CartValidationReasonNone;
                case ValidationIssue.CannotProcessDueToMissingOrderStatus:
                    return StringConstants.TranslationFallback.CartValidationReasonCannotProcessDueToMissingOrderStatus;
                case ValidationIssue.RemovedDueToCodeMissing:
                    return StringConstants.TranslationFallback.CartValidationReasonRemovedDueToCodeMissing;
                case ValidationIssue.RemovedDueToNotAvailableInMarket:
                    return string.Format(
                        StringConstants.TranslationFallback.CartValidationReasonRemovedDueToNotAvailableInMarket,
                        _currentMarket.GetCurrentMarket().MarketName);
                case ValidationIssue.RemovedDueToUnavailableCatalog:
                    return StringConstants.TranslationFallback.CartValidationReasonRemovedDueToUnavailableCatalog;
                case ValidationIssue.RemovedDueToUnavailableItem:
                    return StringConstants.TranslationFallback.CartValidationReasonRemovedDueToUnavailableItem;
                case ValidationIssue.RemovedDueToInsufficientQuantityInInventory:
                    return StringConstants.TranslationFallback
                        .CartValidationReasonRemovedDueToInsufficientQuantityInInventory;
                case ValidationIssue.RemovedDueToInactiveWarehouse:
                    return StringConstants.TranslationFallback.CartValidationReasonRemovedDueToInactiveWarehouse;
                case ValidationIssue.RemovedDueToMissingInventoryInformation:
                    return StringConstants.TranslationFallback
                        .CartValidationReasonRemovedDueToMissingInventoryInformation;
                case ValidationIssue.RemovedDueToInvalidPrice:
                    return StringConstants.TranslationFallback.CartValidationReasonRemovedDueToInvalidPrice;
                case ValidationIssue.AdjustedQuantityByMinQuantity:
                    return StringConstants.TranslationFallback.CartValidationReasonAdjustedQuantityByMinQuantity;
                case ValidationIssue.AdjustedQuantityByMaxQuantity:
                    return StringConstants.TranslationFallback.CartValidationReasonAdjustedQuantityByMaxQuantity;
                case ValidationIssue.AdjustedQuantityByBackorderQuantity:
                    return StringConstants.TranslationFallback.CartValidationReasonAdjustedQuantityByBackorderQuantity;
                case ValidationIssue.AdjustedQuantityByPreorderQuantity:
                    return StringConstants.TranslationFallback.CartValidationReasonAdjustedQuantityByPreorderQuantity;
                case ValidationIssue.AdjustedQuantityByAvailableQuantity:
                    return StringConstants.TranslationFallback.CartValidationReasonAdjustedQuantityByAvailableQuantity;
                case ValidationIssue.PlacedPricedChanged:
                    return StringConstants.TranslationFallback.CartValidationReasonPlacedPricedChanged;
                default:
                    return StringConstants.TranslationFallback.CartValidationReasonGeneric;
            }
        }

        public string GetValidationMessages(Dictionary<ILineItem, List<ValidationIssue>> validationResults)
        {
            if (validationResults == null || !validationResults.Any()) return string.Empty;

            var warningMessages = string.Empty;

            foreach (var result in validationResults)
            {
                var lineItem = result.Key;
                var entry = lineItem.GetEntryContent();
                var displayName = entry != null
                    ? _entryHelper.GetTruncatedDisplayName(lineItem.GetEntryContent().ContentLink)
                    : lineItem.DisplayName;

                warningMessages = warningMessages + string.Format(_localizationService.GetStringByCulture(StringResources.CartValidationItem,
                    StringConstants.TranslationFallback.CartItemValidation, ContentLanguage.PreferredCulture), displayName);

                warningMessages += GetValidationMessage(result);

                warningMessages += ", ";
            }

            warningMessages = warningMessages.Substring(0, warningMessages.Length - 2);
            return warningMessages;
        }

        public bool ValidateCart(ICart cart, out string cartValidationMessages)
        {
            return ValidateCart(cart, out cartValidationMessages, null);
        }
        public bool ValidateCart(ICart cart, out string cartValidationMessages, CustomerContact customerContact)
        {
            cartValidationMessages = string.Empty;

            var validationIssues = _trmCartService.ValidateCart(cart, customerContact);

            if (!validationIssues.Any()) return true;

            cartValidationMessages = GetValidationMessages(validationIssues);
            return false;
        }
        public ProcessPaymentForBullionCartResponse ProcessPaymentForBullionCart(IOrderGroup cart, string orderNumberPrefix, CustomerContact customerContact = null)
        {
            var response = new ProcessPaymentForBullionCartResponse();
            RemovePreviousPayments(cart, customerContact);

            var paymentMethod = _paymentMethodHelper.GetDefaultBullionPaymentMethod(cart, customerContact);
            if (paymentMethod == null)
            {
                response.IsPaymentMethodError = true;
                response.Message = Enums.CheckoutIssue.MissingDefaultPaymentMethod.GetDescriptionAttribute();
                return response;
            }

            var paymentDto = new BasePaymentDto();
            string messages;

            var billingAddress = cart.GetFirstShipment().ShippingAddress;
            var orderNumber = string.Concat(orderNumberPrefix, cart.OrderLink.OrderGroupId.ToString(CultureInfo.CurrentCulture));
            var paymentCreated = CreatePayment(cart, paymentMethod, orderNumber, billingAddress, paymentDto, out messages);
            if (!paymentCreated)
            {
                response.IsPaymentCreatedError = true;
                response.Message = Enums.CheckoutIssue.CanNotCreatePayment.GetDescriptionAttribute();
                return response;
            }

            _orderRepository.Save(cart);

            try
            {
                var results = cart.ProcessPayments(_paymentProcessor, _orderGroupCalculator).ToList();
                _orderRepository.Save(cart);

                response.Success = true;
                response.results = results;
                return response;
            }
            catch (PaymentException paymentException)
            {
                response.IsPaymentExceptionError = true;
                response.Message = Enums.CheckoutIssue.CanNotProcessPayment.GetDescriptionAttribute();
                response.Exception = paymentException;
            }

            return response;
        }
        public bool HasRetryWithPampExportTransaction()
        {
            return HasRetryWithPampExportTransaction(null);
        }
        public bool HasRetryWithPampExportTransaction(CustomerContact customerContact)
        {
            var currentContactId = customerContact != null ? ((Guid)customerContact.PrimaryKeyId) : CustomerContext.Current.CurrentContactId;
            var numberOfRetryWithPampExportTransaction =
               _exportTransactionsRepository.Value.GetRetryWithPampPurchaseOrdersExportTransaction(new string[]
               {
                    currentContactId.ToString()
               }).Count();

            return numberOfRetryWithPampExportTransaction > 0;
        }
        private string GetValidationMessage(KeyValuePair<ILineItem, List<ValidationIssue>> validation)
        {
            var messages = string.Empty;

            foreach (var issue in validation.Value)
            {
                var fallbackMsg = ValidationReason(issue);
                messages += _localizationService.GetStringByCulture(
                                string.Format(StringResources.CartValidationReason, issue), fallbackMsg,
                                ContentLanguage.PreferredCulture) + ", ";
            }

            if (validation.Value.Any())
                messages = messages.Substring(0, messages.Length - 2);

            return messages;
        }

        public IPurchaseOrder ConvertToPurchaseOrder(ICart cart, string orderNumberPrefix)
        {
            return ConvertToPurchaseOrder(cart, orderNumberPrefix, null);
        }
        public IPurchaseOrder ConvertToPurchaseOrder(ICart cart, string orderNumberPrefix, CustomerContact customerContact)
        {

            _trmCartService.InventoryProcessorAdjustInventoryOrRemoveLineItem(cart);
            _trmCartService.RequestInventory(cart);

            var orderReference = _orderRepository.SaveAsPurchaseOrder(cart);

            var po = _orderRepository.Load<IPurchaseOrder>(orderReference.OrderGroupId);

            //Set the order id before printzware
            po.OrderNumber = cart.GetFirstForm()?.Payments.FirstOrDefault()?.TransactionID ??
                             string.Concat(orderNumberPrefix, po.OrderLink.OrderGroupId.ToString(CultureInfo.CurrentCulture));

            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage != null && !startPage.PerformanceMode && startPage.FullCheckoutLoggingLevel >=
                Shared.Constants.Enums.FullCheckoutLoggingLevel.Cookies)
            {
                _orderGroupAuditHelper.WriteAudit(po, "MastercardPageController.PlacedOrder",
                    $"Purchase Order OrderLink.OrderGroupId: {po.OrderLink.OrderGroupId}");
                _orderGroupAuditHelper.WriteAudit(po, "MastercardPageController.PlacedOrder",
                    $"Purchase Order Created: {po.Created:yyyy-MM-dd hh:mm:ss:fff}");
                _orderGroupAuditHelper.WriteAudit(po, "MastercardPageController.PlacedOrder",
                    $"Purchase Order Number: {po.OrderNumber}");

                var purchaseOrder = po as PurchaseOrder;
                if (purchaseOrder != null)
                {
                    _orderGroupAuditHelper.WriteAudit(po, "MastercardPageController.PlacedOrder",
                        $"Purchase Order ParentOrderGroupId: {purchaseOrder.ParentOrderGroupId}");
                    _orderGroupAuditHelper.WriteAudit(po, "MastercardPageController.PlacedOrder",
                        $"Purchase Order TrackingNumber: {purchaseOrder.TrackingNumber}");
                    _orderGroupAuditHelper.WriteAudit(po, "MastercardPageController.PlacedOrder",
                        $"Purchase Order Id: {purchaseOrder.Id}");
                    _orderGroupAuditHelper.WriteAudit(po, "MastercardPageController.PlacedOrder",
                        $"Purchase Order OrderGroupId: {purchaseOrder.OrderGroupId}");
                }

                _orderRepository.Save(po);

                _trmCartService.InventoryProcessorAdjustInventoryOrRemoveLineItem(cart);
                _trmCartService.RequestInventory(cart);
            }

            var thirdPartyTransaction = _thirdPartyTransactionRepository.GetTransaction(po.OrderNumber);
            if (thirdPartyTransaction != null)
            {
                thirdPartyTransaction.TransactionPayloadJson = string.Empty;
                thirdPartyTransaction.TransactionStatus = ThirdPartyTransactionStatus.Success;
                _thirdPartyTransactionRepository.AddOrUpdateTransaction(thirdPartyTransaction);
            }

            _printzwareHelper.ConfirmOrder(po);

            //Set promotions on the purchase order
            po.Properties[CustomFields.PromotionCode] = cart.Forms.FirstOrDefault()?.CouponCodes?.FirstOrDefault() ?? string.Empty;
            //Set the customer code / Ods account number
            po.Properties[CustomFields.CustomerCode] = _customerContext.CurrentContact?.GetStringProperty(CustomFields.ObsAccountNumber) ?? String.Empty;
            po.Properties[CustomFields.GiftMessage] = cart.Properties[CustomFields.GiftMessage] ?? string.Empty;
            po.Properties[CustomFields.IsGiftOrder] = cart.Properties[CustomFields.IsGiftOrder] ?? string.Empty;

            // All Shipment Complete needs to false on initial purchase
            // Becuase it will be actioned in AX 
            po.Properties[CustomFields.ShipComplete] = false; // cart.GetShippingStatus();

            po.Properties[CustomFields.SendStatus] = 1;

            var cartType = cart.GetCartType();
            po.Properties[CustomFields.Type] = cartType;

            if (cartType.Equals(StringConstants.CartType.Bullion))
            {
                po.Properties[CustomFields.CustomerCode] = _customerContext.CurrentContact?.GetStringProperty(CustomFields.BullionObsAccountNumber) ?? String.Empty;
                po.Properties[CustomFields.BullionVATAmount] = cart.GetTaxTotal().Amount;
                po.Properties[CustomFields.ShippingVAT] = cart.GetFirstShipment()?.GetShippingVatCode();
                po.Properties[CustomFields.ShippingVATCost] = _shippingCalculator.GetShippingTax(cart.GetFirstShipment(), (customerContact != null ? customerContact.GetCurrentMarket() : _currentMarket.GetCurrentMarket()), cart.Currency).ToString();
                po.Properties[CustomFields.BullionPAMPRequestQuoteId] = cart.Properties[CustomFields.BullionPAMPRequestQuoteId];
                po.Properties[CustomFields.BullionPAMPQuoteId] = cart.Properties[CustomFields.BullionPAMPQuoteId];
                //Set Storage for vault item
                UpdateStorageFieldForLineItem(po, customerContact);
            }

            var impersonateUser = _userService.GetImpersonateUser(customerContact);

            po.Properties[CustomFields.TransactionPerformedBy] = impersonateUser != null ? impersonateUser.UserType : Shared.Constants.StringConstants.Customer;
            po.Properties[CustomFields.TransactionPerformedByUserId] = impersonateUser?.UserId;

            po.OrderStatus = OrderStatus.InProgress;

            po.ApplyShipment(cart);

            if (_customerContext.CurrentContact != null)
            {
                foreach (var f in po.Forms)
                {
                    f.Properties[CustomFields.IsByMintMarque] =
                        !string.IsNullOrEmpty(_customerContext.CurrentContact.GetStringProperty(CustomFields.CustClassificationId));
                }
            }

            _orderRepository.Delete(cart.OrderLink);
            _orderRepository.Save(po);
            return po;
        }
        public PurchaseOrderViewModel GetPurchaseOrderViewModel(IPurchaseOrder purchaseOrder)
        {
            return GetPurchaseOrderViewModel(purchaseOrder, null);
        }
        public PurchaseOrderViewModel GetPurchaseOrderViewModel(IPurchaseOrder purchaseOrder, CustomerContact customerContact)
        {
            var vm = new PurchaseOrderViewModel();

            CreateOrderGroupViewModel(purchaseOrder, vm, customerContact);

            vm.OriginalOrderNumber = purchaseOrder.OrderNumber;
            vm.OrderNumbers = _orderHelper.GetOrderNumbersFromPurchaseOrder(purchaseOrder);
            vm.OrderFound = true;

            foreach (var form in purchaseOrder.Forms)
            {
                foreach (var payment in form.Payments)
                {
                    vm.Payments.Add(CreatePaymentSummaryViewModel(payment));
                }
            }

            return vm;
        }

        private PaymentSummaryViewModel CreatePaymentSummaryViewModel(IPayment payment)
        {
            var billingAddress = _addressBookService.ConvertToModel(payment.BillingAddress);
            var paymentSummary = new PaymentSummaryViewModel { MethodName = payment.PaymentMethodName, Address = billingAddress };
            switch (payment.PaymentMethodName)
            {
                case Payments.Mastercard:
                    var mastercardPayment = payment as MastercardPayment;
                    if (mastercardPayment != null)
                    {
                        paymentSummary.CardNumber = mastercardPayment.MastercardCardNumber;
                        paymentSummary.CardType = mastercardPayment.MastercardCardType;
                    }
                    break;
            }

            return paymentSummary;
        }

        public void RemovePreviousPayments(IOrderGroup order)
        {
            RemovePreviousPayments(order, null);
        }
        public void RemovePreviousPayments(IOrderGroup order, CustomerContact customerContact)
        {
            if (!order.Forms.Any() || order.Forms.First().Payments == null) return;

            if (order.Forms.First().Payments == null || order.Forms.First().Payments.Count <= 0) return;

            foreach (var pay in order.Forms.First().Payments)
            {
                if (pay == null) continue;

                _orderGroupAuditHelper.WriteAudit(order, pay.PaymentMethodName, $"Payment for {pay.Amount} removed");

                if (pay.PaymentMethodName != Payments.CreditPayment) continue;

                if (pay.Status == PaymentStatus.Processed.ToString())
                    _creditHelper.DeductCredit(pay.Amount * -1, customerContact);
            }

            foreach (var orderForm in order.Forms)
            {
                orderForm.Payments?.Clear();
            }

            _orderRepository.Save(order);
        }

        public bool CreatePayment(IOrderGroup cart, PaymentMethodDto.PaymentMethodRow paymentMethod, string orderNumber, IOrderAddress billingAddress, BasePaymentDto paymentDto, out string messages)
        {
            var payment = cart.CreatePayment(_orderGroupFactory, Type.GetType(paymentMethod.PaymentImplementationClassName));

            if (payment == null)
            {
                messages = "No payment information";
                return false;
            }
            var totals = _orderGroupCalculator.GetOrderGroupTotals(cart);

            payment.TransactionID = orderNumber;
            payment.Amount = totals.Total.Amount;
            payment.CustomerName = cart.Name;
            payment.ValidationCode = string.Empty;
            payment.TransactionType = TransactionType.Sale.ToString();
            payment.PaymentMethodId = paymentMethod.PaymentMethodId;
            payment.PaymentMethodName = paymentMethod.Name;
            payment.BillingAddress = billingAddress;

            bool paymentSetup;

            if (paymentMethod.SystemKeyword == Payments.Mastercard)
            {
                var mastercardPaymentDto = paymentDto as MastercardPaymentDto;
                if (mastercardPaymentDto == null)
                {
                    messages = "No payment information";
                    return false;
                }

                var mastercardPayment = new MastercardPayment();
                paymentSetup = SetupMastercardPayment(mastercardPayment, mastercardPaymentDto, out messages);
                payment.CopyPropertiesWithOverwriteFrom(mastercardPayment);
            }
            else
            {
                messages = string.Empty;
                paymentSetup = true;
            }

            cart.AddPayment(payment);

            return paymentSetup;
        }

        private bool SetupMastercardPayment(IPayment mastercardPayment, MastercardPaymentDto mastercardPaymentDto, out string messages)
        {
            mastercardPayment.Properties["MastercardSessionId"] = mastercardPaymentDto.SessionId;
            mastercardPayment.Properties["MastercardNameOnCard"] = mastercardPaymentDto.NameOnCard;
            mastercardPayment.Properties["TokenisedCardNumber"] = mastercardPaymentDto.TokenisedCard;
            mastercardPayment.Properties["MastercardCardExpiry"] = mastercardPaymentDto.CardToUse.CardExpiry;
            mastercardPayment.Properties["IsAmexPayment"] = mastercardPaymentDto.CardToUse.UseAmex;
            mastercardPayment.Properties["SaveCardDetails"] = mastercardPaymentDto.SaveCard;

            messages = string.Empty;
            return true;
        }

        public bool GetCardToken(PaymentMethodDto.PaymentMethodRow paymentMethod, IAmTokenisablePayment payment, ref string message)
        {
            if (string.IsNullOrEmpty(payment.MastercardSessionId)) return false;

            var gateway = new MastercardPaymentGateway();
            var isAmexPayment = payment.IsAmexPayment;
            var tokenization = gateway.TokeniseCard(payment.MastercardSessionId, paymentMethod, isAmexPayment, ref message);

            if (!string.IsNullOrEmpty(message)) return false;

            payment.MastercardCardNumber = tokenization.sourceOfFunds.provided.card.number;
            payment.MastercardCardType = tokenization.sourceOfFunds.provided.card.brand;
            payment.MastercardCardExpiry = tokenization.sourceOfFunds.provided.card.expiry;
            payment.TokenisedCardNumber = tokenization.token;

            return true;
        }

        public bool IsItemExistingInCart(string code, string cartName)
        {
            var cart = _trmCartService.LoadOrCreateCart(cartName);

            return cart.GetAllLineItems().Any(x => x.Code.Equals(code));
        }

        public int GetCartSummaryItemsCount()
        {
            var cart = _orderRepository.LoadCart<ICart>(_customerContext.CurrentContactId, _trmCartService.DefaultCartName, _currentMarket);
            var bullionCart = _orderRepository.LoadCart<ICart>(_customerContext.CurrentContactId, _trmCartService.DefaultBullionCartName, _currentMarket);

            var cartItemCount = cart?.Forms?.Sum(x => x.GetAllLineItems().Sum(i => i.Quantity)) ?? 0;

            if (bullionCart?.Forms != null)
            {
                foreach (var item in bullionCart.GetAllLineItems())
                {
                    if (item.Properties.ContainsKey(TRM.Shared.Constants.StringConstants.CustomFields.BullionSignatureValueRequested))
                    {
                        cartItemCount++;
                        continue;
                    }

                    cartItemCount += item.Quantity;
                }
            }

            return (int)cartItemCount;
        }

        private decimal GetEstimatedStoreCostForLineItem(ILineItem lineItem, CustomerContact customerContact, PreciousMetalsVariantBase preciousMetalsVariant)
        {
            var rate = _storageRateHelper.GetMatchingStorageRateBlock(customerContact, preciousMetalsVariant)?.OngoingStorageRate ?? decimal.Zero;

            var originalPrice = _bullionPriceHelper.GetPricePerUnitForBullionVariant(preciousMetalsVariant, null, true);

            return originalPrice * lineItem.Quantity * rate / 100;
        }

        #region Coupon Code

        public bool AddCouponCode(ICart cart, string couponCode, out Enums.ePromotionValidation validation, bool checkApplied = true)
        {
            couponCode = couponCode.ToUpper();

            if (!(cart is ICart))
            {
                validation = Enums.ePromotionValidation.InvalidCart;
                return false;
            }

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                validation = Enums.ePromotionValidation.NoCouponProvided;
                return false;
            }

            var orderForm = cart.GetFirstForm();

            if (orderForm == null)
            {
                validation = Enums.ePromotionValidation.InvalidCartForm;
                return false;
            }

            var coupon = _couponService.GetByCouponCode(couponCode);

            // validate unique coupon code 
            if (coupon != null)
            {
                if (coupon.IsRedeemed())
                {
                    validation = Enums.ePromotionValidation.CouponNotValid;
                    return false;
                }
                // override with related promotion code
                couponCode = coupon.PromotionCode;
            }
            else
            {
                if (_couponService.IsCodeReserved(couponCode))
                {
                    validation = Enums.ePromotionValidation.CouponNotValid;
                    return false;
                }
            }

            if (ContainsCouponCode(cart, couponCode))
            {
                validation = Enums.ePromotionValidation.CouponAlreadyApplied;
                return false;
            }

            if (!CheckCouponAvailable(couponCode))
            {
                validation = Enums.ePromotionValidation.CouponNotValid;
                return false;
            }

            validation = Enums.ePromotionValidation.CouponApplied;
            orderForm.CouponCodes.Add(couponCode);

            if (!checkApplied)
            {
                _orderGroupAuditHelper.WriteAudit(cart, "Added Coupon - Without validation", string.Format("{0} (from: {1})", couponCode, HttpContext.Current.Request.UrlReferrer));

                _orderRepository.Save(cart);

                return true;
            }

            var couponApplied = _trmCartService.TryApplyCoupon(cart, couponCode);
            if (!couponApplied)
            {
                validation = Enums.ePromotionValidation.CouponNotValid;
                orderForm.CouponCodes.Remove(couponCode);
            }
            else
            {
                var orderFormPromotion = orderForm?.Promotions.FirstOrDefault(x => x.CouponCode == couponCode);
                if (orderFormPromotion == null)
                {
                    // Removing CouponCode - when actual promotion is not found but the code is valid
                    validation = Enums.ePromotionValidation.CouponNotValid;
                    orderForm.CouponCodes.Remove(couponCode);
                }
                else
                {
                    // store unique coupon code in appropriate cart to track for eventual redemption
                    if (coupon != null) cart.Properties[CustomFields.CouponCode] = coupon.Code;

                    if (orderForm.CouponCodes != null && orderForm.CouponCodes.Count() > 1)
                    {
                        // Remove previous coupon
                        var previousCouponCode = orderForm.CouponCodes.FirstOrDefault(x => x != couponCode);
                        orderForm.CouponCodes.Remove(previousCouponCode);

                        // Remove previous actual promotion code from order
                        var promotionValidation = Enums.ePromotionValidation.CouponNotValid;
                        var previousOrderFormPromotions = orderForm.Promotions.Where(x => x.CouponCode != null && x.CouponCode != couponCode).ToList();
                        foreach (var previousPromotion in previousOrderFormPromotions)
                        {
                            RemoveCouponCode(cart, previousPromotion.CouponCode, out promotionValidation);
                        }
                    }
                    _orderGroupAuditHelper.WriteAudit(cart, "Added Coupon", string.Format("{0} (from: {1})", couponCode, HttpContext.Current.Request.UrlReferrer));
                }
            }

            _orderRepository.Save(cart);

            return couponApplied;
        }

        public bool RemoveCouponCode(ICart cart, string couponCode, out Enums.ePromotionValidation validation)
        {
            if (cart == null)
            {
                validation = Enums.ePromotionValidation.InvalidCart;
                return false;
            }

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                validation = Enums.ePromotionValidation.NoCouponProvided;
                return false;
            }

            var form = cart.Forms.FirstOrDefault();

            if (form == null)
            {
                validation = Enums.ePromotionValidation.InvalidCartForm;
                return false;
            }

            form.CouponCodes.Remove(couponCode);

            var couponApplied = _trmCartService.TryApplyCoupon(cart, couponCode);
            if (couponApplied)
            {
                validation = Enums.ePromotionValidation.CouponRemoved;
                form.CouponCodes.Remove(couponCode);

                _orderGroupAuditHelper.WriteAudit(cart, "Removed Coupon", couponCode);
            }
            else
            {
                validation = Enums.ePromotionValidation.CouponNotValid;
            }

            _orderRepository.Save(cart);

            return !couponApplied;
        }

        public bool AddCouponCode(string promotionCode)
        {
            var cart = _trmCartService.LoadOrCreateCart(_trmCartService.DefaultCartName,
                _currentMarket.GetCurrentMarket().MarketId);

            Enums.ePromotionValidation promotionValidation;
            return AddCouponCode(cart, promotionCode, out promotionValidation, false);
        }

        private bool ContainsCouponCode(IOrderGroup orderGroup, string couponCode)
        {
            return orderGroup.Forms.Any(form => form.CouponCodes.Contains(couponCode));
        }

        private IEnumerable<PromotionData> GetAvailablePromotions()
        {
            var promotions = _promotionEngine.GetEvaluablePromotionsInPriorityOrder(_currentMarket.GetCurrentMarket());

            return promotions;
        }

        //private bool IsCodeReservedForUniqueCoupons(string coupon)
        //{
        //    var allPromotions = GetAvailablePromotions();

        //    var foundPromotion = allPromotions.FirstOrDefault(x => x.Coupon.Code == coupon && x.IsActive);

        //    // determine that the promotion is found and is active
        //    if (foundPromotion != null)
        //    {
        //        // determine that the promotion is reserved for unique coupons
        //        var couponsForPromotion = _couponService.GetByPromotionId(foundPromotion.ContentLink.ID);
        //        var isPromotionReservedForUniqueCoupons = couponsForPromotion != null && couponsForPromotion.Any();

        //        return isPromotionReservedForUniqueCoupons;
        //    }

        //    return false;
        //}

        private bool CheckCouponAvailable(string coupon)
        {
            var allPromotions = GetAvailablePromotions();

            return allPromotions.Any(p => p.IsActive && p.Coupon.Code == coupon);
        }

        private void UpdateStorageFieldForLineItem(IPurchaseOrder purchaseOrder, CustomerContact customerContact = null)
        {
            IEnumerable<ILineItem> lineItems;

            if (purchaseOrder.IsBuyNowCart() && purchaseOrder.GetAllLineItems().Any(x => x.IsInVault()))
            {
                lineItems = purchaseOrder.GetAllLineItems();
            }
            else
            {
                var secondShipment = purchaseOrder.GetSecondShipment();
                if (secondShipment == null || !secondShipment.LineItems.Any()) return;
                lineItems = secondShipment.LineItems;
            }

            var customer = customerContact ?? _customerContext.CurrentContact;

            foreach (var lineItem in lineItems)
            {
                var preciousMetalsVariant = lineItem.GetEntryContent() as PreciousMetalsVariantBase;

                if (preciousMetalsVariant != null) _storageRateHelper.UpdateStorageFieldsFor(lineItem, _storageRateHelper.GetMatchingStorageRateBlock(customer, preciousMetalsVariant));
            }
        }

        #endregion

        #region Initialize Cart on Page
        public List<ShipmentViewModel> GetInitialCart()
        {
            var defaultCartViewModel = GetCartViewModel(_trmCartService.DefaultCartName);
            var bullionCartViewModel = GetCartViewModel(_trmCartService.DefaultBullionCartName);

            return GetShipmentViewModel(defaultCartViewModel, bullionCartViewModel);
        }
        public List<ShipmentViewModel> GetCartViewModel(string cartName)
        {
            var orderGroup = _trmCartService.LoadOrCreateCart(cartName) as IOrderGroup;
            var vm = new LargeOrderGroupViewModel();
            if (orderGroup == null || (!(orderGroup is IPurchaseOrder) && orderGroup.OrderStatus == OrderStatus.Completed))
            {
                vm.Modified = _safeBeginningOfTime;
                return new List<ShipmentViewModel>();
            }

            var totals = _orderGroupCalculator.GetOrderGroupTotals(orderGroup);
            foreach (var shipment in orderGroup.GetFirstForm().Shipments)
            {
                vm.Shipments.Add(CreateShipmentViewModel(orderGroup, shipment, totals));
            }

            return vm.Shipments;
        }
        private List<ShipmentViewModel> GetShipmentViewModel(List<ShipmentViewModel> defaultCartViewModel, List<ShipmentViewModel> bullionCartViewModel)
        {
            var shipments = new List<ShipmentViewModel>();
            shipments.AddRange(defaultCartViewModel);

            if (bullionCartViewModel == null) return shipments;

            if (bullionCartViewModel.Count >= 1)
                bullionCartViewModel[0].ShipmentUniqueId = "BullionCart-Delivered";
            if (bullionCartViewModel.Count >= 2)
                bullionCartViewModel[1].ShipmentUniqueId = "BullionCart-Vaulted";

            shipments.AddRange(bullionCartViewModel);
            return shipments;
        }
        #endregion End - Initialize Cart on Page
    }
}