using EPiServer.ServiceLocation;
using System;
using System.Linq;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess
{
    [ServiceConfiguration(typeof(PampMetalPriceSyncHistoryRepository))]
    [ServiceConfiguration(typeof(PampMetalSyncRepositoryBase<PampMetalPriceSyncHistory>))]
    public class PampMetalPriceSyncHistoryRepository : PampMetalSyncRepositoryBase<PampMetalPriceSyncHistory>
    {
        public override Guid Insert(PampMetalPriceSyncHistory price)
        {
            context.PampMetalPriceSyncHistory.Add(price);
            context.SaveChanges();
            return price.Id;
        }

        public override IQueryable<PampMetalPriceSyncHistory> GetList()
        {
            return context.PampMetalPriceSyncHistory;
        }
    }
}