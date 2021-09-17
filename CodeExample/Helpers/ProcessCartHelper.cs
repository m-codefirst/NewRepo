using EPiServer;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Shared.Models;
using Hephaestus.Core.Business.Attributes;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TRM.Shared.Extensions;
using TRM.Web.Business.Cart;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Cart;
using TRM.Web.Models.Pages;
using TRM.Web.Models.Pages.Bullion;
using TRM.Web.Models.ViewModels.Bullion.QuickCheckout;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Services;
using TRM.Web.Services.AutoInvest;
using TRM.Web.Services.Coupons;
using TRM.Web.Services.Metapack;
using TRM.Web.Services.Metapack.Extensions;
using TRM.Web.Services.Metapack.Models.Request;
using TRM.Web.Services.Portfolio;
using static TRM.Shared.Constants.StringConstants;
using Constant = PricingAndTradingService.Models.Constants;
using DefaultCartNames = TRM.Shared.Constants.DefaultCartNames;

namespace TRM.Web.Helpers
{
    public class ProcessCartHelper : IProcessCartHelper
    {
        private readonly EPiServer.Logging.ILogger _logger = EPiServer.Logging.LogManager.GetLogger(typeof(ProcessCartHelper));
        private const bool SentEmailConfirmationFalse = false;
        private const bool SentEmailConfirmationTrue = true;
        private readonly LocalizationService _localizationService;
        private readonly IContentLoader _contentLoader;
        private readonly ITrmCartService _cartService;
        private readonly IOrderRepository _orderRepository;
        private readonly IAmCartHelper _cartHelper;
        private readonly IAddressBookService _addressBookService;
        private readonly IAmShippingMethodHelper _shippingMethodHelper;
        private readonly IAmInventoryHelper _inventoryHelper;

        private readonly IAmOrderHelper _orderHelper;
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IGlobalPurchaseLimitService _globalPurchaseLimitService;
        private readonly IBullionPortfolioService _bullionPortfolioService;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IBullionEmailHelper _bullionEmailHelper;
        private readonly IAutoPurchaseMailingService _autoPurchaseMailingService;
        private readonly ICouponService _couponService;
        private readonly IMetapackShippingService _shippingService;
        private readonly Lazy<IBullionTradingService> _pampTradingService = new Lazy<IBullionTradingService>(() => ServiceLocator.Current.GetInstance<IBullionTradingService>());

        public ProcessCartHelper(LocalizationService localizationService,
            IContentLoader contentLoader,
            ITrmCartService cartService,
            IOrderRepository orderRepository,
            IAmCartHelper cartHelper,
            IAddressBookService addressBookService,
            IAmShippingMethodHelper shippingMethodHelper,
            IAmInventoryHelper inventoryHelper,
            IAmOrderHelper orderHelper,
            IOrderGroupCalculator orderGroupCalculator,
            IAmBullionContactHelper bullionContactHelper,
            IGlobalPurchaseLimitService globalPurchaseLimitService,
            IBullionPortfolioService bullionPortfolioService,
            IAmTransactionHistoryHelper transactionHistoryHelper,
            IBullionEmailHelper bullionEmailHelper,
            IAutoPurchaseMailingService autoPurchaseMailingService,
            IMetapackShippingService shippingService,
            ICouponService couponService)
        {
            _localizationService = localizationService;
            _contentLoader = contentLoader;
            _cartService = cartService;
            _orderRepository = orderRepository;
            _cartHelper = cartHelper;
            _addressBookService = addressBookService;
            _shippingMethodHelper = shippingMethodHelper;
            _inventoryHelper = inventoryHelper;

            _orderHelper = orderHelper;
            _orderGroupCalculator = orderGroupCalculator;
            _bullionContactHelper = bullionContactHelper;
            _globalPurchaseLimitService = globalPurchaseLimitService;
            _bullionPortfolioService = bullionPortfolioService;
            _transactionHistoryHelper = transactionHistoryHelper;
            _bullionEmailHelper = bullionEmailHelper;
            _autoPurchaseMailingService = autoPurchaseMailingService;
            _couponService = couponService;
            _shippingService = shippingService;
        }

