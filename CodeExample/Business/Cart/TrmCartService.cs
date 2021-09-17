using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Marketing.Promotions;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Core;
using EPiServer.Core.Internal;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Cart.Extensions;
using Hephaestus.Commerce.Cart.Services;
using Hephaestus.Commerce.Product.ProductService;
using Hephaestus.Commerce.Shared.Facades;
using Hephaestus.Commerce.Shared.Services;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Extensions;
using TRM.Shared.Helpers;
using TRM.Shared.Interfaces;
using TRM.Web.Business.Promotions;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Cart;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Services;
using Hephaestus.Commerce.Extensions;
using CustomFields = TRM.Shared.Constants.StringConstants.CustomFields;
using Enums = TRM.Web.Constants.Enums;

namespace TRM.Web.Business.Cart
{
    public class TrmCartService : CartService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderGroupFactory _orderGroupFactory;

        private readonly CustomerContextFacade _customerContext;
        private readonly ICurrentMarket _currentMarket;
        private readonly ICurrencyService _currencyService;
        private readonly IAmValidationHelper _validationHelper;
        private readonly IAmOrderGroupAuditHelper _orderGroupAuditHelper;
        private readonly IAmReferenceConverter _referenceConverter;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly ITrmInventoryService _trmInventoryService;
        private readonly IPrintzwareHelper _printzwareHelper;
        private readonly IPromotionEngine _promotionEngine;
        private readonly ILineItemValidator _lineItemValidator;
        private readonly IInventoryProcessor _inventoryProcessor;
        private readonly IPlacedPriceProcessor _placedPriceProcessor;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly ContentLoader _contentLoader;

        public TrmCartService(IPricingService pricingService, IOrderGroupFactory orderGroupFactory, IPlacedPriceProcessor placedPriceProcessor,
            IInventoryProcessor inventoryProcessor, ILineItemValidator lineItemValidator, IOrderRepository orderRepository, IPromotionEngine promotionEngine,
            IAddressBookService addressBookService, ICurrentMarket currentMarket, ICurrencyService currencyService, CustomerContextFacade customerContext,
            IAmValidationHelper validationHelper, IAmOrderGroupAuditHelper orderGroupAuditHelper, IAmReferenceConverter referenceConverter,
            IAmInventoryHelper inventoryHelper, ITrmInventoryService trmInventoryService, IPrintzwareHelper printzwareHelper,
            ContentLoader contentLoader,
            IAmBullionContactHelper bullionContactHelper)
            : base(pricingService, orderGroupFactory, placedPriceProcessor, inventoryProcessor, lineItemValidator,
                  orderRepository, promotionEngine, addressBookService, currentMarket, currencyService, customerContext)
        {
            _orderRepository = orderRepository;
            _customerContext = customerContext;
            _currentMarket = currentMarket;
            _currencyService = currencyService;
            _orderGroupFactory = orderGroupFactory;
            _validationHelper = validationHelper;
            _orderGroupAuditHelper = orderGroupAuditHelper;
            _referenceConverter = referenceConverter;
            _inventoryHelper = inventoryHelper;
            _trmInventoryService = trmInventoryService;
            _promotionEngine = promotionEngine;
            _printzwareHelper = printzwareHelper;
            _contentLoader = contentLoader;
            _lineItemValidator = lineItemValidator;
            _inventoryProcessor = inventoryProcessor;
            _placedPriceProcessor = placedPriceProcessor;
            _bullionContactHelper = bullionContactHelper;
        }

        public ICart LoadOrCreateCart(string name, MarketId marketId)
        {
            var cart = _orderRepository.LoadOrCreateCart<ICart>(_customerContext.CurrentContactId, name, _currentMarket);
            if (cart != null)
                SetCartCurrency(cart, _currencyService.GetCurrentCurrency());
            return cart;
        }

