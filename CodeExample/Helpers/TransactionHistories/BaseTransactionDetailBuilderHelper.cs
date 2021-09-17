using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using System.Collections.Generic;
using TRM.Web.Constants;
using TRM.Web.Models.EntityFramework.Transactions;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public abstract class BaseTransactionDetailBuilderHelper : ITransactionHistoryDetailBuilderHelper
    {
        protected LocalizationService _localizationServie;
        public BaseTransactionDetailBuilderHelper(LocalizationService localizationService)
        {
            _localizationServie = localizationService;
        }
        public virtual Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel)
        {
            var result= new Dictionary<string, object>();
            if (transactionViewModel == null) return result;
           
            return result;
        }

        public virtual bool IsSatified(TransactionHistoryType transactionType)
        {
            return false;
        }
    }
}