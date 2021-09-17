using Mediachase.BusinessFoundation.Data.Meta.Management;

namespace Hephaestus.Commerce.Helpers
{
    public interface IAmMetaDataHelper
    {
        MetaFieldType GetEnumByName(string name);
    }
}
