using Castle.Core.Internal;
using EPiServer.Logging.Compatibility;
using Mediachase.BusinessFoundation.Common;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.BusinessFoundation.Data.Meta;
using Mediachase.BusinessFoundation.Data.Meta.Management.SqlSerialization;

namespace TRM.Web.Helpers
{
    public class MetaFieldTypeHelper : IMetaFieldTypeHelper
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(MetaFieldTypeHelper));
        public MetaFieldType CreateEnumMetaFieldType(string fieldName, Type enumType)
        {
            if (!enumType.IsEnum)
            {
                _logger.ErrorFormat("{0} does not support type {1}, only Enum is valid", "CreateEnumMetaFieldType", enumType.Name);
                return null;
            }
            if (string.IsNullOrWhiteSpace(fieldName) || IsExisted(fieldName)) return null;

            MetaFieldType metaFieldType = null;

            try
            {
                metaFieldType = EntityObjectHelper.ManagedEnumToMetaEnum(enumType, fieldName);
            }
            catch (Exception ex)
            {
                _logger.ErrorFormat("Error when trying to create new MetaFieldType {0}, error message: {1}", fieldName, ex.Message);
            }
            return metaFieldType;
        }

        public bool UpdateInitializedCollectionForEnumMetaFieldType(string fieldTypeName, IEnumerable<KeyValuePair<string, int>> newItems)
        {
            MetaFieldType enumFieldType;
            const string methodName = "UpdateInitializedCollectionForEnumMetaFieldType";
            if (!TryGetFieldType(fieldTypeName, out enumFieldType))
            {
                _logger.ErrorFormat("{0}, The Enum Meta Field Type named {1} cannot be found.", methodName, fieldTypeName);
                return false;
            }

            RemoveAllExistingItems(enumFieldType);

            newItems = newItems?.Where(x => !string.IsNullOrWhiteSpace(x.Key)).Distinct();
            if (newItems.IsNullOrEmpty())
            {
                _logger.InfoFormat("{0}, Remove all the value for {1}", methodName, fieldTypeName);
                return true;
            }

            return AddNewItems(enumFieldType, newItems.ToList());
        }

        public MetaEnumItem[] GetMetaEnumItems(string metaFieldTypeName)
        {
            MetaFieldType enumFieldType;
            if (!TryGetFieldType(metaFieldTypeName, out enumFieldType))
            {
                _logger.ErrorFormat("{0}, The Enum Meta Field Type named cannot be found.",metaFieldTypeName);
                return new MetaEnumItem[] { };
            }

            return GetMetaEnumItems(enumFieldType);
        }

        private bool AddNewItems(MetaFieldType enumFieldType, List<KeyValuePair<string, int>> newItems)
        {
            foreach (var item in newItems)
            {
                MetaEnum.AddItem(enumFieldType, item.Value, item.Key, item.Value);
            }

            return CheckSynchronizeProcessSuccessful(enumFieldType, newItems);
        }

        private bool CheckSynchronizeProcessSuccessful(MetaFieldType enumFieldType, List<KeyValuePair<string, int>> newItems)
        {
            var savedEnumItems = GetMetaEnumItems(enumFieldType);
            foreach (var newItem in newItems)
            {
                var savedItem = savedEnumItems.FirstOrDefault(x => x.Handle == newItem.Value && x.Name.Equals(newItem.Key));
                if (savedItem == null) return false;
                if (savedItem.Handle != savedItem.OrderId) return false;
            }

            return true;
        }

        private MetaEnumItem[] GetMetaEnumItems(MetaFieldType type)
        {
            var list = new List<MetaEnumItem>();
            var array = mcmd_MetaEnumRow.List(new FilterElementCollection(FilterElement.EqualElement("TypeName", type.Name)), new SortingElementCollection(new SortingElement("OrderId", SortingElementType.Asc)));
            foreach (mcmd_MetaEnumRow row in array)
            {
                list.Add(new MetaEnumItem()
                {
                    Handle = row.Id,
                    Name = row.FriendlyName,
                    OrderId = row.OrderId,
                    Owner = row.Owner,
                    AccessLevel = (AccessLevel)row.AccessLevel
                });
            }
            return list.ToArray();
        }

        private void RemoveAllExistingItems(MetaFieldType enumFieldType)
        {
            var allExistingItemIds = MetaEnum.GetValues(enumFieldType);
            if (allExistingItemIds.IsNullOrEmpty()) return;
            foreach (var id in allExistingItemIds)
            {
                MetaEnum.RemoveItem(enumFieldType, id);
            }
        }

        private bool TryGetFieldType(string fieldName, out MetaFieldType result)
        {
            result = null;
            foreach (MetaFieldType fieldType in DataContext.Current.MetaModel.RegisteredTypes)
            {
                if (fieldType.Name == fieldName)
                {
                    result = fieldType;
                    return true;
                }
            }
            return false;
        }

        private bool IsExisted(string metaFieldName)
        {
            foreach (MetaFieldType fieldType in DataContext.Current.MetaModel.RegisteredTypes)
            {
                if (fieldType.Name == metaFieldName) { return true; }
            }
            return false;
        }

        public void UpdateDefaultValueForMetaField(string clsName, string fieldName, string defaultValue)
        {
            var customerMetadata = DataContext.Current.MetaModel.MetaClasses
            .Cast<MetaClass>()
            .First(mc => mc.Name == clsName);

            if (customerMetadata.Fields[fieldName] != null)
            {
                customerMetadata.Fields[fieldName].DefaultValue = defaultValue;
            }
        }
    }
}