        public HttpResponseJsonResult AddToCart(CartItemDto addToCart, bool isBullion = false)
        {
            var response = AddToCart(addToCart, isBullion, null, null);
            return new HttpResponseJsonResult
            {
                Data = new CartResponseDto
                {
                    Success = response != null ? response.Success : false,
                    Title = response != null ? response.Title : _localizationService.GetStringByCulture(StringResources.CartValidationTitle, StringConstants.TranslationFallback.CartValidationTitle, ContentLanguage.PreferredCulture),
                    Message = response != null ? response.Message : _localizationService.GetStringByCulture(StringResources.CartValidationCouldNotLoadOrCreate, "Cart could not be loaded or created", ContentLanguage.PreferredCulture),
                }
            };
        }
        public AddToCartResponse AddToCart(CartItemDto addToCart, bool isBullion, string cartName, CustomerContact customerContact)
        {
            if (string.IsNullOrWhiteSpace(cartName))
            {
                cartName = isBullion == true ? _cartService.DefaultBullionCartName : _cartService.DefaultBuyNowCartName;
            }
            var cart = _cartService.LoadOrCreateCart(addToCart, cartName, isBullion, customerContact);
            if (cart == null) return new AddToCartResponse
            {
                Success = false,
                Title = _localizationService.GetStringByCulture(StringResources.CartValidationTitle, StringConstants.TranslationFallback.CartValidationTitle, ContentLanguage.PreferredCulture),
                Message = _localizationService.GetStringByCulture(StringResources.CartValidationCouldNotLoadOrCreate, "Cart could not be loaded or created", ContentLanguage.PreferredCulture),
                Status = Enums.AutoInvestUpdateOrderStatus.CartCouldNotLoad
            };

            var checkMinInvestAmount = _cartService.CheckMinInvestAmount(addToCart);
            if (!checkMinInvestAmount)
            {
                var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
                var signaturePage = _contentLoader.Get<BullionSignatureLandingPage>(startPage?.BullionSignatureLandingPage);
                var underMinimumSpendAmountMessage = (signaturePage != null) ? signaturePage.UnderMinimumSpentAmountMessage?.ToString() : "Under minimum spend amount";

                return new AddToCartResponse
                {
                    Success = false,
                    Title = _localizationService.GetStringByCulture(StringResources.CartValidationTitle, StringConstants.TranslationFallback.CartValidationTitle, ContentLanguage.PreferredCulture),
                    Message = _localizationService.GetStringByCulture(StringResources.MinSpendAmountValidation, underMinimumSpendAmountMessage, ContentLanguage.PreferredCulture),
                    Status = Enums.AutoInvestUpdateOrderStatus.UnderMiniSpend
                };
            }

            addToCart.QuantityMode = Enums.eQuantityMode.AddToCurrent;

            Dictionary<ILineItem, List<ValidationIssue>> msgs;
            var result = _cartService.AddItemWithQuantity(customerContact, cart, addToCart, out msgs);
            if (result) _orderRepository.Save(cart);

            var validationResults = GetValidationResults(msgs);
            var resultMsg = GetResultMessage(validationResults);

            return new AddToCartResponse
            {
                Cart = cart,
                Success = !(validationResults.Count > 0) && result,
                Title = _localizationService.GetStringByCulture(StringResources.CartValidationTitle, StringConstants.TranslationFallback.CartValidationTitle, ContentLanguage.PreferredCulture),
                Message = resultMsg,
                Status = Enums.AutoInvestUpdateOrderStatus.Success
            };
        }
        public Dictionary<ILineItem, List<ValidationIssue>> GetValidationResults(Dictionary<ILineItem, List<ValidationIssue>> msgs)
        {
            var validationResults = new Dictionary<ILineItem, List<ValidationIssue>>();

            if (!msgs.Any()) return validationResults;

            foreach (var msg in msgs)
            {
                if (validationResults.Any(r => r.Key == msg.Key))
                {
                    validationResults.First(m => m.Key == msg.Key).Value.AddRange(msg.Value);
                }
                else
                {
                    validationResults.Add(msg.Key, msg.Value);
                }
            }

            return validationResults;
        }
        public string GetResultMessage(Dictionary<ILineItem, List<ValidationIssue>> validationResults)
        {
            return validationResults.Any() ? _cartHelper.GetValidationMessages(validationResults) :
                _localizationService.GetStringByCulture(
                    StringResources.CartValidationAddedToBasket,
                    StringConstants.TranslationFallback.CartValidationAddedToBasket,
                    ContentLanguage.PreferredCulture);
        }

