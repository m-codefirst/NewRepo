using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using System.Collections.Generic;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public class WithdrawalTransactionDetailBuilderHelper : BaseTransactionDetailBuilderHelper
    {
        public WithdrawalTransactionDetailBuilderHelper(LocalizationService localizationService)
            : base(localizationService)
        {

        }
        public override bool IsSatified(TransactionHistoryType transactionType)
        {
            return transactionType == TransactionHistoryType.Withdraw;
        }

        public override Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel)
        {
            var result = base.BuildTheDetailInformation(transactionViewModel);
            var submittedByText = _localizationServie.GetStringByCulture(StringResources.TransactionHistorySubmittedBy, StringConstants.TranslationFallback.TransactionHistorySubmittedBy, ContentLanguage.PreferredCulture);
            result.Add(submittedByText, transactionViewModel.SubmittedBy);
            var bankAccount = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryBankAccount, StringConstants.TranslationFallback.TransactionHistoryBankAccount, ContentLanguage.PreferredCulture);
            var paymentType = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryPaymentType, StringConstants.TranslationFallback.TransactionHistoryPaymentType, ContentLanguage.PreferredCulture);
            result.Add(bankAccount, transactionViewModel.TransactionRecord.BankAccount);
            result.Add(paymentType, transactionViewModel.TransactionRecord.WithdrawalMethod);
            return result;
        }

    }
}