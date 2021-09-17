using EPiServer;
using EPiServer.Web;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.IntegrationServices.Constants;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Models.Export;
using TRM.IntegrationServices.Models.Export.Payloads.WithdrawalRequest;
using TRM.IntegrationServices.Models.Impersonation;
using TRM.Shared.Extensions;
using TRM.Shared.Helpers;
using TRM.Web.Business.Pricing.BullionTax;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.Bullion.CustomerBankAccount;
using TRM.Web.Models.ViewModels.Bullion.RequestStandardWithdrawal;
using TRM.Web.Services;
using Enums = TRM.Web.Constants.Enums;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Helpers.Bullion
{
    public class BullionWithdrawalHelper : IBullionWithdrawalHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IBullionTaxService _bullionTaxService;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IExportTransactionService _exportTransactionService;
        private readonly IBullionPremiumGroupHelper _bullionPremiumGroupHelper;

        private readonly IUserService _userService;
        private readonly IImpersonationLogService _impersonationLogService;

        public BullionWithdrawalHelper(
            IContentLoader contentLoader,
            IAmTransactionHistoryHelper transactionHistoryHelper,
            IBullionTaxService bullionTaxService,
            IExportTransactionService exportTransactionService, 
            IBullionPremiumGroupHelper bullionPremiumGroupHelper,
            IUserService userService,
            IImpersonationLogService impersonationLogService)
        {
            _contentLoader = contentLoader;
            _transactionHistoryHelper = transactionHistoryHelper;
            _bullionTaxService = bullionTaxService;
            _exportTransactionService = exportTransactionService;
            _bullionPremiumGroupHelper = bullionPremiumGroupHelper;
            _userService = userService;
            _impersonationLogService = impersonationLogService;
        }

        public WithdrawalFundsDto WithdrawFunds(CustomerContact customer, Guid contactId, decimal amountToWithdraw,
            BankAccountViewModel bankAccount, string orderNumberPrefix, RequestWithdrawalPaymentType paymentType,
            out decimal availableToWithdraw)
        {
            #region validation

            availableToWithdraw = decimal.Zero;
            if (customer == null) return new WithdrawalFundsDto {IsSuccess = false, Message = "Customer was null"};
            if (bankAccount == null)
                return new WithdrawalFundsDto {IsSuccess = false, Message = "Bank Account was null"};

            var withdrawalFee = GetWithdrawalFee(customer, bankAccount, paymentType.IsQuickPayment,
                out var withdrawalFeeMessage);
            if (withdrawalFee == null)
                return new WithdrawalFundsDto
                {
                    IsSuccess = false,
                    Message = "Could not find an applicable withdrawal fee block, Withdrawal Fee Message: " +
                              withdrawalFeeMessage
                };
            var withdrawalVat = GetWithdrawalVat(bankAccount, withdrawalFee);

            var amountIncludingFee = amountToWithdraw + withdrawalFee.Fee + withdrawalVat;

            var customerAvailableToWithdraw =
                customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToWithdraw);
            if (customerAvailableToWithdraw < amountIncludingFee)
            {
                return new WithdrawalFundsDto
                {
                    IsSuccess = false,
                    Message =
                        $"Available to Withdraw: {availableToWithdraw} is less than the amount including fee: {amountIncludingFee}"
                };
            }

            var nextPaymentId = customer.GetIntegerProperty(StringConstants.CustomFields.BullionNextPaymentNo);
            if (nextPaymentId == 0) nextPaymentId = 1;

            Guid customerBankAccountId;
            if (!Guid.TryParse(bankAccount.CustomerBankAccountId, out customerBankAccountId))
            {
                return new WithdrawalFundsDto
                {
                    IsSuccess = false,
                    Message =
                        $"bankAccount.CustomerBankAccountId: {bankAccount.CustomerBankAccountId} could not be parsed into a Guid."
                };
            }

            #endregion

            var withdrawalPayload = new WithdrawalRequestTransactionPayload
            {
                Payment = new WithdrawalRequestPayment
                {
                    BankAccountId = customerBankAccountId.ToString("N").Substring(0, 10),
                    PaymentValue = amountToWithdraw,
                    FasterPayment = paymentType.IsQuickPayment,
                    WithdrawalFee = withdrawalFee.Fee + withdrawalVat
                }
            };

            ExportTransaction exportTransaction;
            var transactionAdded = _exportTransactionService.CreateExportTransaction(withdrawalPayload,
                CustomerContext.Current.CurrentContactId.ToString(), ExportTransactionType.WithdrawalRequest,
                out exportTransaction);

            if (transactionAdded)
            {
                UserDetails userDetails;
                if (_userService.IsImpersonating())
                    userDetails = UserDetails.ForImpersonator(CustomerContext.Current, RequestHelper.GetClientIpAddress());
                else
                    userDetails = UserDetails.ForCustomer(CustomerContext.Current, RequestHelper.GetClientIpAddress());

                var impersonationLog = ImpersonationLog.ForExportTransaction(
                    userDetails,
                    exportTransaction,
                    null);
                _impersonationLogService.CreateLog(impersonationLog);
            }

            if (transactionAdded)
            {
                var customerEffectiveBalance =
                    customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);
                var customerAvailableToSpendBalance =
                    customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend);
                availableToWithdraw = customerAvailableToWithdraw - amountIncludingFee;
                customer.Properties[StringConstants.CustomFields.BullionCustomerAvailableToWithdraw].Value =
                    Math.Round(availableToWithdraw, 2);
                customer.Properties[StringConstants.CustomFields.BullionCustomerEffectiveBalance].Value =
                    Math.Round(customerEffectiveBalance - amountIncludingFee, 2);
                customer.Properties[StringConstants.CustomFields.BullionCustomerAvailableToSpend].Value =
                    Math.Round(customerAvailableToSpendBalance - amountIncludingFee, 2);
                customer.Properties[StringConstants.CustomFields.BullionNextPaymentNo].Value = nextPaymentId + 1;
                customer.SaveChanges();
                _transactionHistoryHelper.LogTheWithdrawTransactionHistory(bankAccount.CustomerBankAccountId,
                    bankAccount.Nickname, amountIncludingFee, paymentType.DisplayName,
                    withdrawalFee.Fee + withdrawalVat, customer.PreferredCurrency, exportTransaction?.TransactionId);
            }

            return new WithdrawalFundsDto
            {
                IsSuccess = transactionAdded,
                Message = "Transaction added: " + transactionAdded
            };
        }

        public decimal GetWithdrawalVat(BankAccountViewModel bankAccount, BullionBankWithdrawalFeeBlock withdrawalFee)
        {
            return GetWithdrawalVatRate(bankAccount) * withdrawalFee.Fee / 100;
        }

        public BullionBankWithdrawalFeeBlock GetWithdrawalFee(CustomerContact customer,
            BankAccountViewModel bankAccount, bool quickWithdrawal, out string message)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var currencyCode = customer.PreferredCurrency;
            var customerPremiumGroupInt =
                customer.GetIntegerProperty(StringConstants.CustomFields.BullionPremiumGroupInt);

            var feeBlocksContentArea = (quickWithdrawal && bankAccount.QuickWithdrawalEnabled)
                ? startPage.BankAccountQuickWithdrawalFees
                : startPage.BullionBankWithdrawalFees;

            if (feeBlocksContentArea?.IsEmpty ?? true)
            {
                message = string.Format("feeBlocksContentArea is empty, we used this content area: {0}",
                    quickWithdrawal && bankAccount.QuickWithdrawalEnabled
                        ? "BankAccountQuickWithdrawalFees"
                        : "BullionBankWithdrawalFees");
                return null;
            }

            var allFeeBlocks = feeBlocksContentArea.Items
                .Select(x => _contentLoader.Get<BullionBankWithdrawalFeeBlock>(x.ContentLink))
                .Where(x => x != null && x.Currency == currencyCode).ToList();

            var applicableFeeBlocks = allFeeBlocks.Where(x => x.BankCountry == bankAccount.CountryCode && (x.CustomerPremiumGroup == customerPremiumGroupInt.ToString() || string.IsNullOrWhiteSpace(x.CustomerPremiumGroup))).ToList();

            if (applicableFeeBlocks.Any())
            {
                message = "applicableFeeBlocks found!";
                return applicableFeeBlocks.First();
            }

            applicableFeeBlocks = allFeeBlocks.Where(x =>
                string.IsNullOrWhiteSpace(x.BankCountry) && string.IsNullOrWhiteSpace(x.CustomerPremiumGroup)).ToList();

            if (applicableFeeBlocks.Any())
            {
                message = "applicableFeeBlocks for a bank/premiumgroup found!";
                return applicableFeeBlocks.First();
            }

            message = "applicableFeeBlocks for a bank/premiumgroup not found!";
            return null;
        }

        public List<RequestWithdrawalPaymentType> GetListRequestWithdrawalPaymentTypes()
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var requestWithdrawalPaymentTypes = startPage.RequestWithdrawalPaymentTypes;
            return requestWithdrawalPaymentTypes?.ToList() ?? new List<RequestWithdrawalPaymentType>();
        }

        public RequestWithdrawalPaymentType GetWithdrawalPaymentType(string paymentName)
        {
            return GetListRequestWithdrawalPaymentTypes().FirstOrDefault(x => x.DisplayName.Equals(paymentName));
        }

        public decimal GetWithdrawalVatRate(BankAccountViewModel bankAccount)
        {
            var vatRule = _bullionTaxService.GetVatRule(Enums.BullionActionType.FundsWithdrawalFee);
            return vatRule == null
                ? decimal.Zero
                : _bullionTaxService.GetVatRateAmount(bankAccount.CountryCode, vatRule.VatCode);
        }
    }
}