        public BuyNowResponse BuyNow(ICart cart, string orderNumberPrefix, DeliveryOption deliveryOption, CustomerContact customerContact = null)
        {
            var response = new BuyNowResponse();

            //validate cart items
            var cartValidationMessages = string.Empty;

            if (!_cartHelper.ValidateCart(cart, out cartValidationMessages))
            {
                response.IsValidateCartError = true;
                response.Message = cartValidationMessages;
                response.Status = Enums.AutoInvestUpdateOrderStatus.Error;
                return response;
            }

            //Update default shipping address
            var deliver = deliveryOption == DeliveryOption.Delivered;
            var addressModel = UpdateShippingAddress(cart, deliver);
            if (addressModel == null)
            {
                response.IsUpdateShippingAddressError = true;
                response.Message = Enums.CheckoutIssue.MissingShippingAddress.GetDescriptionAttribute();
                response.Status = Enums.AutoInvestUpdateOrderStatus.MissingShippingAddress;
                return response;
            }

            //Update default shipping method
            var updatedShipping = UpdateShippingMethod(cart, addressModel);
            if (!updatedShipping)
            {
                response.IsUpdateShippingMethodError = true;
                response.Message = Enums.CheckoutIssue.MissingDefaultShippingMethod.GetDescriptionAttribute();
                response.Status = Enums.AutoInvestUpdateOrderStatus.MissingDefaultShippingMethod;
                return response;
            }
            if (_cartHelper.HasRetryWithPampExportTransaction())
            {
                response.HasRetryWithPampExportTransactionError = true;
                response.Message = Enums.CheckoutIssue.HasPendingExportTransaction.GetDescriptionAttribute();
                response.Status = Enums.AutoInvestUpdateOrderStatus.HasPendingExportTransaction;
                return response;
            }

            //set shipping message for items
            _inventoryHelper.SetItemShippingMessages(cart);

            //log inventory 
            _inventoryHelper.LogStockQuantity(cart, _logger, $"{Shared.Constants.StringConstants.CustomFields.BeforeText} AdjustInventoryOrRemoveLineItems");

            var validationIssues = new Dictionary<ILineItem, List<ValidationIssue>>();
            cart.AdjustInventoryOrRemoveLineItems((item, issue) => validationIssues.Add(item, new List<ValidationIssue> { issue }));

            //log inventory  
            _inventoryHelper.LogStockQuantity(cart, _logger, $"{Shared.Constants.StringConstants.CustomFields.BeforeText} AdjustInventoryOrRemoveLineItems");

            //Update default payment method
            var result = _cartHelper.ProcessPaymentForBullionCart(cart, orderNumberPrefix)?.results?.FirstOrDefault();

            if (result == null || !result.IsSuccessful)
            {
                var message = Enums.CheckoutIssue.CanNotProcessPayment.GetDescriptionAttribute();
                if (!string.IsNullOrEmpty(result?.Message))
                {
                    message = _localizationService.GetStringByCulture(result.Message, message,
                        ContentLanguage.PreferredCulture);
                }
                if (result.Message.ToLower().Contains("insufficientfund"))
                {
                    response.Status = Enums.AutoInvestUpdateOrderStatus.InsufficientFunds;
                }
                else
                {
                    response.Status = Enums.AutoInvestUpdateOrderStatus.Error;
                }
                response.IsProcessPaymentError = true;
                response.Message = message;
                return response;
            }
            else
            {
                response.Success = true;
                response.Status = Enums.AutoInvestUpdateOrderStatus.Success;

                //log inventory 
                _inventoryHelper.LogStockQuantity(cart, _logger, $"{Shared.Constants.StringConstants.CustomFields.AfterPaymentText}");
            }

            return response;
        }
        public AddressModel UpdateShippingAddress(IOrderGroup cart, bool deliver = false)
        {
            var addressModel = deliver ? _cartService.GetDefaultShippingAddressForDelivery() : GetDefaultAddress(cart);

            if (addressModel == null)
            {
                return null;
            }

            var shipment = cart.GetFirstShipment();
            shipment.ShippingAddress = _addressBookService.ConvertToAddress(addressModel, cart);
            UpdateLineItemProperties(cart, addressModel.Name.Equals(Shared.Constants.StringConstants.DefaultVaultShippingAddressName));
            _orderRepository.Save(cart);
            return addressModel;
        }
        public bool UpdateShippingMethod(IOrderGroup cart, AddressModel addressModel)
        {
            var isToVault = addressModel.Name.Equals(Shared.Constants.StringConstants.DefaultVaultShippingAddressName);
            var defaultShippingMethod = _shippingMethodHelper.GetBullionDefaultShippingMethod(cart, addressModel.CountryCode, isToVault);

            if (defaultShippingMethod == null)
            {
                cart.GetFirstShipment().ShippingMethodId = Guid.Empty;
                _orderRepository.Save(cart);
                return false;
            }

            cart.GetFirstShipment().ShippingMethodId = new Guid(defaultShippingMethod.Id);
            cart.ApplyDiscounts();
            _orderRepository.Save(cart);
            return true;
        }
        public AddressModel GetDefaultAddress(IOrderGroup cart)
        {
            //if item is signature or can vault -> update Address Vault
            //if item on can delivery -> get kyc address'customer.
            var lineItem = cart.GetAllLineItems().FirstOrDefault();
            var variant = lineItem?.Code.GetVariantByCode();
            if (variant == null) return null;

            var virtualVariant = variant as VirtualVariantBase;
            var physicalVariant = variant as PhysicalVariantBase;
            if (virtualVariant != null || (physicalVariant != null && physicalVariant.CanVault))
            {
                return _cartService.GetDefaultShippingAddressForVault();
            }
            return _cartService.GetDefaultShippingAddressForDelivery();
        }

