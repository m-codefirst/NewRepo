using System.Collections.Generic;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public class AccountActivityTransactionHistoryDetailBuidlerHelper : BaseTransactionDetailBuilderHelper
    {
        public AccountActivityTransactionHistoryDetailBuidlerHelper(LocalizationService localizationService)
            : base(localizationService)
        {
        }
        public override bool IsSatified(TransactionHistoryType transactionType)
        {
            return transactionType == TransactionHistoryType.AccountActivity;
        }

        public override Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel)
        {
            var result= base.BuildTheDetailInformation(transactionViewModel);
            var adjustmentCommentText = _localizationServie.GetStringByCulture(
                StringResources.TransactionHistoryAccountActivityCommentText,
                StringConstants.TranslationFallback.TransactionHistoryAccountActivityCommentText,
                ContentLanguage.PreferredCulture);
            result.Add(adjustmentCommentText, transactionViewModel.TransactionRecord.Description);
            return result;
        }
    }
}