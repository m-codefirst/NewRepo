using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using System;
using System.Data.Entity.Migrations;
using System.Linq;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.Transactions;

namespace TRM.Web.Business.DataAccess
{
    [ServiceConfiguration(typeof(ITransactionHistoryRepository), Lifecycle = ServiceInstanceScope.Transient)]
    public class TransactionHistoryRepository : DbContextDisposable<TransactionHistoriesContext>, ITransactionHistoryRepository
    {
        private static ILog Logger = LogManager.GetLogger(typeof(TransactionHistoryRepository));

        public bool UpsertTransactionHistoryOrderLineRecord(TransactionHistoryOrderLine record)
        {
            try
            {
                if (record == null) throw new Exception("Cannot insert the null record into the table TransactionHistoryOrderLine");
                context.TransactionHistoryOrderLine.AddOrUpdate(record);
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        public bool AddOrUpdateTransactionHistoryRecord(TransactionHistory record)
        {
            try
            {
                if (record == null) throw new Exception("Cannot insert the null record into the table TransactionHistory");
                context.TransactionHistory.AddOrUpdate(record);
                context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        public bool UpdateTransactionHistoryStatus(TransactionHistory record)
        {
            var existingTransaction = context.TransactionHistory.FirstOrDefault(x => x.PkId == record.PkId);

            if (existingTransaction == null) return false;

            existingTransaction.ModifiedDate = DateTime.Now;
            existingTransaction.IncludedInAxBalances = record.IncludedInAxBalances;
            existingTransaction.Status = record.Status;

            return AddOrUpdateTransactionHistoryRecord(existingTransaction);

        }

        public TransactionHistoryOrderLine GetTransactionHistoryOrderLineByEpiTransId(string epiTransId)
        {
            return context.TransactionHistoryOrderLine.FirstOrDefault(x => x.LineItemId == epiTransId);
        }
    }
}
