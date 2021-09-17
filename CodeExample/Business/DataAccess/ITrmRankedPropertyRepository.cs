using System.Linq;
using Hephaestus.CMS.DataAccess;
using TRM.Web.Models.Catalog.DDS;

namespace TRM.Web.Business.DataAccess
{
    public interface ITrmRankedPropertyRepository<T> : IRepository<T> where T : RankedMultiSelectBase
    {
        IOrderedQueryable<RankedMultiSelectBase> Items();
    }
}