        public bool AddItemWithQuantity(ICart cart, CartItemDto itemToAdd, out Dictionary<ILineItem, List<ValidationIssue>> warningMessages, bool shouldAudit = true)
        {
            return AddItemWithQuantity(null, cart, itemToAdd, out warningMessages, shouldAudit);
        }
        public bool AddItemWithQuantity(CustomerContact customerContact, ICart cart, CartItemDto itemToAdd, out Dictionary<ILineItem, List<ValidationIssue>> warningMessages, bool shouldAudit = true)
        {
            warningMessages = new Dictionary<ILineItem, List<ValidationIssue>>();

            if (!_validationHelper.IsValid(itemToAdd))
            {
                warningMessages.Add(new LineItem { Code = itemToAdd.Code }, new List<ValidationIssue> { ValidationIssue.RemovedDueToCodeMissing });
                return false;
            }

            if (!CanAddItem(cart, itemToAdd.Code))
            {
                warningMessages.Add(new LineItem { Code = itemToAdd.Code }, new List<ValidationIssue> { ValidationIssue.RemovedDueToUnavailableItem });
                return false;
            }

            bool shouldRemovePriceChangeValidationMessage;
            var lineItem = AddOrUpdateLineItem(cart, itemToAdd, out shouldRemovePriceChangeValidationMessage, customerContact);

            warningMessages = ValidateCart(customerContact, cart, shouldAudit);

            //Remove the price change validation message as the price always changes
            if (shouldRemovePriceChangeValidationMessage)
                RemovePlacedPricedChangedValidationIssue(warningMessages, lineItem.Code);

            if (warningMessages.Count > 0) return false;

            return GetFirstLineItem(cart, itemToAdd.Code) != null;
        }

        public bool SetSubscription(ICart cart, UpdateSubscribedItemDto subscribedItem)
        {
            if (!_validationHelper.IsValid(subscribedItem)) return false;

            var lineItem = GetFirstLineItem(cart, subscribedItem.Code);
            if (lineItem == null) return false;

            lineItem.Properties["Subscribed"] = subscribedItem.Subscribe;

            if (GetLineItem(cart.GetFirstShipment(), lineItem) == null)
            {
                return false;
            }

            _orderRepository.Save(cart);
            return true;
        }

