using System;
using System.Linq;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess
{
    public class PampMetalPriceImportingRepository : PampMetalSyncRepositoryBase<PampMetalPriceSync>
    {
        public override IQueryable<PampMetalPriceSync> GetList()
        {
            return context.PampMetalPriceSync;
        }

        public override Guid Insert(PampMetalPriceSync pampMetalPrice)
        {
            context.PampMetalPriceSync.Add(pampMetalPrice);
            context.SaveChanges();
            return pampMetalPrice.Id;
        }
    }
}