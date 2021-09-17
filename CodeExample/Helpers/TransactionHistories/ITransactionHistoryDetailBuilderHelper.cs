using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.Transactions;
using TRM.Web.Models.ViewModels.Bullion;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.TransactionHistories
{
    public interface ITransactionHistoryDetailBuilderHelper
    {
        bool IsSatified(TransactionHistoryType transactionType);

        Dictionary<string, object> BuildTheDetailInformation(TransactionHistoryItemViewModel transactionViewModel);
    }
}