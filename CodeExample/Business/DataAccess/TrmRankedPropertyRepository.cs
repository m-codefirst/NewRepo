using System.Linq;
using TRM.Web.Models.Catalog.DDS;

namespace TRM.Web.Business.DataAccess
{
    
    public class TrmRankedPropertyRepository<T> : TrmGenericRepository<T>, ITrmRankedPropertyRepository<T> where T : RankedMultiSelectBase
    {
        protected override void CopyDataIntoDto(T objectToSave, T dto)
        {
            dto.DisplayName = objectToSave.DisplayName;
            dto.Value = objectToSave.Value;
            dto.Rank = objectToSave.Rank;
        }

        public IOrderedQueryable<RankedMultiSelectBase> Items()
        {
            return Store.Items<RankedMultiSelectBase>();
        }
    }
}