        #region Buy bullion product
        public BullionBuyLTResponse BuyBullionByAutoInvest(ICart cart, string orderNumberPrefix, CustomerContact customerContact, string clientIpAddress = "")
        {
            var result = ProcessBullionCart(cart, orderNumberPrefix, customerContact);
            var response = new BullionBuyLTResponse
            {
                Message = result.Message,
                Status = result.Status
            };
            if (result.Success == true)
            {
                Constant.PampFinishQuoteStatus pampStatus;
                var po = ConvertToPurchaseOrder(cart, orderNumberPrefix, out pampStatus, SentEmailConfirmationFalse, customerContact, clientIpAddress);

                if (pampStatus == Constant.PampFinishQuoteStatus.Rejected)
                {
                    response.Message = _localizationService.GetStringByCulture(
                        Constants.StringResources.BullionCheckoutPampRejected, "Your order has been rejected. Please try again!",
                        ContentLanguage.PreferredCulture);
                    response.Status = Enums.AutoInvestUpdateOrderStatus.Rejected;
                }
                else if (pampStatus == Constant.PampFinishQuoteStatus.Timeout)
                {
                    response.Message = "Pamp request fail - Time out";
                    response.Status = Enums.AutoInvestUpdateOrderStatus.Timeout;
                }
                else if (pampStatus == Constant.PampFinishQuoteStatus.Undefined)
                {
                    response.Message = "Pamp request fail - Undefined ";
                    response.Status = Enums.AutoInvestUpdateOrderStatus.Undefined;
                }
                else
                {
                    response.Success = pampStatus == Constant.PampFinishQuoteStatus.Successful;
                    response.Status = Enums.AutoInvestUpdateOrderStatus.Success;
                }

            }
            return response;
        }
        public bool CheckAndLoadCart(bool isInEditMode, string cartName, out ICart cart)
        {
            cart = _cartService.LoadCart(cartName);
            if (isInEditMode) return true;
            return cart != null && cart.GetAllLineItems().Any();
        }
        public BullionBuyResponse ProcessBullionCart(ICart cart, string orderNumberPrefix)
        {
            return ProcessBullionCart(cart, orderNumberPrefix, CustomerContext.Current.CurrentContact);
        }
        public BullionBuyResponse ProcessBullionCart(ICart cart, string orderNumberPrefix, CustomerContact customerContact)
        {
            var response = new BullionBuyResponse();
            if (cart == null)
            {
                response.Message = "Cart is null";
                response.Status = Enums.AutoInvestUpdateOrderStatus.Error;
                return response;
            }   
            
            if (customerContact == null)
            {
                response.Message = "CustomerContact is null";
                response.Status = Enums.AutoInvestUpdateOrderStatus.Error;
                return response;
            }

            //validate cart items
            var cartValidationMessages = string.Empty;

            if (!_cartHelper.ValidateCart(cart, out cartValidationMessages, customerContact))
            {
                response.IsValidateCartError = true;
                response.Message = cartValidationMessages;
                response.Status = Enums.AutoInvestUpdateOrderStatus.Error;
                return response;
            }

            //Update default shipping address
            var addressModel = ValidateShippingAddress(cart);
            if (addressModel.Success == false)
            {
                response.IsValidateShippingAddressError = true;
                response.Message = addressModel.Statuses.FirstOrDefault().GetDescriptionAttribute();
                response.Status = addressModel.Statuses.FirstOrDefault();
                return response;
            }

            //var shippingMethod = SetShippingMethodsForBullionCart(cart);
            var shippingMethod = SetMetapackShipping(cart, customerContact);
            if (shippingMethod.Success == false)
            {
                var sb = new StringBuilder();
                var count = shippingMethod.Statuses.Count();
                foreach (var status in shippingMethod.Statuses)
                {
                    sb.Append(status.GetDescriptionAttribute());
                    if (count > 1) sb.Append(", ");
                }
                response.IsSetShippingMethodsForBullionCartError = true;
                response.Message = sb.ToString();
                response.Status = shippingMethod.Statuses.FirstOrDefault();
                return response;
            }

            if (_cartHelper.HasRetryWithPampExportTransaction(customerContact))
            {
                response.HasRetryWithPampExportTransactionError = true;
                response.Message = Enums.AutoInvestUpdateOrderStatus.HasPendingExportTransaction.GetDescriptionAttribute();
                response.Status = Enums.AutoInvestUpdateOrderStatus.HasPendingExportTransaction;
                return response;
            }

            //set shipping message for items
            _inventoryHelper.SetItemShippingMessages(cart, customerContact);

            //log inventory 
            _inventoryHelper.LogStockQuantity(cart, _logger, $"{Shared.Constants.StringConstants.CustomFields.BeforeText} AdjustInventoryOrRemoveLineItems");

            var validationIssues = new Dictionary<ILineItem, List<ValidationIssue>>();
            cart.AdjustInventoryOrRemoveLineItems((item, issue) => validationIssues.Add(item, new List<ValidationIssue> { issue }));

            //log inventory
            _inventoryHelper.LogStockQuantity(cart, _logger, $"{Shared.Constants.StringConstants.CustomFields.AfterText} AdjustInventoryOrRemoveLineItems");

            //Remove previous payments
            var processPaymentResponse = ProcessPaymentForBullionCart(cart, orderNumberPrefix, customerContact);
            var result = processPaymentResponse?.Results?.FirstOrDefault();
            if (result == null || !result.IsSuccessful)
            {
                var message = Enums.CheckoutIssue.CanNotProcessPayment.GetDescriptionAttribute();
                if (!string.IsNullOrEmpty(result?.Message))
                {
                    message = _localizationService.GetStringByCulture(result.Message, message,
                        ContentLanguage.PreferredCulture);
                }
                if (result.Message.ToLower().Contains("insufficientfund"))
                {
                    response.Status = Enums.AutoInvestUpdateOrderStatus.InsufficientFunds;
                }
                else
                {
                    response.Status = Enums.AutoInvestUpdateOrderStatus.Error;
                }
                response.IsProcessPaymentError = true;
                response.Message = message;
                return response;
            }
            else
            {
                response.Success = true;
                response.Status = Enums.AutoInvestUpdateOrderStatus.Success;

                //log inventory 
                _inventoryHelper.LogStockQuantity(cart, _logger, $"{Shared.Constants.StringConstants.CustomFields.AfterPaymentText}");
            }
            return response;
        }
        public IPurchaseOrder BullionConvertToPurchaseOrder(ICart cart, string orderNumberPrefix, out Constant.PampFinishQuoteStatus pampStatus, string clientIpAddress = "")
        {
            AddDefaultBullionPaymentIfMissing(cart, orderNumberPrefix);
            return ConvertToPurchaseOrder(cart, orderNumberPrefix, out pampStatus, SentEmailConfirmationTrue, null, clientIpAddress);
        }

