using System.Collections.Generic;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public class SellFromVaultTransactionHistoryDetailBuilderHelper: BaseTransactionDetailBuilderHelper
    {
        public SellFromVaultTransactionHistoryDetailBuilderHelper(LocalizationService localizationService)
            : base(localizationService)
        {

        }
        public override bool IsSatified(TransactionHistoryType transactionType)
        {
            return transactionType == TransactionHistoryType.SellFromVault;
        }

        public override Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel)
        {
            var result = base.BuildTheDetailInformation(transactionViewModel);
            var submittedByText = _localizationServie.GetStringByCulture(StringResources.TransactionHistorySubmittedBy, StringConstants.TranslationFallback.TransactionHistorySubmittedBy, ContentLanguage.PreferredCulture);
            result.Add(submittedByText, transactionViewModel.SubmittedBy);

            var orderNumberText = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryOrderNumber, StringConstants.TranslationFallback.TransactionHistoryOrderNumber, ContentLanguage.PreferredCulture);
            result.Add(orderNumberText, transactionViewModel.TransactionRecord.OrderNumber);

            var orderSummaryText = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryOrderSummary, StringConstants.TranslationFallback.TransactionHistoryOrderSummary, ContentLanguage.PreferredCulture);
            result.Add(orderSummaryText, transactionViewModel.OrderDetailViewModel);

            return result;
        }
    }
}