        public bool TryApplyCoupon(ICart cart, string couponCode)
        {
            var rewardDescriptions = cart.Name.Equals(DefaultCartName)
                ? cart.ApplyDiscounts()
                : ApplyPromotionsToBullionCart(cart);

            var appliedCoupons = rewardDescriptions.Where(r => r.Status == FulfillmentStatus.Fulfilled).Select(c =>
                string.IsNullOrWhiteSpace(c.Promotion.Coupon.Code) ? c.Promotion.Name : c.Promotion.Coupon.Code);
            return appliedCoupons.Any(c => c.Equals(couponCode, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<RewardDescription> ApplyPromotionsToBullionCart(IOrderGroup orderGroup, Dictionary<ILineItem, List<ValidationIssue>> validationIssues = null)
        {
            var rewardDescriptions = _promotionEngine.Run(orderGroup, new PromotionEngineSettings
            {
                ApplyReward = false
            }).ToList();

            CreateAndAddBullionGiftPromotionItems(orderGroup, rewardDescriptions, validationIssues);
            UpdateBullionLineItemDiscountAndSaleTax(orderGroup, rewardDescriptions);
            UpdateShippingDiscount(orderGroup, rewardDescriptions);

            return rewardDescriptions;
        }

        private void CreateAndAddBullionGiftPromotionItems(IOrderGroup orderGroup, List<RewardDescription> rewardDescriptions, Dictionary<ILineItem, List<ValidationIssue>> validationIssues)
        {
            var shipment = orderGroup.GetFirstShipment();
            var invalidReward = new List<RewardDescription>();

            foreach (var reward in rewardDescriptions)
            {
                IList<ContentReference> giftItems;
                switch (reward.Promotion)
                {
                    case BuyItemsGetAFreeGift buyItemsGetAFreeGift:
                        giftItems = buyItemsGetAFreeGift.GiftItems;
                        break;
                    case SpendAmountGetGiftItems spendAmountGetGiftItems:
                        giftItems = spendAmountGetGiftItems.GiftItems;
                        break;
                    default:
                        continue;
                }

                if (giftItems != null)
                {
                    foreach (var giftItem in giftItems)
                    {
                        var variant = _contentLoader.Get<TrmVariant>(giftItem);
                        if (variant == null) continue;

                        var giftLineItem = _orderGroupFactory.CreateLineItem(variant.Code, orderGroup);
                        if (giftLineItem == null) continue;

                        giftLineItem.DisplayName = variant.DisplayName;

                        if (!orderGroup.IsConsumerCart() && !(variant is IAmPremiumVariant)
                            || orderGroup.IsConsumerCart() && variant is IAmPremiumVariant)
                        {
                            invalidReward.Add(reward);
                        }
                        if (orderGroup.IsConsumerCart()) continue;

                        giftLineItem.IsGift = true;
                        giftLineItem.Quantity = reward.Redemptions.Count();

                        var discountValue = giftLineItem.PlacedPrice * giftLineItem.Quantity;
                        giftLineItem.SetEntryDiscountValue(discountValue);
                        if (shipment.LineItems.All(x => x.LineItemId != giftLineItem.LineItemId))
                        {
                            shipment.LineItems.Add(giftLineItem);
                        }

                        if (validationIssues != null && validationIssues.Any())
                        {
                            validationIssues.RemoveAll(x => x.Key.Code == variant.Code &&
                                                            x.Value.All(i => i == ValidationIssue.PlacedPricedChanged));
                        }
                    }
                }
            }
            invalidReward.ForEach(x => rewardDescriptions.Remove(x));
        }

        private void UpdateBullionLineItemDiscountAndSaleTax(IOrderGroup orderGroup, IEnumerable<RewardDescription> rewardDescriptions)
        {
            var lineItemsWithPromotionsApplied = rewardDescriptions
                .Where(x => x.Promotion.DiscountType == DiscountType.LineItem)
                .SelectMany(x => x.Redemptions.SelectMany(d => d.AffectedEntries.SavedAmountPerCode())).ToList();

            var allLineItems = orderGroup.GetAllLineItems();

            var needToSaveOrderGroup = false;
            foreach (var lineItem in allLineItems)
            {
                var newEntryDiscount = lineItemsWithPromotionsApplied.Where(x => x.Key == lineItem.Code).Sum(x => x.Value);
                if (lineItem.GetPropertyValue<decimal>(CustomFields.LineItemEntryDiscountAmount) == newEntryDiscount) continue;

                lineItem.Properties[CustomFields.LineItemEntryDiscountAmount] = newEntryDiscount;
                // Update vat
                ILineItemCalculatedAmount lineItemCalculatedAmount = lineItem as ILineItemCalculatedAmount;
                if (lineItemCalculatedAmount != null)
                {
                    lineItemCalculatedAmount.IsSalesTaxUpToDate = false;
                }
                needToSaveOrderGroup = true;
            }

            if (needToSaveOrderGroup)
            {
                IOrderGroupCalculatedAmount orderGroupCalculatedAmount;
                if ((orderGroupCalculatedAmount = (orderGroup as IOrderGroupCalculatedAmount)) != null)
                {
                    orderGroupCalculatedAmount.IsTaxTotalUpToDate = false;
                }
                _orderRepository.Save(orderGroup);
            }
        }

        private void UpdateShippingDiscount(IOrderGroup orderGroup, List<RewardDescription> rewardDescriptions)
        {
            var shipment = orderGroup.GetFirstShipment();

            var shippingRewardDescriptions = rewardDescriptions
                .Where(x => x.Promotion.DiscountType == DiscountType.Shipping);

            shipment.SetShipmentDiscount(shippingRewardDescriptions.Sum(x => x.SavedAmount));
        }

        public virtual bool RemoveItemFromCart(RemoveFromCartDto removeFromCart)
        {
            var cart = LoadCart(DefaultCartName);
            if (cart == null) return false;

            var shipment = cart.GetFirstShipment();

            var lineItem = cart.GetAllLineItems().FirstOrDefault(x => x.Code == removeFromCart.Code && GetPwOrderId(x) == removeFromCart.PWOrderId);

            if (lineItem != null)
            {
                shipment.LineItems.Remove(lineItem);
            }
            if (!shipment.LineItems.Any())
            {
                cart.GetFirstForm().Shipments.Remove(shipment);
            }

            ValidateCart(cart);
            _orderRepository.Save(cart);

            return true;
        }

        protected virtual ILineItem AddOrUpdateLineItem(ICart cart, CartItemDto itemToAdd, out bool shouldRemovePriceChangeValidationMessage)
        {
            return this.AddOrUpdateLineItem(cart, itemToAdd, out shouldRemovePriceChangeValidationMessage, null);
        }
        protected virtual ILineItem AddOrUpdateLineItem(ICart cart, CartItemDto itemToAdd, out bool shouldRemovePriceChangeValidationMessage, CustomerContact customerContact)
        {
            var lineItem = GetFirstLineItem(cart, itemToAdd.Code, itemToAdd.PWOrderId);

            var variant = itemToAdd.Code.GetVariantByCode();

            if (lineItem == null)
            {
                lineItem = CreateNewCartLineItem(cart, variant, itemToAdd);

                cart.AddLineItem(lineItem, _orderGroupFactory);

                shouldRemovePriceChangeValidationMessage = true;
            }
            else
            {
                var firstShipment = cart.GetFirstShipment();

                cart.UpdateLineItemQuantity(firstShipment, lineItem, GetItemQuantityToAdd(lineItem, itemToAdd));

                shouldRemovePriceChangeValidationMessage = false;
            }

            return lineItem;
        }

        protected ILineItem CreateNewCartLineItem(IOrderGroup cart, TrmVariant variant, CartItemDto itemToAdd)
        {
            var lineItem = _orderGroupFactory.CreateLineItem(itemToAdd.Code, cart);
            lineItem.DisplayName = variant.DisplayName;
            lineItem.Quantity = GetItemQuantityToAdd(lineItem, itemToAdd); // investigate => calculate qty

            lineItem.Properties[CustomFields.MandatoryFieldName] = variant.RecurrenceType == Shared.Constants.Enums.eRecurrenceType.Mandatory;

            if (!string.IsNullOrWhiteSpace(itemToAdd.PWOrderId))
            {
                SetLineItemPersonalisationProperties(lineItem, variant, itemToAdd);
            }

            return lineItem;
        }

        private void SetLineItemPersonalisationProperties(ILineItem lineItem, TrmVariant personalisedItem, CartItemDto itemToAdd)
        {
            var pwEpiserverImageId = _printzwareHelper.GetThumb(itemToAdd.PWOrderId);
            lineItem.Properties[CustomFields.PersonalisationUniqueId] = itemToAdd.PWOrderId;
            lineItem.Properties[CustomFields.PersonalisationImageId] = pwEpiserverImageId;
            lineItem.Properties[CustomFields.PersonalisationCharge] = personalisedItem?.PersonalisationPrice;
            lineItem.Properties[CustomFields.PersonalisationType] = personalisedItem?.PersonalisationType;
        }

        protected decimal GetItemQuantityToAdd(ILineItem lineItem, CartItemDto itemToAdd)
        {
            return itemToAdd.QuantityMode == Enums.eQuantityMode.AddToCurrent
                ? lineItem.Quantity + itemToAdd.Quantity
                : itemToAdd.Quantity;
        }

        protected ILineItem GetFirstLineItem(IOrderGroup cart, string code, string pwOrderId = null)
        {
            return string.IsNullOrEmpty(pwOrderId) ? cart.GetAllLineItems().FirstOrDefault(x => x.Code == code && string.IsNullOrEmpty(GetPwOrderId(x)))
                : cart.GetAllLineItems().FirstOrDefault(x => x.Code == code && GetPwOrderId(x) == pwOrderId);
        }

        public string GetPwOrderId(ILineItem lineItem)
        {
            return lineItem.Properties[CustomFields.PersonalisationUniqueId] as string;
        }

        private ILineItem GetLineItem(IShipment shipment, ILineItem lineItem)
        {
            var theLineItem = shipment.LineItems.FirstOrDefault(li => li.LineItemId == lineItem.LineItemId);

            return theLineItem;
        }

        private bool CanAddItem(ILineItem lineItem)
        {
            var variant = lineItem.GetEntryContent();

            var merchandising = variant as IControlMyMerchandising;
            var inventory = variant as IControlInventory;

            return merchandising == null || (merchandising.PublishOntoSite && merchandising.Sellable && !(inventory?.OffSale ?? false));
        }

        private bool CanAddItem(ICart cart, string code)
        {
            var lineItem = cart.CreateLineItem(code, _orderGroupFactory);

            return CanAddItem(lineItem);
        }

        private void RemovePlacedPricedChangedValidationIssue(Dictionary<ILineItem, List<ValidationIssue>> warningMessages, string lineItemCode)
        {
            if (warningMessages.Any(m => m.Key.Code == lineItemCode))
            {
                var messages = warningMessages.FirstOrDefault(m => m.Key.Code == lineItemCode);

                warningMessages[messages.Key].Remove(ValidationIssue.PlacedPricedChanged);

                if (!warningMessages[messages.Key].Any())
                {
                    warningMessages.Remove(messages.Key);
                }
            }
        }

        #region Validate Cart

        public Dictionary<ILineItem, List<ValidationIssue>> ValidateCart(ICart cart, bool shouldAudit = true)
        {
            return this.ValidateCart(null, cart, shouldAudit);
        }
        public Dictionary<ILineItem, List<ValidationIssue>> ValidateCart(CustomerContact customerContact, ICart cart, bool shouldAudit = true)
        {
            var validationResults = new Dictionary<ILineItem, List<ValidationIssue>>();

            //consumer validation
            if (cart.Name.Equals(DefaultCartName))
            {
                validationResults.AddRange(ValidateMerchandising(cart, customerContact));
                validationResults.AddRange(ValidateOutsourcingInventory(cart, customerContact));
                validationResults.AddRange(ValidatePersonalisationCost(cart));
                validationResults.AddRange(base.ValidateCart(cart, customerContact));

                //Perform coupon validation after base so discounts have already been applied
                validationResults.AddRange(RemoveInvalidCouponCodes(cart));
            }
            else
            {
                //other validation (bullion....)
                validationResults.AddRange(ValidateCustomerCurrency(cart, customerContact));
                validationResults.AddRange(ValidateBullionRestrictions(cart, customerContact));

                //Work-around for custom promotion engine
                validationResults.AddRange(OverrideHephaestusValidateCartForBullion(cart, customerContact));

                //Perform coupon validation after base so discounts have already been applied
                validationResults.AddRange(RemoveInvalidCouponCodes(cart));
            }

            if (!validationResults.Any() || !shouldAudit) return validationResults;

            _orderRepository.Save(cart);

            //record validation results
            foreach (var validationResult in validationResults)
            {
                var result = $"{validationResult.Key.DisplayName}:";
                foreach (var issue in validationResult.Value)
                {
                    result += $"{issue},";
                }

                _orderGroupAuditHelper.WriteAudit(cart, "Validation Results", result);
            }

            return validationResults;
        }

        private Dictionary<ILineItem, List<ValidationIssue>> OverrideHephaestusValidateCartForBullion(ICart cart, CustomerContact customerContact = null)
        {
            if (cart == null || cart.Name.Equals(DefaultWishListName) || cart.GetFirstShipment() == null ||
                !cart.Forms.SelectMany(x => x.Shipments).SelectMany(x => x.LineItems).Any()) return new Dictionary<ILineItem, List<ValidationIssue>>();

            var validationIssues = new Dictionary<ILineItem, List<ValidationIssue>>();
            cart.ValidateOrRemoveLineItems((item, issue) => validationIssues.AddValidationIssues(item, issue), _lineItemValidator);
            cart.UpdatePlacedPriceOrRemoveLineItems(customerContact ?? _customerContext.GetContactById(cart.CustomerId), (item, issue) => validationIssues.AddValidationIssues(item, issue), _placedPriceProcessor);
            cart.UpdateInventoryOrRemoveLineItems((item, issue) => validationIssues.AddValidationIssues(item, issue), _inventoryProcessor);
            ApplyPromotionsToBullionCart(cart, validationIssues);
            return validationIssues;
        }

        /// <summary>
        /// A message is displayed and the add to basket button is removed/disabled (pending designs)
        /// based on the Customers Currency. 
        /// If Contact.Currency = GBP then allow add to basket and checkout for Consumer Variants
        /// If Contact.Currency != GBP then don't allow add to basket & show an appropriate message. 
        /// Existing items in a basket should check for Currency when validating cart,
        /// to ensure incorrect items are cleaned up for existing baskets. 
        /// </summary>
        /// <see>
        ///     <cref>https://maginus.atlassian.net/browse/BULL-192</cref>
        /// </see>
        /// <param name="cart"></param>
        private Dictionary<ILineItem, List<ValidationIssue>> ValidateCustomerCurrency(ICart cart, CustomerContact customerContact = null)
        {
            var results = new Dictionary<ILineItem, List<ValidationIssue>>();

            var currentContact = customerContact ?? _customerContext.GetContactById(cart.CustomerId);
            if (currentContact == null) return results;

            if (string.IsNullOrEmpty(currentContact.PreferredCurrency)
                || currentContact.PreferredCurrency.Equals(Currency.GBP)) return results;

            var shipmentsCollection = cart.GetFirstForm().Shipments.ToList();

            foreach (var shipment in shipmentsCollection)
            {
                var lineItemsCollection = shipment.LineItems.ToList();

                foreach (var lineItem in lineItemsCollection)
                {
                    var entryContent = lineItem.GetEntryContent() as TrmVariant;
                    if (entryContent != null && entryContent.IsConsumerProducts)
                    {
                        _orderGroupAuditHelper.WriteAudit(cart, "Removed Due To Not Available In Market", lineItem.DisplayName);
                        ChangeCartItem(cart, shipment.ShipmentId, lineItem.Code, 0M, string.Empty, string.Empty, customerContact);
                        results.Add(lineItem, new List<ValidationIssue>
                        {
                            ValidationIssue.RemovedDueToNotAvailableInMarket
                        });
                    }
                }
            }

            return results;
        }

        private Dictionary<ILineItem, List<ValidationIssue>> ValidateOutsourcingInventory(ICart cart, CustomerContact customerContact = null)
        {
            var results = new Dictionary<ILineItem, List<ValidationIssue>>();
            var listOfAllItemsWithOutSourcedInventory = new List<OutSourcedCartItemsDto>();

            foreach (var shipment in cart.GetFirstForm().Shipments)
            {
                foreach (var lineItem in shipment.LineItems)
                {
                    var outSourceDto = _trmInventoryService.GetOutSourcedAndSourceCodes(lineItem.Code);
                    var baseCodeToUse = outSourceDto.IsOutSourced ? outSourceDto.BaseCodeIfOutSourced : outSourceDto.CodeToUse;

                    var dtoToAdd = new OutSourcedCartItemsDto
                    {
                        CodeToUse = outSourceDto.CodeToUse,
                        BaseCodeIfOutSourced = outSourceDto.BaseCodeIfOutSourced,
                        Quantity = shipment.LineItems.Where(li => li.Code == baseCodeToUse).Sum(li => li.Quantity)
                    };

                    listOfAllItemsWithOutSourcedInventory.Add(dtoToAdd);
                }

                foreach (var listGroupedByBaseCode in listOfAllItemsWithOutSourcedInventory.GroupBy(t => t.CodeToUse))
                {
                    if (listGroupedByBaseCode.Count() == 1) continue;
                    var sumOfItemsByBaseCode = listGroupedByBaseCode.Sum(t => t.Quantity);
                    var baseCodeRef = _referenceConverter.GetContentLink(listGroupedByBaseCode.Key);
                    var availableQuantity = _inventoryHelper.GetStockSummary(baseCodeRef, customerContact).TotalAvailable;
                    var quantityToRemove = sumOfItemsByBaseCode - availableQuantity;
                    if (sumOfItemsByBaseCode <= availableQuantity) continue;
                    {
                        foreach (var baseCodeThatNeedsChecking in listGroupedByBaseCode.OrderByDescending(t => t.BaseCodeIfOutSourced))
                        {
                            var allLineItemsWithThisBaseCode =
                                shipment.LineItems.Where(li => li.Code == baseCodeThatNeedsChecking.BaseCodeIfOutSourced).ToList();
                            allLineItemsWithThisBaseCode.AddRange(shipment.LineItems.Where(li => li.Code == baseCodeThatNeedsChecking.CodeToUse).ToList());

                            foreach (var lineItem in allLineItemsWithThisBaseCode)
                            {
                                var howManyWeCanRemoveFromThisLine = quantityToRemove >= lineItem.Quantity ? lineItem.Quantity : quantityToRemove;
                                if (howManyWeCanRemoveFromThisLine > 0)
                                {
                                    var quantityToSet = lineItem.Quantity - howManyWeCanRemoveFromThisLine;
                                    if (quantityToSet > 0)
                                    {
                                        results.Add(lineItem, new List<ValidationIssue> { ValidationIssue.AdjustedQuantityByAvailableQuantity });
                                        cart.UpdateLineItemQuantity(shipment, lineItem, quantityToSet);
                                    }
                                    else
                                    {
                                        results.Add(lineItem, new List<ValidationIssue> { ValidationIssue.RemovedDueToUnavailableItem });
                                        ChangeCartItem(cart, 0, lineItem.Code, 0M, null, null, customerContact);
                                        _orderRepository.Save(cart);
                                    }

                                    quantityToRemove = quantityToRemove - howManyWeCanRemoveFromThisLine;
                                    if (quantityToRemove <= 0)
                                        break;
                                }
                                if (quantityToRemove <= 0)
                                    break;
                            }
                        }
                    }
                }
            }

            return results;
        }

        private Dictionary<ILineItem, List<ValidationIssue>> ValidateMerchandising(ICart cart, CustomerContact customerContact = null)
        {

            var results = new Dictionary<ILineItem, List<ValidationIssue>>();

            var shipmentsCollection = cart.GetFirstForm().Shipments.ToList();

            foreach (var shipment in shipmentsCollection)
            {
                var lineItemsCollection = shipment.LineItems.ToList();

                foreach (var lineItem in lineItemsCollection)
                {
                    if (CanAddItem(lineItem)) continue;

                    _orderGroupAuditHelper.WriteAudit(cart, "Item Removed Due to Merchandising", lineItem.DisplayName);
                    ChangeCartItem(cart, shipment.ShipmentId, lineItem.Code, 0M, string.Empty, string.Empty, customerContact);
                    results.Add(lineItem, new List<ValidationIssue> { ValidationIssue.RemovedDueToUnavailableItem });
                }
            }

            return results;

        }

        public Dictionary<ILineItem, List<ValidationIssue>> ValidatePersonalisationCost(ICart cart)
        {
            var results = new Dictionary<ILineItem, List<ValidationIssue>>();

            var shipmentsCollection = cart.GetFirstForm().Shipments.ToList();

            foreach (var shipment in shipmentsCollection)
            {
                var lineItemsCollection = shipment.LineItems.ToList();

                foreach (var lineItem in lineItemsCollection)
                {
                    var hasBeenPersonalised = lineItem.Properties[CustomFields.PersonalisationUniqueId]?.ToString() != null;
                    if (!hasBeenPersonalised) continue;

                    var variant = lineItem.GetEntryContent();
                    var personalisedItem = variant as TrmVariant;

                    if (lineItem.Properties[CustomFields.PersonalisationCharge]?.ToString() == personalisedItem?.PersonalisationPrice) continue;

                    lineItem.Properties[CustomFields.PersonalisationCharge] = personalisedItem?.PersonalisationPrice;

                    results.Add(lineItem, new List<ValidationIssue> { ValidationIssue.PlacedPricedChanged });
                }
            }

            return results;
        }

        private Dictionary<ILineItem, List<ValidationIssue>> RemoveInvalidCouponCodes(ICart cart)
        {
            var results = new Dictionary<ILineItem, List<ValidationIssue>>();

            var orderForm = cart.GetFirstForm();

            //We have no items so don't validate the coupons
            if (orderForm == null) return results;
            if (!cart.GetAllLineItems().Any())
            {
                orderForm.CouponCodes.Clear();
                return results;
            }

            var coupons = orderForm.CouponCodes.ToList();

            foreach (var coupon in coupons)
            {
                if (TryApplyCoupon(cart, coupon)) continue;

                var fakeItem = new LineItem { DisplayName = coupon };
                _orderGroupAuditHelper.WriteAudit(cart, "Coupon not valid", coupon);
                results.Add(fakeItem, new List<ValidationIssue> { ValidationIssue.RemovedDueToUnavailableItem });
                orderForm.CouponCodes.Remove(coupon);
            }

            return results;
        }

        protected virtual Dictionary<ILineItem, List<ValidationIssue>> ValidateBullionRestrictions(ICart cart)
        {
            return new Dictionary<ILineItem, List<ValidationIssue>>();
        }
        protected virtual Dictionary<ILineItem, List<ValidationIssue>> ValidateBullionRestrictions(ICart cart, CustomerContact customerContact)
        {
            return new Dictionary<ILineItem, List<ValidationIssue>>();
        }

        public ICart LoadCart(string name, bool isMiniBasket = false)
        {
            if (!name.Equals(DefaultCartName))
            {
                var cart = _orderRepository.LoadCart<ICart>(_customerContext.CurrentContactId, name, _currentMarket);
                if (cart != null)
                {
                    SetCartCurrency(cart, _currencyService.GetCurrentCurrency());
                }

                if (isMiniBasket)
                {
                    if (OverrideHephaestusValidateCartForBullion(cart).Any())
                    {
                        _orderRepository.Save(cart);
                    }
                }

                return cart;

            }
            return base.LoadCart(name);
        }

        public ICart LoadOrCreateCart(string name, bool useValidationWithCustomPromotionEngineSetting = false, CustomerContact customerContact = null)
        {
            if (!name.Equals(DefaultCartName))
            {
                var currentContentId = _customerContext.CurrentContactId;
                Currency currentCurrency = null;
                if (customerContact != null)
                {
                    currentContentId = (Guid)customerContact.PrimaryKeyId;
                    currentCurrency = _currencyService.GetCurrentCurrency(_bullionContactHelper.GetDefaultCurrencyCode(customerContact));
                }
                else
                {
                    currentCurrency = _currencyService.GetCurrentCurrency();
                }

                var cart = customerContact != null ? _orderRepository.LoadOrCreateCart<ICart>(currentContentId, name, customerContact) : _orderRepository.LoadOrCreateCart<ICart>(currentContentId, name, _currentMarket);
                if (cart != null)
                    SetCartCurrency(cart, currentCurrency, customerContact);
                if (OverrideHephaestusValidateCartForBullion(cart, customerContact).Any())
                {
                    _orderRepository.Save(cart);
                }

                return cart;

            }
            return base.LoadOrCreateCart(name, customerContact);
        }

        #endregion
    }
}