        private void AddDefaultBullionPaymentIfMissing(ICart cart, string orderNumberPrefix)
        {
            if (!cart.GetFirstForm().Payments.Any())
            {
                _cartHelper.ProcessPaymentForBullionCart(cart, orderNumberPrefix, null);
            }
        }

        public IPurchaseOrder ConvertToPurchaseOrder(ICart cart, string orderNumberPrefix, out Constant.PampFinishQuoteStatus pampStatus, bool sentEmailConfirmation, CustomerContact customerContact, string clientIpAddress = "")
        {
            pampStatus = Constant.PampFinishQuoteStatus.Undefined;

            _cartService.SyncAndAdjustCartWithPampQuote(customerContact, cart, true);

            //Convert to purchase order
            var po = _cartHelper.ConvertToPurchaseOrder(cart, orderNumberPrefix, customerContact);
            if (!po.OrderStatus.Equals(OrderStatus.AwaitingExchange) && !po.OrderStatus.Equals(OrderStatus.InProgress)) return po;

            var quotRequestDtoId = po.Properties[TRM.Shared.Constants.StringConstants.CustomFields.BullionPAMPRequestQuoteId].ToString();
            if (string.IsNullOrEmpty(quotRequestDtoId) || string.IsNullOrWhiteSpace(quotRequestDtoId)) return po;
            //Finish the Pamp quote
            var finishQuoteResult = _pampTradingService.Value.FinishQuoteRequest(Guid.Parse(quotRequestDtoId));
            if (finishQuoteResult?.Result == null) return po;

            if (finishQuoteResult.Result.Success == false && finishQuoteResult.Result.ExecuteOnQuoteStatus == Constant.PampFinishQuoteStatus.Rejected)
            {
                //If Pamp request = rejected => don't create Export Transaction, return error to client 
                pampStatus = Constant.PampFinishQuoteStatus.Rejected;
                //Update order status to Cancelled
                _orderHelper.UpdatePurchaseOrderWhenPampQuoteRejected(po);
                _logger.Error($"Finish the Quote with status = {finishQuoteResult.Result.ExecuteOnQuoteStatus}");
                return po;
            }

            if (finishQuoteResult.Result.Success == false)
            {
                //If Pamp = fail, create Export Transaction record with status = Retry with PAMP
                pampStatus = Constant.PampFinishQuoteStatus.Timeout;
                //create export transaction
                _orderHelper.SavePurchaseOrdersExportTransaction(po, customerContact, IntegrationServices.Constants.StringConstants.AxIntegrationStatus.RetryWithPamp);
                _logger.Error($"Finish the Quote with status = {finishQuoteResult.Result.ExecuteOnQuoteStatus}");
                return po;
            }

            if (finishQuoteResult.Result.Success)
            {
                var contact = customerContact != null ? customerContact : CustomerContext.Current.CurrentContact;
                pampStatus = Constant.PampFinishQuoteStatus.Successful;
                //update purchase order
                _orderHelper.UpdatePurchaseOrderWhenPampQuoteSuccess(po, finishQuoteResult);
                //Update contact balance
                var totals = _orderGroupCalculator.GetOrderGroupTotals(po);
                _bullionContactHelper.UpdateBalances(contact, -totals.Total.Amount, -totals.Total.Amount, -totals.Total.Amount);
                //Update lifetime balance
                _bullionContactHelper.UpdateLifeTimeBalance(CustomerContext.Current.CurrentContact, totals.Total.Amount);
                //Update global purchase limit
                _globalPurchaseLimitService.UpdateGlobalPurchaseLimits(po);
                //Update portfolio
                _bullionPortfolioService.CreatePortfolioContentsWhenPurchase(po, customerContact);
                //create export transaction
                _orderHelper.SavePurchaseOrdersExportTransaction(po, customerContact);
                //Log transaction history
                _transactionHistoryHelper.LogThePurchaseTransaction(po, customerContact, clientIpAddress);

                if (sentEmailConfirmation == true)
                {
                    //Send mail confirmation order
                    SendBullionOrderConfirmationEmail(po);
                }
                else if (customerContact != null)
                {
                    //Send mail confirmation order - For LT Auto Investement
                    AutoPurchaseBullionOrderConfirmationEmail(po, customerContact);
                }

                //log inventory 
                _inventoryHelper.LogStockQuantity(cart, _logger, $"{Shared.Constants.StringConstants.CustomFields.AfterPaymentText} - OrderNumber {po.OrderNumber}");
            }

            return po;
        }
        public ValidateShippingAddress ValidateShippingAddress(IOrderGroup cart)
        {
            var response = new ValidateShippingAddress();

            if (cart != null && cart.Name == DefaultCartNames.DefaultAutoInvestCartName) return response;

            // deliver to address
            var addressShipment = cart.GetShipment();

            if (addressShipment == null)
            {
                response.Success = false;
                response.Statuses.Add(Enums.AutoInvestUpdateOrderStatus.MissingShippingAddress);
            }

            // deliver to vault
            var vaultShipment = cart.GetShipmentForVault();

            if (vaultShipment == null)
            {
                response.Success = false;
                response.Statuses.Add(Enums.AutoInvestUpdateOrderStatus.MissingVaultShippingAddress);
            }

            return response;
        }

