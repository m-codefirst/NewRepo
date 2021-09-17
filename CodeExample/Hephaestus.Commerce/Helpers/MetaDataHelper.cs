using Mediachase.BusinessFoundation.Core;
using Mediachase.BusinessFoundation.Data.Meta.Management;

namespace Hephaestus.Commerce.Helpers
{
    class MetaDataHelper : IAmMetaDataHelper
    {
        public MetaFieldType GetEnumByName(string name)
        {
            return MetaDataWrapper.GetEnumByName(name);
        }
    }
}
