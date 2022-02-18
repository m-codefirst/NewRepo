using EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays.Extensions;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using log4net;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TRM.Shared.Constants;
using TRM.Shared.DataAccess;
using TRM.Shared.Helpers;
using TRM.Shared.Models.DTOs;
using TRM.Shared.Models.DTOs.Payments;

namespace EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays
{
    // ReSharper disable once InconsistentNaming
    public class BarclaysCardPaymentGateway : IPaymentPlugin
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(BarclaysCardPaymentGateway));

        public const string MerchantIdParameter = "BarclaysCardMerchantId";
        public const string SAUrlParameter = "SAUrlParameter";
        public const string SAUrlSilentParameter = "SAUrlSilentParameter";
        public const string CapturePayment = "BarclaysCardCapturePayment";
        public const string AllowSavedCards = "AllowSavedCards";

        public const string ProfileID = "ProfileID";
        public const string AccessKey = "AccessKey";
        public const string SecretKey = "SecretKey";

        public const string UsProfileID = "UsProfileID";
        public const string UsAccessKey = "UsAccessKey";
        public const string UsSecretKey = "UsSecretKey";

        // ReSharper disable once InconsistentNaming
        private const string CardPaymentError = "cardpaymenterror";

        private readonly IThirdPartyTransactionRepository _thirdPartyTransactionRepository;

        public BarclaysCardPaymentGateway()
            : this(ServiceLocator.Current.GetInstance<IThirdPartyTransactionRepository>())
        {
        }

    
        public BarclaysCardPaymentGateway(IThirdPartyTransactionRepository thirdPartyTransactionRepository)
        {
            _thirdPartyTransactionRepository = thirdPartyTransactionRepository;
        }

        
        private IOrderGroup OrderGroup { get; set; }

        private string _responseUrl;

        public PaymentProcessingResult ProcessPayment(IOrderGroup orderGroup, IPayment payment)
        {
            OrderGroup = orderGroup;

            var message = string.Empty;

            var result = ProcessPayment(payment, ref message,orderGroup.CustomerId);

            return result ? PaymentProcessingResult.CreateSuccessfulResult(message, _responseUrl) : PaymentProcessingResult.CreateUnsuccessfulResult(message);
        }

        public IDictionary<string, string> Settings { get; set; }

        public bool ProcessPayment(IPayment payment, ref string message, Guid customerId)
        {

            try
            {
                if (string.IsNullOrEmpty(payment.TransactionType))
                    payment.TransactionType = Mediachase.Commerce.Orders.TransactionType.Authorization.ToString();

                var mastercardPayment = payment.ConvertToBarclaysPayment();
                if (mastercardPayment == null) return false;

                var cart = OrderGroup as ICart;
                if (cart == null)
                {
                    message = StringResources.PaymentErrorsMastercardError;
                    return false;
                }
                
                var manualPaymentDto = ExtractDtoFromPayment(mastercardPayment, cart);

                _thirdPartyTransactionRepository.AddOrUpdateTransaction(new ThirdPartyTransaction
                {
                    Id = manualPaymentDto.OrderNumber,
                    TransactionDate = DateTime.Now,
                    TransactionPayloadJson = JsonConvert.SerializeObject(manualPaymentDto),
                    TransactionType = TRM.Shared.Models.DTOs.TransactionType.Mastercard,
                    TransactionStatus = ThirdPartyTransactionStatus.Pending
                });


                PaymentStatusManager.ProcessPayment(payment);

                return true;
            }
            catch (System.Exception err)
            {
                _logger.Error(err.Message, err);
                payment.Status = PaymentStatus.Failed.ToString();

                message = StringResources.PaymentErrorsMastercardError;
                return false;
            }
        }

        public ManualPaymentDto ExtractDtoFromPayment(Orders.BarclaysCardPayment payment, ICart cart)
        {
            var iPayment = payment as IPayment;
            if (iPayment.BillingAddress == null)
            {
                iPayment.BillingAddress = new OrderAddress();
            }

            var paymentHelper = ServiceLocator.Current.GetInstance<IAmPaymentHelper>();
            var amountToPayNow = paymentHelper?.GetCartTotalWithoutRecuringItems(cart).Amount ?? payment.Amount;

            var orderNumber = string.IsNullOrWhiteSpace(payment.MastercardOrderNumber)
                ? Utilities.GenerateOrderIdFromTransactionId(payment.TransactionID)
                : payment.MastercardOrderNumber;

            return new ManualPaymentDto
            {
                MastercardSessionId = payment.MastercardSessionId,
                OrderNumber = orderNumber,
                IsAmexPayment = payment.IsAmexPayment,
                Amount = Math.Round(amountToPayNow, 2),
                NameOnCard = payment.MastercardNameOnCard,
                TokenisedCardNumber = payment.TokenisedCardNumber,
                BillingAddress = new Address
                {
                    city = string.IsNullOrWhiteSpace(iPayment.BillingAddress?.City) ? null : iPayment.BillingAddress.City,
                    postcodeZip = string.IsNullOrWhiteSpace(iPayment.BillingAddress?.PostalCode) ? null : iPayment.BillingAddress.PostalCode,
                    street = string.IsNullOrWhiteSpace(iPayment.BillingAddress?.Line1) ? null : iPayment.BillingAddress.Line1,
                    street2 = string.IsNullOrWhiteSpace(iPayment.BillingAddress?.Line2) ? null : iPayment.BillingAddress.Line2,
                    country = string.IsNullOrWhiteSpace(iPayment.BillingAddress?.CountryCode) ? StringConstants.DefaultValues.CountryCode : iPayment.BillingAddress.CountryCode
                },
                CurrencyCode = cart.Currency.CurrencyCode,
                TransactionId = payment.TransactionID,
                PaymentId = payment.PaymentId,
                PaymentMethodName = payment.PaymentMethodName,
                Mastercard3DSecureId = payment.Mastercard3DSecureId,
                PaymentMethodId = payment.PaymentMethodId,
                CustomerId = cart.CustomerId,
                PaymentType = Enums.TrmPaymentType.Purchase,
                OrderGroupReference = cart.OrderLink
            };
        }

    }
}