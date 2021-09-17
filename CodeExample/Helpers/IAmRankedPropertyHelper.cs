using TRM.Web.Models.Catalog.DDS;

namespace TRM.Web.Helpers
{
    // ReSharper disable once UnusedTypeParameter
    public interface IAmRankedPropertyHelper<T> where T : RankedMultiSelectBase
    {
        string GetRankedMultiSelectDisplayName(string value);
    }
}