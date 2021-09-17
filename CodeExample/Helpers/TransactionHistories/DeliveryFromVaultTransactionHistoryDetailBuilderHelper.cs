using System.Collections.Generic;
using System.Linq;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public class DeliveryFromVaultTransactionHistoryDetailBuilderHelper : BaseTransactionDetailBuilderHelper
    {
        private readonly IAmEntryHelper _entryHelper;
        public DeliveryFromVaultTransactionHistoryDetailBuilderHelper(
            LocalizationService localizationService,
            IAmEntryHelper entryHelper)
            : base(localizationService)
        {
            _entryHelper = entryHelper;
        }
        public override bool IsSatified(TransactionHistoryType transactionType)
        {
            return transactionType == TransactionHistoryType.DeliverFromVault;
        }

        public override Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel)
        {
            var result = base.BuildTheDetailInformation(transactionViewModel);

            var submittedByText = _localizationServie.GetStringByCulture(StringResources.TransactionHistorySubmittedBy, 
                StringConstants.TranslationFallback.TransactionHistorySubmittedBy, 
                ContentLanguage.PreferredCulture);
            result.Add(submittedByText, transactionViewModel.SubmittedBy);

            var orderNumberText = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryReferenceNumber,
                StringConstants.TranslationFallback.TransactionHistoryReferenceNumber, 
                ContentLanguage.PreferredCulture);
            result.Add(orderNumberText, transactionViewModel.TransactionRecord.OrderNumber);

            var orderSummaryText = _localizationServie.GetStringByCulture(StringResources.TransactionHistoryOrderSummary, 
                StringConstants.TranslationFallback.TransactionHistoryOrderSummary, 
                ContentLanguage.PreferredCulture);

            if (transactionViewModel.OrderDetailViewModel != null)
            {
                transactionViewModel.OrderDetailViewModel.OrderLineViewModels =
                    transactionViewModel.OrderDetailViewModel.OrderLineViewModels.Where(x => x.LineItemId.StartsWith("Ch"));
            }
            
            result.Add(orderSummaryText, transactionViewModel.OrderDetailViewModel);                

            return result;
        }
    }
}