        public ValidateShippingAddress SetMetapackShipping(IOrderGroup cart, CustomerContact contact)
        {
            var dto = ShippingManager.GetShippingMethodsByMarket(cart.MarketId.ToString(), false);
            var prvder = dto?.ShippingOption.FirstOrDefault(x => x.SystemKeyword == Shared.Constants.StringConstants.MetapackShippingProvider);
            var method = dto?.ShippingMethod.FirstOrDefault(x => x.IsActive && x.Currency == cart.Currency.CurrencyCode && x.ShippingOptionId == prvder?.ShippingOptionId);

            if (prvder == null || method == null)
                return SetShippingMethodsForBullionCart(cart, contact);

            var response = new ValidateShippingAddress();

            // deliver to address
            var addressShipment = cart.GetShipment();

            if (addressShipment.LineItems.Any())
            {
                var apiUrlParameter = dto.ShippingOptionParameter
                    .First(x => x.ShippingOptionId == prvder.ShippingOptionId && x.Parameter == ShippingParameters.ApiUrl);
                var userKeyParameter = dto.ShippingMethodParameter
                    .First(x => x.ShippingMethodId == method.ShippingMethodId && x.Parameter == ShippingParameters.UserKey);

                var shippingRequest = new ShippingRequest
                {
                    Key = Guid.Parse(userKeyParameter.Value),
                    CustomerCountryCode = addressShipment.ShippingAddress.CountryCode,
                    CustomerPostcode = addressShipment.ShippingAddress.PostalCode,
                };
                var option = _shippingService.GetShippingOptions(apiUrlParameter.Value, shippingRequest).Results.First();
                cart.SetShipment(method.ShippingMethodId, option.BookingCode, option.ShippingCharge);
            }
            else
            {
                cart.SetShipment(Guid.Empty, string.Empty, decimal.Zero);
            }

            // deliver to vault
            var vaultShipment = cart.GetShipmentForVault();

            if (vaultShipment.LineItems.Any())
            {
                var vaultMethod = _shippingMethodHelper.GetBullionDefaultShippingMethod(cart, vaultShipment.ShippingAddress.CountryCode, true);
                if (vaultMethod == null)
                {
                    vaultShipment.ShippingMethodId = Guid.Empty;
                }
                else
                {
                    cart.SetShipmentForVault(Guid.Parse(vaultMethod.Id));
                }
            }
            else
            {
                cart.SetShipmentForVault(Guid.Empty);
            }

            _orderRepository.Save(cart);

            return response;
        }
        public ValidateShippingAddress SetShippingMethodsForBullionCart(IOrderGroup cart, CustomerContact customerContact)
        {
            var response = new ValidateShippingAddress();

            var isValidAddress = ValidShippingAddress(cart, cart.GetFirstShipment(), customerContact, false);
            if (isValidAddress == false)
            {
                response.Success = false;
                response.Statuses.Add(Enums.AutoInvestUpdateOrderStatus.MissingDefaultShippingMethod);
            }
            isValidAddress = ValidShippingAddress(cart, cart.GetSecondShipment(), customerContact, true);
            if (isValidAddress == false)
            {
                response.Success = false;
                response.Statuses.Add(Enums.AutoInvestUpdateOrderStatus.MissingDefaultShippingMethodForVault);
            }

            _orderRepository.Save(cart);
            return response;
        }

