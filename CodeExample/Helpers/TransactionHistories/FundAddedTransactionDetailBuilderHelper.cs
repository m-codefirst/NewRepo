using System.Collections.Generic;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public class FundAddedTransactionDetailBuilderHelper : BaseTransactionDetailBuilderHelper
    {
        public FundAddedTransactionDetailBuilderHelper(LocalizationService localizationService)
            : base(localizationService)
        {

        }
        public override bool IsSatified(TransactionHistoryType transactionType)
        {
            return transactionType == TransactionHistoryType.FundsAddedCard;
        }

        public override Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel)
        {
            var result= base.BuildTheDetailInformation(transactionViewModel);

            var submittedByText = _localizationServie.GetStringByCulture(StringResources.TransactionHistorySubmittedBy, StringConstants.TranslationFallback.TransactionHistorySubmittedBy, ContentLanguage.PreferredCulture);
            result.Add(submittedByText, transactionViewModel.SubmittedBy);

            var orderNumberText = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryOrderNumber, StringConstants.TranslationFallback.TransactionHistoryReferenceNumber, ContentLanguage.PreferredCulture);
            result.Add(orderNumberText, transactionViewModel.TransactionRecord.OrderNumber);

            var fundedMethod = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryFundedMethod, StringConstants.TranslationFallback.TransactionHistoryFundedMethod, ContentLanguage.PreferredCulture);
            result.Add(fundedMethod, "Debit / Credit Card");

            return result;
        }
    }
}