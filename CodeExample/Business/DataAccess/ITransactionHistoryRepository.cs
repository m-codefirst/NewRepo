using TRM.Web.Models.EntityFramework.Transactions;

namespace TRM.Web.Business.DataAccess
{
    public interface ITransactionHistoryRepository
    {
        bool AddOrUpdateTransactionHistoryRecord(TransactionHistory record);
        bool UpsertTransactionHistoryOrderLineRecord(TransactionHistoryOrderLine record);
        bool UpdateTransactionHistoryStatus(TransactionHistory record);
        TransactionHistoryOrderLine GetTransactionHistoryOrderLineByEpiTransId(string epiTransId);
    }
}
