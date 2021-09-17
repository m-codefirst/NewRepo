using Mediachase.BusinessFoundation.Data.Meta;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using System;
using System.Collections.Generic;

namespace TRM.Web.Helpers
{
    public interface IMetaFieldTypeHelper
    {
        MetaFieldType CreateEnumMetaFieldType(string fieldName, Type type);
        bool UpdateInitializedCollectionForEnumMetaFieldType(string fieldTypeName, IEnumerable<KeyValuePair<string, int>> newItems);
        void UpdateDefaultValueForMetaField(string clsName, string fieldName, string defaultValue);
        MetaEnumItem[] GetMetaEnumItems(string metaFieldTypeName);
    }
}
