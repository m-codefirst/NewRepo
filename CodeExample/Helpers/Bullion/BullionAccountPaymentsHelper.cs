using System;
using EPiServer.Business.Commerce.Payment.Mastercard.Mastercard;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Models.Export;
using TRM.IntegrationServices.Models.Export.Orders;
using TRM.IntegrationServices.Models.Export.Payloads.CardDeposit;
using TRM.IntegrationServices.Models.Impersonation;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Helpers;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Extentions;
using TRM.Web.Services;

namespace TRM.Web.Helpers.Bullion
{
    [ServiceConfiguration(typeof(IBullionAccountPaymentsHelper), Lifecycle = ServiceInstanceScope.Transient)]
    public class BullionAccountPaymentsHelper : IBullionAccountPaymentsHelper
    {
        private readonly CustomerContext _customerContext;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IExportTransactionService _exportTransactionService;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        private readonly IUserService _userService;
        private readonly IImpersonationLogService _impersonationLogService;

        public BullionAccountPaymentsHelper(
            CustomerContext customerContext,
            IAmTransactionHistoryHelper transactionHistoryHelper,
            IExportTransactionService exportTransactionService, 
            IAmBullionContactHelper bullionContactHelper,
            IImpersonationLogService impersonationLogService, 
            IUserService userService)
        {
            _customerContext = customerContext;
            _transactionHistoryHelper = transactionHistoryHelper;
            _exportTransactionService = exportTransactionService;
            _bullionContactHelper = bullionContactHelper;
            _impersonationLogService = impersonationLogService;
            _userService = userService;
        }

        public bool CreateFundTransactionHistory(ManualPaymentDto dto, string exportTransactionId, string clientIpAddress = "")
        {
            var newCustomerEffectiveBalance = _bullionContactHelper.GetEffectiveBalance(_customerContext.CurrentContact) + dto.Amount;

            return _transactionHistoryHelper.LogTheAddFundTransaction(
                dto.Amount,
                newCustomerEffectiveBalance,
                dto.CurrencyCode,
                dto.OrderNumber, exportTransactionId, clientIpAddress);
        }

        public string CreateExportTransaction(ManualPaymentDto dto, Tokenization token, string lastFour)
        {
            var payload = new CardDepositExportTransactionPayload
            {
                CardDeposit = new ExportTransactionCardDeposit
                {
                    Header = new CardDepositHeader()
                    {
                        CurrencyCode = dto.CurrencyCode,
                        TransactionRef = dto.TransactionId,
                        OrderTotal = dto.Amount.ToString("0.##")
                    },
                    Payment = new CardDepositPayment()
                    {
                        PaymentValue = dto.Amount.ToString("0.##"),
                        PaymentType = ((int)PaymentMethod.WEBCRD).ToString(),
                        CardTypeId = CardTypeId.OnlineCard.ToString(),
                        PreauthCode = dto.AuthorizationCode,
                        TokenId = token != null ? token.token : dto.TokenisedCardNumber,
                        EncryptedCardNumber = dto.MastercardCardNumber,
                        Last4Digits = lastFour,
                        ExpiryDate = dto.MastercardCardExpiry,
                        CardHolderName = dto.NameOnCard,
                        ECIID = dto.Mastercard3DsEci,
                        CV2 = string.Empty,
                        ThreeDSStatus = dto.Mastercard3DsStatus,
                        SecureTransactionID = dto.MastercardSessionId,
                        ThreeDSAuthenticationValue = dto.Mastercard3DSecureId,
                        SecureCodeSecurityID = dto.Mastercard3DsSid,
                        BillingAddressLine1 = dto.BillingAddress.street,
                        BillingAddressLine2 = dto.BillingAddress.street2,
                        BillingAddressLine3 = dto.BillingAddress.city,
                        BillingAddressLine4 = string.Empty,
                        BillingAddressLine5 = dto.BillingAddress.country,
                        PostCode = dto.BillingAddress.postcodeZip,
                        PaymentOrderId = dto.OrderNumber
                    }
                }
            };

            ExportTransaction exportTransaction;

            var customerId = _customerContext.CurrentContactId.ToString();
            _exportTransactionService.CreateExportTransaction(payload, customerId, IntegrationServices.Constants.ExportTransactionType.CardDeposit, out exportTransaction);

            UserDetails userDetails;
            if (_userService.IsImpersonating())
                userDetails = UserDetails.ForImpersonator(CustomerContext.Current, RequestHelper.GetClientIpAddress());
            else
                userDetails = UserDetails.ForCustomer(CustomerContext.Current, RequestHelper.GetClientIpAddress());

            var impersonationLog = ImpersonationLog.ForExportTransaction(
                userDetails,
                exportTransaction,
                dto.OrderNumber);
            _impersonationLogService.CreateLog(impersonationLog);

            return exportTransaction?.TransactionId;
        }

        public decimal UpdateCustomerBalances(CustomerContact customer, decimal amount)
        {
            var newCustomerEffectiveBalance = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance) + amount;
            var newCustomerAvailableToSpendBalance = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend) + amount;

            customer.Properties[StringConstants.CustomFields.BullionCustomerEffectiveBalance].Value = Math.Round(newCustomerEffectiveBalance, 2);
            customer.Properties[StringConstants.CustomFields.BullionCustomerAvailableToSpend].Value = Math.Round(newCustomerAvailableToSpendBalance, 2);
            customer.SaveChanges();
            return newCustomerAvailableToSpendBalance;
        }
    }
}