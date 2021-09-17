using System.Linq;
using TRM.Web.Business.DataAccess;
using TRM.Web.Models.Catalog.DDS;

namespace TRM.Web.Helpers
{
    public class RankedPropertyHelper<T> : IAmRankedPropertyHelper<T> where T : RankedMultiSelectBase
    {
        private readonly ITrmRankedPropertyRepository<T> _trmRankedPropertyRepository;

        public RankedPropertyHelper(ITrmRankedPropertyRepository<T> trmRankedPropertyRepository)
        {
            _trmRankedPropertyRepository = trmRankedPropertyRepository;
        }

        public string GetRankedMultiSelectDisplayName(string value)
        {
            var rankedItems = (from r in _trmRankedPropertyRepository.Items() where r.Value == value select r)
                .OrderBy(r => r.Rank);

            if (rankedItems != null && rankedItems.FirstOrDefault() != null)
            {
                var rankedItem = rankedItems.FirstOrDefault();
                return !string.IsNullOrEmpty(rankedItem.DisplayName)
                    ? rankedItem.DisplayName
                    : !string.IsNullOrEmpty(rankedItem.Value) ? rankedItem.Value : string.Empty;
            }
            return string.Empty;
        }

        //public IOrderedEnumerable<string> GetRankedMultiSelectDisplayNames(IEnumerable<string>  denoms )
        //{
        //    var firstResult = _trmRankedPropertyRepository.Find(x => denoms.Contains(x.Value)).OrderBy(x => x.Rank);
        //    //if (firstResult != null)
        //    //{
        //    //    return !string.IsNullOrEmpty(firstResult.DisplayName)
        //    //        ? firstResult.DisplayName
        //    //        : !string.IsNullOrEmpty(firstResult.Value) ? firstResult.Value : string.Empty;
        //    //}
        //    return (IOrderedEnumerable<string>) firstResult;
        //}
    }
}