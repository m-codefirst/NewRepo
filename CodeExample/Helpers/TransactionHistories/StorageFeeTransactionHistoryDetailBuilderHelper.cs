using System.Collections.Generic;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public class StorageFeeTransactionHistoryDetailBuilderHelper : BaseTransactionDetailBuilderHelper
    {
        public StorageFeeTransactionHistoryDetailBuilderHelper(LocalizationService localizationService)
            : base(localizationService)
        {

        }
        public override bool IsSatified(TransactionHistoryType transactionType)
        {
            return transactionType == TransactionHistoryType.StorageFee;
        }

        public override Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel)
        {
            var result= base.BuildTheDetailInformation(transactionViewModel);
            var forPeriodText = _localizationServie.GetStringByCulture(
                StringResources.TransactionHistoryForPeriod,
                StringConstants.TranslationFallback.TransactionHistoryAccountActivityCommentText, 
                ContentLanguage.PreferredCulture);
            var dateTimeFormat = "MMM d yyyy";
            var timePeriod = string.Format("{0} to {1}", transactionViewModel.TransactionRecord.PeriodFrom.ToString(dateTimeFormat),
                transactionViewModel.TransactionRecord.PeriodTo.ToString(dateTimeFormat));
            result.Add(forPeriodText, transactionViewModel.TransactionRecord.PeriodFrom.ToString("MMM d yyyy"));
            return result;
        }
    }
}