        private bool ValidShippingAddress(IOrderGroup cart, IShipment shipment, CustomerContact customerContact, bool isToVault)
        {
            bool valid = true;
            if (shipment != null)
            {
                if (shipment.LineItems.Any())
                {
                    var shippingAddress = shipment.ShippingAddress;

                    var shippingMethod = _shippingMethodHelper.GetBullionDefaultShippingMethod(cart, shippingAddress.CountryCode, isToVault, customerContact);
                    if (shippingMethod == null)
                    {
                        valid = false;
                        shipment.ShippingMethodId = Guid.Empty;
                    }
                    else
                    {
                        shipment.ShippingMethodId = Guid.Parse(shippingMethod.Id);
                    }
                }
                else
                {
                    shipment.ShippingMethodId = Guid.Empty;
                }
            }
            return valid;
        }
        private ProcessPaymentForBullionResponse ProcessPaymentForBullionCart(IOrderGroup cart, string orderNumberPrefix, CustomerContact customerContact)
        {
            var response = new ProcessPaymentForBullionResponse();
            var result = _cartHelper.ProcessPaymentForBullionCart(cart, orderNumberPrefix, customerContact);

            if (result != null && result.Success)
            {
                response.Results = result.results;
                return response;
            }

            if (result != null && result.IsPaymentMethodError)
            {
                response.Statuses.Add(Enums.AutoInvestUpdateOrderStatus.MissingDefaultPaymentMethod);
                response.Message = result.Message;
                return response;
            }

            if (result != null && result.IsPaymentCreatedError)
            {
                response.Statuses.Add(Enums.AutoInvestUpdateOrderStatus.CanNotCreatePayment);
                response.Message = result.Message;
                response.Results = new List<PaymentProcessingResult>();
                return response;
            }

            if (result != null && result.IsPaymentExceptionError)
            {
                response.Statuses.Add(Enums.AutoInvestUpdateOrderStatus.CanNotProcessPayment);
                response.Message = result.Message;
                _logger.Error($"{result.Exception.Message} {result.Exception.InnerException?.Message}", result.Exception);
                return response;
            }

            return null;
        }
        private void SendBullionOrderConfirmationEmail(IPurchaseOrder po)
        {
            var purchaseOrder = _cartHelper.GetPurchaseOrderViewModel(po);
            _bullionEmailHelper.SendBullionOrderConfirmationEmail(purchaseOrder.ConvertToInvestmentPurchaseOrder());
        }
        private void AutoPurchaseBullionOrderConfirmationEmail(IPurchaseOrder po, CustomerContact customerContact)
        {
            var purchaseOrder = _cartHelper.GetPurchaseOrderViewModel(po, customerContact);
            _autoPurchaseMailingService.AutoPurchaseBullionOrderConfirmationEmail(purchaseOrder.ConvertToInvestmentPurchaseOrder(), customerContact);
        }
        #endregion

        private void UpdateLineItemProperties(IOrderGroup cart, bool isVault)
        {
            var lineItem = cart.GetAllLineItems().FirstOrDefault();
            if (lineItem == null) return;

            if (isVault)
            {
                lineItem.Properties[Shared.Constants.StringConstants.CustomFields.BullionDeliver] = (int)Enums.BullionDeliver.Vault;
            }
            else
            {
                lineItem.Properties[Shared.Constants.StringConstants.CustomFields.BullionDeliver] = (int)Enums.BullionDeliver.Deliver;
            }
        }
    }
}