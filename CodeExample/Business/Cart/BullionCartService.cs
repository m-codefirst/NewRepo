using Castle.Core.Internal;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Core.Internal;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Product.ProductService;
using Hephaestus.Commerce.Shared.Facades;
using Hephaestus.Commerce.Shared.Models;
using Hephaestus.Commerce.Shared.Services;
using log4net;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using NuGet;
using PricingAndTradingService.Models;
using PricingAndTradingService.Models.APIResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Extensions;
using TRM.Shared.Helpers;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Cart;
using TRM.Web.Services;
using static PricingAndTradingService.Models.Constants;
using static TRM.Shared.Constants.StringConstants;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Business.Cart
{
    public class BullionCartService : TrmCartService, ITrmCartService
    {
        private const string Vault = "vault";
        private const string Storage = "storage";
        private const int BuyForDelivery = 1;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(BullionCartService));

        private readonly IBullionTradingService _pampTradingService;
        private readonly IAddressBookService _addressBookService;
        private readonly IKycHelper _kycHelper;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly IAmShippingMethodHelper _shippingMethodHelper;
        private readonly CustomerContext _customerContext;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionCartService(IPricingService pricingService,
            IOrderGroupFactory orderGroupFactory,
            IPlacedPriceProcessor placedPriceProcessor,
            IInventoryProcessor inventoryProcessor,
            ILineItemValidator lineItemValidator,
            IOrderRepository orderRepository,
            IPromotionEngine promotionEngine,
            IAddressBookService addressBookService,
            ICurrentMarket currentMarket,
            ICurrencyService currencyService,
            IAmValidationHelper validationHelper,
            IAmOrderGroupAuditHelper orderGroupAuditHelper,
            IAmReferenceConverter referenceConverter,
            IAmInventoryHelper inventoryHelper,
            ITrmInventoryService trmInventoryService,
            IBullionTradingService pampTradingService,
            IKycHelper kycHelper,
            IBullionPriceHelper bullionPriceHelper,
            IAmShippingMethodHelper shippingMethodHelper,
            CustomerContextFacade customerContextFacade,
            CustomerContext customerContext,
            IPrintzwareHelper printzwareHelper,
            ContentLoader contentLoader, IAmBullionContactHelper bullionContactHelper) :
            base(pricingService, orderGroupFactory, placedPriceProcessor, inventoryProcessor, lineItemValidator,
                orderRepository, promotionEngine, addressBookService, currentMarket, currencyService,
                customerContextFacade, validationHelper, orderGroupAuditHelper, referenceConverter, inventoryHelper,
                trmInventoryService, printzwareHelper, contentLoader, bullionContactHelper)
        {
            _pampTradingService = pampTradingService;
            _addressBookService = addressBookService;
            _kycHelper = kycHelper;
            _customerContext = customerContext;
            _bullionContactHelper = bullionContactHelper;
            _bullionPriceHelper = bullionPriceHelper;
            _shippingMethodHelper = shippingMethodHelper;
            _orderRepository = orderRepository;
            _orderGroupFactory = orderGroupFactory;
        }

        public string DefaultBuyNowCartName => Shared.Constants.DefaultCartNames.DefaultBuyNowCartName;
        public string DefaultBullionCartName => Shared.Constants.DefaultCartNames.DefaultBullionCartName;

        public ICart LoadOrCreateCart(CartItemDto cartItemDto)
        {
            return LoadOrCreateCart(cartItemDto, null, DefaultConstants.IsBullionTrue, null);
            ////check bullion account
            //var currentContact = _customerContext.CurrentContact;

            //var variant = cartItemDto.Code.GetVariantByCode();
            //if (variant is IAmPremiumVariant)
            //{
            //    if (currentContact == null) return LoadOrCreateBullionCart();

            //    if (!_bullionContactHelper.IsBullionAccount(currentContact)) return null;

            //    //Check Kyc address to allow user purchase bullion
            //    if (!_kycHelper.KycCanProceed(currentContact)) return null;

            //    return LoadOrCreateBullionCart();
            //}
            //if (currentContact != null && !_kycHelper.KycCanProceed(currentContact)) return null;

            //return LoadOrCreateCart(DefaultCartName);
        }

        public ICart LoadOrCreateCart(CartItemDto cartItemDto, string name)
        {
            return LoadOrCreateCart(cartItemDto, name, DefaultConstants.IsBullionFalse, null);
            ////check bullion account
            //var currentContact = _customerContext.CurrentContact;

            //var variant = cartItemDto.Code.GetVariantByCode();
            //if (variant is IAmPremiumVariant)
            //{
            //    if (currentContact == null) return LoadOrCreateCart(name);

            //    if (!_bullionContactHelper.IsBullionAccount(currentContact)) return null;

            //    //Check Kyc address to allow user purchase bullion variant
            //    if (!_kycHelper.KycCanProceed(currentContact)) return null;

            //    return LoadOrCreateCart(name);
            //}
            //if (currentContact != null && !_kycHelper.KycCanProceed(currentContact)) return null;

            //return LoadOrCreateCart(DefaultCartName);
        }
        public ICart LoadOrCreateCart(CartItemDto cartItemDto, string name, bool isBullion, CustomerContact customerContact)
        {
            //check bullion account
            //var currentContact = _customerContext.CurrentContact;
            var currentContact = customerContact ?? _customerContext.CurrentContact;
            var variant = cartItemDto.Code.GetVariantByCode();
            if (variant is IAmPremiumVariant)
            {
                if (currentContact == null)
                {
                    return isBullion == true ? LoadOrCreateBullionCart(name, customerContact) : LoadOrCreateCart(name, DefaultConstants.UseValidationCustomPromoSettingFalse, customerContact);
                }

                if (!_bullionContactHelper.IsBullionAccount(currentContact)) return null;

                //Check Kyc address to allow user purchase bullion variant
                if (!_kycHelper.KycCanProceed(currentContact)) return null;

                return isBullion == true ? LoadOrCreateBullionCart(name, customerContact) : LoadOrCreateCart(name, DefaultConstants.UseValidationCustomPromoSettingFalse, customerContact);
            }
            if (currentContact != null && !_kycHelper.KycCanProceed(currentContact)) return null;

            return LoadOrCreateCart(DefaultCartName, DefaultConstants.UseValidationCustomPromoSettingFalse, customerContact);
        }
        //private ICart LoadOrCreateCartCommon(CartItemDto cartItemDto, string name, CustomerContact customerContact = null, bool isBullion = false)
        //{
        //    //check bullion account
        //    var currentContact = customerContact ?? _customerContext.CurrentContact;

        //    var variant = cartItemDto.Code.GetVariantByCode();
        //    if (variant is IAmPremiumVariant)
        //    {
        //        if (currentContact == null)
        //        {
        //            return isBullion == true ? LoadOrCreateBullionCart(name, customerContact) : LoadOrCreateCart(name, false, customerContact);
        //        }

        //        if (!_bullionContactHelper.IsBullionAccount(currentContact)) return null;
        //        //Check Kyc address to allow user purchase bullion variant
        //        if (!_kycHelper.KycCanProceed(currentContact)) return null;

        //        if (isBullion == true)
        //        {
        //            return LoadOrCreateBullionCart(name, customerContact);
        //        }
        //        else
        //        {
        //            return customerContact == null ? LoadOrCreateCart(name, false, customerContact) : LoadOrCreateCart(name, false, customerContact);
        //        }
        //    }
        //    if (currentContact != null && !_kycHelper.KycCanProceed(currentContact)) return null;
        //    return LoadOrCreateCart(DefaultCartName, false, customerContact);
        //}
        public bool CheckMinInvestAmount(CartItemDto cartItemDto)
        {
            var signatureVariant = cartItemDto.Code.GetVariantByCode() as SignatureVariant;

            var minSpend = signatureVariant?.MinSpendConfigs.FirstOrDefault();
            if (minSpend == null) return true;

            return minSpend.Amount <= cartItemDto.InvestmentAmount;
        }

        public bool CheckMinInvestAmount(CustomerContact customerContact)
        {
            var isLtUser = customerContact.GetBooleanProperty(Shared.Constants.StringConstants.CustomFields.IsAutoInvest);
            if (isLtUser == false) return false;

            var bullionCustomerEffectiveBalance = customerContact.GetDecimalProperty(Shared.Constants.StringConstants.CustomFields.BullionCustomerEffectiveBalance);
            var investmentAmount = customerContact.GetDecimalProperty(Shared.Constants.StringConstants.CustomFields.AutoInvestAmount);

            return bullionCustomerEffectiveBalance < investmentAmount;
        }

        protected override ILineItem AddOrUpdateLineItem(ICart cart, CartItemDto itemToAdd, out bool shouldRemovePriceChangeValidationMessage)
        {
            return AddOrUpdateLineItem(cart, itemToAdd, out shouldRemovePriceChangeValidationMessage, null);
        }
        protected override ILineItem AddOrUpdateLineItem(ICart cart, CartItemDto itemToAdd, out bool shouldRemovePriceChangeValidationMessage, CustomerContact customerContact)
        {
            var variant = itemToAdd.Code.GetVariantByCode();
            shouldRemovePriceChangeValidationMessage = true;

            var bullionVariant = variant as IAmPremiumVariant;
            if (bullionVariant == null) return base.AddOrUpdateLineItem(cart, itemToAdd, out shouldRemovePriceChangeValidationMessage, customerContact);

            ResetCartWithPampQuoteResult(cart);

            var lineItem = GetFirstLineItem(cart, itemToAdd.Code);
            if (lineItem == null)
            {
                lineItem = CreateNewCartLineItem(cart, variant, itemToAdd);

                if (variant is IAmInvestmentVariant)
                    lineItem.Properties[CustomFields.BullionSignatureValueRequested] = itemToAdd.InvestmentAmount;

                if (cart.Name.Equals(DefaultBuyNowCartName))
                {
                    AddLineItemToBuyNowCart(cart, lineItem, bullionVariant, customerContact);
                }
                else
                {
                    AddLineItemToBullionCart(cart, lineItem, bullionVariant);
                }
            }
            else
            {
                if (variant is IAmInvestmentVariant)
                    lineItem.Properties[CustomFields.BullionSignatureValueRequested] = itemToAdd.InvestmentAmount;

                var containerShipment = GetShipmentContainLineItem(cart, lineItem.Code);

                if (cart.Name.Equals(DefaultBuyNowCartName))
                {
                    itemToAdd.QuantityMode = eQuantityMode.SetTo;
                    containerShipment.ShippingAddress = null;
                }

                cart.UpdateLineItemQuantity(containerShipment, lineItem, GetItemQuantityToAdd(lineItem, itemToAdd));
            }

            if (cart.Name.Equals(DefaultBullionCartName)) UpdateBullionShippingMethod(cart, lineItem.Code, customerContact);

            return lineItem;
        }

        public bool ChangeShipment(ICart cart, string variantCode)
        {
            //Get cart and find shipment contain line item
            var lineItem = GetFirstLineItem(cart, variantCode);
            if (lineItem == null) return false;

            //Get variant, get line item
            var variant = variantCode.GetVariantByCode();
            if (variant == null) return false;

            var matchShipment = GetShipmentContainLineItem(cart, variantCode);
            if (matchShipment == null) return false;

            var firstShipment = cart.GetFirstShipment();
            var secondShipment = cart.GetSecondShipment();

            if (firstShipment == null || secondShipment == null) return false;

            var bullionVariant = variant as IAmPremiumVariant;
            if (bullionVariant == null) return false;

            //if variant belong first shipment -> check Can Vault to switch to Vault
            if (firstShipment.ShipmentId.Equals(matchShipment.ShipmentId))
            {
                if (bullionVariant.CanDeliverCanVault == CanDeliverCanVault.CanVault || bullionVariant.CanDeliverCanVault == CanDeliverCanVault.Both)
                {
                    var newLineItem = CreateNewBullionCartItem(cart, matchShipment, variant);
                    if (newLineItem == null) return false;

                    AddLineItemToVault(cart, newLineItem);

                    UpdateBullionShippingMethod(cart, newLineItem.Code);
                    ValidateCart(cart, false);
                    _orderRepository.Save(cart);
                    return true;
                }
            }
            else if (secondShipment.ShipmentId.Equals(matchShipment.ShipmentId))
            {
                var contact = _customerContext.CurrentContact;
                var isSippCustomer = _bullionContactHelper.IsSippContact(contact);

                //if variant belong second shipment -> check Can Delivery to switch to Delivery 
                // or customer is not sipp customer
                if (!isSippCustomer && bullionVariant.CanDeliverCanVault == CanDeliverCanVault.CanDeliver || bullionVariant.CanDeliverCanVault == CanDeliverCanVault.Both)
                {
                    var newLineItem = CreateNewBullionCartItem(cart, matchShipment, variant);
                    if (newLineItem == null) return false;

                    AddLineItemToDelivery(cart, newLineItem);

                    UpdateBullionShippingMethod(cart, newLineItem.Code);
                    ValidateCart(cart, false);
                    _orderRepository.Save(cart);
                    return true;
                }
            }

            //Return result
            return false;
        }

        public void ValidateVaultShipment(ICart cart)
        {
            var updateCart = false;
            try
            {
                foreach (var shipment in cart.GetFirstForm().Shipments)
                {
                    var shippingMethod = Mediachase.Commerce.Orders.Managers.ShippingManager.GetShippingMethod(shipment.ShippingMethodId)
                        ?.ShippingMethod
                        ?.FirstOrDefault()
                        ?.GetShippingMethodParameterRows()
                        ?.FirstOrDefault(x => x.Parameter == "ObsMethodName")
                        ?.Value;

                    var shippingMethodName = !string.IsNullOrWhiteSpace(shippingMethod) ? shippingMethod.ToLower() : string.Empty;
                    if (shippingMethodName.Contains(Vault) || shippingMethodName.Contains(Storage))
                    {
                        foreach (var lineItem in shipment.LineItems)
                        {
                            var lineItemType = lineItem.GetIntegerProperty(CustomFields.BullionDeliver);
                            if (lineItemType == BuyForDelivery)
                            {
                                // Update shipping method Id to correct Delivery type
                                lineItem.Properties[CustomFields.BullionDeliver] = (int)BullionDeliver.Vault;

                                updateCart = true;
                            }
                        }
                    }
                }
                if (updateCart == true) _orderRepository.Save(cart);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private ILineItem CreateNewBullionCartItem(ICart cart, IShipment matchShipment, TrmVariant variant)
        {
            var oldLineItem = RemoveLineItemFromBullionCart(cart, matchShipment.ShipmentId, variant.Code);
            if (oldLineItem == null) return null;

            var newLineItem = CreateNewCartLineItem(cart, variant, new CartItemDto { Code = oldLineItem.Code, Quantity = (int)oldLineItem.Quantity });
            newLineItem.CopyPropertiesFrom(oldLineItem);

            var lineItemCalculatedAmount = newLineItem as ILineItemCalculatedAmount;
            if (lineItemCalculatedAmount != null) lineItemCalculatedAmount.IsSalesTaxUpToDate = false;

            return newLineItem;
        }

        public ICart LoadOrCreateBullionCart()
        {
            return LoadOrCreateBullionCart(DefaultBullionCartName);

        }
        public ICart LoadOrCreateBullionCart(string name, CustomerContact customerContact = null)
        {
            var cartName = string.IsNullOrWhiteSpace(name) ? DefaultBullionCartName : name;
            var cart = LoadOrCreateCart(cartName, DefaultConstants.UseValidationCustomPromoSettingFalse, customerContact);
            var firstForm = cart.GetFirstForm() ?? _orderGroupFactory.CreateOrderForm(cart);

            var firstShipment = cart.GetFirstShipment();
            if (firstShipment == null)
            {
                firstShipment = _orderGroupFactory.CreateShipment(cart);
                firstForm.Shipments.Add(firstShipment);
            }

            var secondShipment = firstForm.Shipments.Count >= 2 ? firstForm.Shipments.ElementAt(1) : null;
            if (secondShipment == null)
            {
                secondShipment = _orderGroupFactory.CreateShipment(cart);
                firstForm.Shipments.Add(secondShipment);
            }
            var kycAddress = GetKycAddressModel(customerContact);

            if (firstShipment != null && firstShipment.ShippingAddress == null && kycAddress != null)
            {
                firstShipment.ShippingAddress = _addressBookService.ConvertToAddress(kycAddress, cart);
            }

            if (secondShipment != null && secondShipment.ShippingAddress == null)
            {
                secondShipment.ShippingAddress = _addressBookService.ConvertToAddress(GetDefaultShippingAddressForVault(), cart);
                ValidateCart(cart, customerContact);
            }

            return cart;
        }

        private void AddLineItemToBuyNowCart(ICart cart, ILineItem lineItem, IAmPremiumVariant variant, CustomerContact customerContact)
        {
            RemoveAllLineItemFromCart(cart, customerContact);

            if (variant.CanDeliverCanVault.Equals(CanDeliverCanVault.CanVault) ||
                variant.CanDeliverCanVault.Equals(CanDeliverCanVault.Both))
            {
                lineItem.Properties[CustomFields.BullionDeliver] = (int)BullionDeliver.Vault;
            }
            else if (variant.CanDeliverCanVault.Equals(CanDeliverCanVault.CanDeliver) ||
                     variant.CanDeliverCanVault.Equals(CanDeliverCanVault.Both))
            {
                lineItem.Properties[CustomFields.BullionDeliver] = (int)BullionDeliver.Deliver;
            }

            cart.AddLineItem(lineItem, _orderGroupFactory);
            var shipment = cart.GetFirstShipment();
            shipment.ShippingAddress = null;
        }

        private void AddLineItemToBullionCart(ICart cart, ILineItem lineItem, IAmPremiumVariant variant)
        {
            if (variant.CanDeliverCanVault.Equals(CanDeliverCanVault.CanVault) ||
                variant.CanDeliverCanVault.Equals(CanDeliverCanVault.Both))
            {
                AddLineItemToVault(cart, lineItem);
            }
            else if (variant.CanDeliverCanVault.Equals(CanDeliverCanVault.CanDeliver) ||
                     variant.CanDeliverCanVault.Equals(CanDeliverCanVault.Both))
            {
                AddLineItemToDelivery(cart, lineItem);
            }
        }

        private void AddLineItemToVault(ICart cart, ILineItem lineItem)
        {
            var shipments = cart.GetFirstForm().Shipments;
            if (shipments == null || shipments.Count < 2) return;

            lineItem.Properties[CustomFields.BullionDeliver] = (int)BullionDeliver.Vault;

            var vaultShipment = shipments.LastOrDefault();
            vaultShipment?.LineItems.Add(lineItem);
        }

        private void AddLineItemToDelivery(ICart cart, ILineItem lineItem)
        {
            if (cart?.GetFirstShipment() == null || lineItem == null) return;

            lineItem.Properties[CustomFields.BullionDeliver] = (int)BullionDeliver.Deliver;

            cart.AddLineItem(lineItem, _orderGroupFactory);
        }

        private IShipment GetShipmentContainLineItem(IOrderGroup cart, string lineItemCode)
        {
            return cart.GetFirstForm()?.Shipments?.FirstOrDefault(x => x.LineItems.Any(y => y.Code.Equals(lineItemCode)));
        }

        private void UpdateBullionShippingMethod(IOrderGroup cart, string lineItemCode, CustomerContact customerContact = null)
        {
            var shipment = GetShipmentContainLineItem(cart, lineItemCode);
            _shippingMethodHelper.UpdateBullionShippingMethod(cart, shipment?.ShipmentId, customerContact);
        }

        private AddressModel GetKycAddressModel(CustomerContact customerContact = null)
        {
            //GetKycAddress
            var contact = customerContact ?? _customerContext.CurrentContact;
            if (contact == null) return null;
            var kycAddress = _bullionContactHelper.GetBullionAddress(contact);
            if (kycAddress == null) return null;

            var kycAddressModel = new AddressModel();
            _addressBookService.MapToModel(kycAddress, kycAddressModel, customerContact);
            return kycAddressModel;
        }

        public AddressModel GetDefaultShippingAddressForVault()
        {
            return new AddressModel
            {
                AddressId = "ED66116F-CC75-40C2-AD9D-7792A340FDA9",
                Name = DefaultVaultShippingAddressName,
                CountryCode = "GBR"
            };
        }

        public AddressModel GetDefaultShippingAddressForDelivery()
        {
            var model = new AddressModel();
            var currentContact = _customerContext.CurrentContact;
            var contactAddress = _bullionContactHelper.GetBullionAddress(currentContact);
            if (contactAddress == null) return null;

            _addressBookService.MapToModel(contactAddress, model);
            return model;
        }

        public override bool RemoveItemFromCart(RemoveFromCartDto removeFromCart)
        {
            try
            {
                var variant = removeFromCart.Code.GetVariantByCode();
                var bullionVariant = variant as IAmPremiumVariant;
                if (bullionVariant == null) return base.RemoveItemFromCart(removeFromCart);

                var cart = LoadCart(DefaultBullionCartName);
                if (cart == null) return false;

                var shipment = GetShipmentContainLineItem(cart, removeFromCart.Code);
                if (shipment == null) return false;

                RemoveLineItemFromBullionCart(cart, shipment.ShipmentId, removeFromCart.Code);
                _shippingMethodHelper.UpdateBullionShippingMethod(cart, shipment.ShipmentId);
                _orderRepository.Save(cart);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public void RemoveCookieForEmptyMixedCart()
        {
            var defaultCart = LoadCart(DefaultCartName);
            var bullionCart = LoadCart(DefaultBullionCartName);

            var isEmptyDefaultCart = defaultCart == null || !defaultCart.GetAllLineItems().Any();
            var isEmptyBullionCart = bullionCart == null || !bullionCart.GetAllLineItems().Any();

            if (isEmptyDefaultCart && isEmptyBullionCart)
            {
                CookieHelper.RemoveCookie(StringConstants.MarketCookieName);
            }
        }

        private ILineItem RemoveLineItemFromBullionCart(ICart cart, int shipmentId, string code, CustomerContact customerContact = null)
        {
            var shipment = cart.GetFirstForm().Shipments.First(s => s.ShipmentId == shipmentId);

            var lineItem = shipment.LineItems.FirstOrDefault(l => l.Code == code);
            if (lineItem != null)
            {
                shipment.LineItems.Remove(lineItem);
            }

            ValidateCart(cart, customerContact);
            return lineItem;
        }

        private void RemoveAllLineItemFromCart(ICart cart, CustomerContact customerContact)
        {
            if (cart == null) return;
            var allLineItemCodes = cart.GetAllLineItems().Select(x => x.Code).ToList();

            foreach (var itemCode in allLineItemCodes)
            {
                ChangeCartItem(cart, 0, itemCode, 0, null, null, customerContact);
            }
            _orderRepository.Save(cart);
        }

        #region Process PAMP Quote

        public SyncWithPampQuoteResult SyncAndAdjustCartWithPampQuote(string cartName)
        {
            var cart = LoadCart(cartName);
            if (cart == null)
            {
                Logger.ErrorFormat("Cannot load the cart with name {0}", cartName);
                return SyncWithPampQuoteResult.InvalidCart;
            }
            return SyncAndAdjustCartWithPampQuote(cart);
        }

        public SyncWithPampQuoteResult SyncAndAdjustCartWithPampQuote(ICart cart, bool isRecalculate = false)
        {
            return SyncAndAdjustCartWithPampQuote(null, cart, isRecalculate);
        }
        public SyncWithPampQuoteResult SyncAndAdjustCartWithPampQuote(CustomerContact customerContact, ICart cart, bool isRecalculate = false)
        {
            try
            {
                //reset pamp quote result in cart
                ResetCartWithPampQuoteResult(cart);

                // first quote request                               
                Dictionary<ILineItem, IAmPremiumVariant> dictLineItemVariants = GetMappingLineItemWithPremiumVariants(cart);
                if (dictLineItemVariants.IsNullOrEmpty()) return SyncWithPampQuoteResult.InvalidCart;

                var adjustedLineItems = dictLineItemVariants.Select(x => new BullionAdjustedLineItemModel(x.Key, x.Value)).ToList();

                QuoteResponse successQuoteResponse;
                var processQuoteSuccess = SendAndProcessThePampQuote(cart, adjustedLineItems, out successQuoteResponse, customerContact);
                if (!processQuoteSuccess || successQuoteResponse == null) return SyncWithPampQuoteResult.RejectedOrTimeOut;

                if (!IsNeedToProcessAnotherQuote(adjustedLineItems) || isRecalculate)
                {
                    UpdateCartWithRecalculatedInfoFromPampQuote(cart, adjustedLineItems);
                    StoreQuoteResponseInfoToCart(cart, successQuoteResponse);
                    ForceSaleTaxIsOutOfDate(cart);
                    ValidateCart(customerContact, cart);
                    _orderRepository.Save(cart);
                    return SyncWithPampQuoteResult.Successfully;
                }

                // second quote request
                processQuoteSuccess = SendAndProcessThePampQuote(cart, adjustedLineItems, out successQuoteResponse, customerContact);
                if (!processQuoteSuccess || successQuoteResponse == null) return SyncWithPampQuoteResult.RejectedOrTimeOut;
                //if (IsNeedToProcessAnotherQuote(adjustedLineItems)) return SyncWithPampQuoteResult.NotEnoughMoney;

                UpdateCartWithRecalculatedInfoFromPampQuote(cart, adjustedLineItems);
                StoreQuoteResponseInfoToCart(cart, successQuoteResponse);
                ForceSaleTaxIsOutOfDate(cart);
                ValidateCart(customerContact, cart);
                _orderRepository.Save(cart);
                return SyncWithPampQuoteResult.Successfully;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return SyncWithPampQuoteResult.RejectedOrTimeOut;
            }
        }

        private void ForceSaleTaxIsOutOfDate(IOrderGroup cart)
        {
            if (cart == null) return;
            foreach (var lineItem in cart.GetAllLineItems())
            {
                var lineItemCalculatedAmount = lineItem as ILineItemCalculatedAmount;
                if (lineItemCalculatedAmount != null) lineItemCalculatedAmount.IsSalesTaxUpToDate = false;
            }

            IOrderGroupCalculatedAmount orderGroupCalculatedAmount;
            if ((orderGroupCalculatedAmount = (cart as IOrderGroupCalculatedAmount)) != null)
            {
                orderGroupCalculatedAmount.IsTaxTotalUpToDate = false;
            }
        }

        private void ResetCartWithPampQuoteResult(ICart cart)
        {
            cart.Properties[CustomFields.UpdatedWithPampQuote] = bool.FalseString;
            var allLineItems = cart.GetAllLineItems();
            foreach (var item in allLineItems)
            {
                item.Properties[CustomFields.BullionNeedAdjustTotalPrice] = bool.FalseString;
                item.Properties[CustomFields.BullionAdjustedTotalPrice] = string.Empty;
                item.Properties[CustomFields.PampMetalPricePerOneOz] = string.Empty;
                item.Properties[CustomFields.BullionAdjustedTotalPriceIncludePremiums] = string.Empty;
                item.Properties[CustomFields.BullionAdjustedInvestmentQuantity] = null;
            }
            _orderRepository.Save(cart);
        }

        private void UpdateCartWithRecalculatedInfoFromPampQuote(ICart cart, List<BullionAdjustedLineItemModel> adjustedLineItems)
        {
            cart.Properties[CustomFields.UpdatedWithPampQuote] = bool.TrueString;
            foreach (var item in adjustedLineItems)
            {
                item.LineItem.Properties[CustomFields.BullionNeedAdjustTotalPrice] = item.NeedToAdjustTheTotalPrice.ToString();
                item.LineItem.Properties[CustomFields.BullionAdjustedTotalPrice] = item.AdjustedTotalPrice;
                item.LineItem.Properties[CustomFields.PampMetalPricePerOneOz] = item.PampMetalPricePerOneOz;
                if (item.NeedToAdjustTheTotalPrice)
                {
                    item.LineItem.Properties[CustomFields.BullionAdjustedTotalPriceIncludePremiums] = item.AdjustTotalPriceIncludePremium;
                }
                if (item.IsInvestmentItem && item.DeductedQuantityTotal > 0)
                {
                    var newQuantity = item.LineItem.Quantity - item.DeductedQuantityTotal;
                    item.LineItem.Quantity = newQuantity;
                    item.LineItem.Properties[CustomFields.BullionAdjustedInvestmentQuantity] = newQuantity;
                }
            }
        }

        private bool IsNeedToProcessAnotherQuote(List<BullionAdjustedLineItemModel> adjustedLineItems)
        {
            return adjustedLineItems.Any(x => x.NeedToDeduct);
        }

        private bool SendAndProcessThePampQuote(ICart cart, List<BullionAdjustedLineItemModel> adjustedLineItemModels, out QuoteResponse successQuoteResponse, CustomerContact customerContact)
        {
            successQuoteResponse = null;
            try
            {
                //TODO: Convert to Metal Quantities by grouping Metal of all kinds of variants( Bar, Coin and Signature)
                var metalQuantities = ExtractMetalQuantities(adjustedLineItemModels, cart.Currency).ToList();

                var quoteResponse = _pampTradingService.RequestPampForQuote(customerContact, metalQuantities);
                if (quoteResponse == null || quoteResponse.MetalPriceMap.IsNullOrEmpty())
                {
                    return false;
                }
                successQuoteResponse = quoteResponse;

                var metalPriceMap = _pampTradingService.GetPampPriceForMetalsFromQuoteResponse(quoteResponse);
                var metalTypePricePerOneOzMappings = _pampTradingService.GetPampPricePerOneOzForMetals(metalQuantities, metalPriceMap);

                //  Store PampQuote Placed Price
                foreach (var item in adjustedLineItemModels)
                {
                    item.PampMetalPricePerOneOz = metalTypePricePerOneOzMappings[item.MetaType];
                }

                // Adjust line items in cart with the update prices returned from Pamp Quote
                ValidateAndRecalculateLineItemQuantityOz(adjustedLineItemModels, metalPriceMap, metalTypePricePerOneOzMappings, customerContact);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        private IEnumerable<MetalQuantity> ExtractMetalQuantities(List<BullionAdjustedLineItemModel> adjustedLineItemModels, Currency currency)
        {
            return adjustedLineItemModels.GroupBy(x => x.MetaType).Select(
                     gr => new MetalQuantity
                     {
                         Currency = currency,
                         Metal = gr.Key,
                         QuantityInOz = gr.Sum(item => item.QuantityInOz)
                     });
        }

        private Dictionary<ILineItem, IAmPremiumVariant> GetMappingLineItemWithPremiumVariants(ICart cart)
        {
            var premiums = cart.GetAllLineItems()
                .Select(x => new
                {
                    premiumVariant = x.GetEntryContent() as IAmPremiumVariant,
                    lineItem = x
                })
                .Where(y => y.premiumVariant != null).ToList();

            return premiums.IsNullOrEmpty() ? new Dictionary<ILineItem, IAmPremiumVariant>() : premiums.ToDictionary(y => y.lineItem, y => y.premiumVariant);
        }

        private void ValidateAndRecalculateLineItemQuantityOz(
            List<BullionAdjustedLineItemModel> adjustedLineItemModels,
            Dictionary<MetalType, decimal> quoteResponseMetalPriceMap,
            Dictionary<MetalType, decimal> metalTypePricePerOzMappings,
            CustomerContact customerContact)
        {
            foreach (var metaPriceMap in quoteResponseMetalPriceMap)
            {
                MetalType metalType = metaPriceMap.Key;
                var pricePerOz = metalTypePricePerOzMappings[metalType];
                List<BullionAdjustedLineItemModel> lineItemModelsInThisMetalType = adjustedLineItemModels.Where(x => x.MetaType.Equals(metalType)).ToList();

                AdjustTotalPrice(lineItemModelsInThisMetalType, pricePerOz, metaPriceMap.Value);

                ApplyBuyPremiumsForAdjustedItems(lineItemModelsInThisMetalType.Where(x => x.NeedToAdjustTheTotalPrice), customerContact);

                RecalculateSignatureVariant(lineItemModelsInThisMetalType.FirstOrDefault(x => x.VariantType == BullionVariantType.Signature), pricePerOz, customerContact);

            }
        }

        private void ApplyBuyPremiumsForAdjustedItems(IEnumerable<BullionAdjustedLineItemModel> adjustedItems, CustomerContact customerContact)
        {
            var bullionAdjustedLineItemModels = adjustedItems as BullionAdjustedLineItemModel[] ?? adjustedItems.ToArray();
            foreach (var item in bullionAdjustedLineItemModels)
            {
                if (!item.NeedToAdjustTheTotalPrice || item.AdjustedTotalPrice == decimal.Zero) { continue; }
                item.AdjustTotalPriceIncludePremium = ApplyPremiums(item, customerContact);
            }
        }

        private decimal ApplyPremiums(BullionAdjustedLineItemModel item, CustomerContact customerContact)
        {
            if (item.IsInvestmentItem)
            {
                var totalInvestIncludePremium = item.LineItem.GetPropertyValue<decimal>(CustomFields.BullionSignatureValueRequested);

                return _bullionPriceHelper.GetSignaturePricePerOneOzIncludedPremium(customerContact, item.Variant, totalInvestIncludePremium, item.AdjustedTotalPrice);
            }
            return _bullionPriceHelper.ApplyBuyPremiumsForPhysicalItem(customerContact, item.AdjustedTotalPrice, item.Variant, item.LineItem.Quantity, true);

        }

        private void RecalculateSignatureVariant(BullionAdjustedLineItemModel signatureLineItemModel, decimal pricePerOz, CustomerContact customerContact = null)
        {
            if (signatureLineItemModel == null) return;
            var totalInvestmentWithoutPremium = signatureLineItemModel.NeedToAdjustTheTotalPrice
                ? signatureLineItemModel.AdjustedTotalPrice
                : Math.Round(signatureLineItemModel.QuantityInOz * pricePerOz, 2);

            var totalInvestIncludePremium = signatureLineItemModel.LineItem.GetPropertyValue<decimal>(CustomFields.BullionSignatureValueRequested);
            var totalInvestmentIncludedPremiumWithPampPrice = _bullionPriceHelper.GetSignaturePricePerOneOzIncludedPremium(customerContact, signatureLineItemModel.Variant, totalInvestIncludePremium, totalInvestmentWithoutPremium);
            var quantityInOzBeforAdjusting = signatureLineItemModel.QuantityInOz;

            signatureLineItemModel.NeedToDeduct = totalInvestIncludePremium < totalInvestmentIncludedPremiumWithPampPrice;
            if (signatureLineItemModel.NeedToDeduct)
            {
                signatureLineItemModel.DeductQuantity(totalInvestIncludePremium, totalInvestmentIncludedPremiumWithPampPrice, quantityInOzBeforAdjusting);
            }
        }

        private void AdjustTotalPrice(
            List<BullionAdjustedLineItemModel> lineItemModelsInThisMetalType,
            decimal pricePerOz,
            decimal totalPrice)
        {
            // Reset all line item as "do not need to adjust the total price"
            UpdateLineItemModelWithOptionAdjustTotalPrice(lineItemModelsInThisMetalType, false);
            var variantTypeQuantities = ExtractVariantTypeQuantities(lineItemModelsInThisMetalType).ToList();

            decimal[] arrayDistributedTotalPriceForThisMetalType = DistributePriceWithRounding(totalPrice, pricePerOz, variantTypeQuantities.Select(x => x.QuantityInOz).ToArray());

            Dictionary<BullionVariantType, decimal> variantTypePriceMap = variantTypeQuantities
                .ToDictionary(x => x.Type, x => arrayDistributedTotalPriceForThisMetalType[variantTypeQuantities.IndexOf(x)]);
            foreach (var variantTypePrice in variantTypePriceMap)
            {
                var lineItemModelsInThisType = lineItemModelsInThisMetalType.Where(x => x.VariantType == variantTypePrice.Key).ToList();
                var lastItem = lineItemModelsInThisType.Last();
                decimal[] arrayDistributedTotalPriceForThisVariantyType = DistributePriceWithRounding(variantTypePrice.Value, pricePerOz, lineItemModelsInThisType.Select(x => x.QuantityInOz).ToArray());
                if (IsNeedToAdjustTotalPrice(lastItem, pricePerOz, variantTypePrice.Value))
                {
                    UpdateLineItemModelWithOptionAdjustTotalPrice(new[] { lastItem }, true);
                    lastItem.AdjustedTotalPrice = arrayDistributedTotalPriceForThisVariantyType.Last();
                }
            }
        }

        private bool IsNeedToAdjustTotalPrice(BullionAdjustedLineItemModel lineItemModel, decimal pricePerOz, decimal totalPrice)
        {
            return Math.Round(lineItemModel.QuantityInOz * pricePerOz, 2) != totalPrice;
        }

        private decimal[] DistributePriceWithRounding(decimal totalPrice, decimal pricePerOz, decimal[] quantityInOzArray)
        {
            var totalQuantityInOz = quantityInOzArray.Sum();
            if (Equals(totalQuantityInOz * pricePerOz, totalPrice))
            {
                return quantityInOzArray.Select(x => Math.Round(x * pricePerOz, 2)).ToArray();
            }

            List<decimal> priceMap = new List<decimal>();
            var idx = 0;
            var lastItemIdx = quantityInOzArray.Length - 1;
            var currentTotalPrice = decimal.Zero;
            foreach (var item in quantityInOzArray)
            {
                if (idx == lastItemIdx)
                {
                    priceMap.Add(totalPrice - currentTotalPrice);
                    return priceMap.ToArray();
                }
                var price = Math.Round(pricePerOz * item, 2);
                idx++;
                currentTotalPrice += price;
                priceMap.Add(price);
            }
            return priceMap.ToArray();
        }

        private IEnumerable<BullionVariantTypeQuantity> ExtractVariantTypeQuantities(List<BullionAdjustedLineItemModel> lineItemModelsInThisMetalType)
        {
            return lineItemModelsInThisMetalType.GroupBy(x => x.VariantType).Select(
                  gr => new BullionVariantTypeQuantity
                  {
                      Type = gr.Key,
                      QuantityInOz = gr.Sum(item => item.QuantityInOz)
                  });
        }

        private void UpdateLineItemModelWithOptionAdjustTotalPrice(IEnumerable<BullionAdjustedLineItemModel> lineItemModelsInThisMetalType, bool needToAdjustTotalPrice)
        {
            foreach (var item in lineItemModelsInThisMetalType)
            {
                item.NeedToAdjustTheTotalPrice = needToAdjustTotalPrice;
            }
        }

        private void StoreQuoteResponseInfoToCart(ICart cart, QuoteResponse quoteResponse)
        {
            cart.Properties[CustomFields.BullionRequestQuotationId] = quoteResponse.QuoteId;
            cart.Properties[CustomFields.BullionPAMPRequestQuoteId] = quoteResponse.QuoteDtoId.ToString();
            cart.Properties[CustomFields.BullionPAMPQuoteId] = quoteResponse.QuoteId;
            cart.Properties[CustomFields.BullionPAMPStatus] = quoteResponse.Result.Success
                ? PampExecuteOnQuoteStatus.Success.ToString()
                : PampExecuteOnQuoteStatus.Tentative.ToString();
            cart.Properties[CustomFields.Currency] = cart.Currency.ToString();
        }

        #endregion

        #region Bullion Restrictions

        protected override Dictionary<ILineItem, List<ValidationIssue>> ValidateBullionRestrictions(ICart cart)
        {
            return ValidateBullionRestrictions(cart, null);
        }
        protected override Dictionary<ILineItem, List<ValidationIssue>> ValidateBullionRestrictions(ICart cart, CustomerContact customerContact)
        {
            var listResults = new Dictionary<ILineItem, List<ValidationIssue>>();

            var currentContact = customerContact ?? _customerContext.GetContactById(cart.CustomerId);

            if (currentContact == null) return listResults;

            var unableToPurchaseBullion = currentContact.GetBooleanProperty(CustomFields.BullionUnableToPurchaseBullion);
            var isSippCustomer = _bullionContactHelper.IsSippContact(currentContact);

            if (!isSippCustomer && !unableToPurchaseBullion) return listResults;

            var deliverResults = new Dictionary<ILineItem, List<ValidationIssue>>();

            if (isSippCustomer) RemoveItemAllDeliveryItems(cart, out deliverResults, customerContact);

            listResults.AddRange(deliverResults);

            var lineItemsCollection = cart.GetAllLineItems().ToList();

            var bullionResults = new Dictionary<ILineItem, List<ValidationIssue>>();

            foreach (var lineItem in lineItemsCollection)
            {
                ValidateLineItemBullionRestrictions(cart, lineItem, isSippCustomer, out bullionResults, unableToPurchaseBullion, customerContact);
            }

            listResults.AddRange(bullionResults);
            return listResults;
        }

        private void RemoveItemAllDeliveryItems(ICart cart, out Dictionary<ILineItem, List<ValidationIssue>> results, CustomerContact customerContact = null)
        {
            results = new Dictionary<ILineItem, List<ValidationIssue>>();

            var deliveryShipment = cart.GetFirstShipment();
            var secondShipment = cart.GetSecondShipment();
            if (deliveryShipment != null && secondShipment != null && deliveryShipment.LineItems.Any())
            {
                results.AddRange(deliveryShipment.LineItems.ToList().ToDictionary(x => x, x => RemoveItemByBullionRestrictions(cart, x, deliveryShipment.ShipmentId, customerContact)));
            }
        }

        private void ValidateLineItemBullionRestrictions(ICart cart, ILineItem lineItem, bool isSippCustomer, out Dictionary<ILineItem, List<ValidationIssue>> results,
            bool unableToPurchaseBullion, CustomerContact customerContact = null)
        {
            results = new Dictionary<ILineItem, List<ValidationIssue>>();

            var variant = lineItem.GetEntryContent();
            var entryContent = variant as TrmVariant;

            var shipment = GetShipmentContainLineItem(cart, lineItem.Code);
            if (shipment == null) return;

            if (entryContent != null && entryContent.IsConsumerProducts && isSippCustomer)
                results.TryAdd(lineItem, RemoveItemByBullionRestrictions(cart, lineItem, shipment.ShipmentId, customerContact));

            var metalVariant = variant as PreciousMetalsVariantBase;

            if (metalVariant != null && (unableToPurchaseBullion || !metalVariant.CanPensionBuy))
                results.TryAdd(lineItem, RemoveItemByBullionRestrictions(cart, lineItem, shipment.ShipmentId, customerContact));

            var physicalVariant = variant as PhysicalVariantBase;

            if (physicalVariant != null && !physicalVariant.CanVault)
                results.TryAdd(lineItem, RemoveItemByBullionRestrictions(cart, lineItem, shipment.ShipmentId, customerContact));
        }

        private List<ValidationIssue> RemoveItemByBullionRestrictions(ICart cart, ILineItem lineItem, int shipmentId, CustomerContact customerContact = null)
        {
            RemoveLineItemFromBullionCart(cart, shipmentId, lineItem.Code, customerContact);
            return new List<ValidationIssue> { ValidationIssue.RemovedDueToNotAvailableInMarket };
        }

        #endregion

        public IEnumerable<RewardDescription> ApplyPromotionsToCart(IOrderGroup orderGroup, Dictionary<ILineItem, List<ValidationIssue>> validationIssues = null)
        {
            return ApplyPromotionsToBullionCart(orderGroup, validationIssues);
        }

    }
}