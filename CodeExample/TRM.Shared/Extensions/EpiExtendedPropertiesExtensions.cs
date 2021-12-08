using System;
using EPiServer.Commerce.Storage;
using Newtonsoft.Json;

namespace TRM.Shared.Extensions
{
    public static class EpiExtendedPropertiesExtensions
    {
        public static T GetPropertyValue<T>(this IExtendedProperties obj, string propertyName)
        {
            var strValue = obj.Properties[propertyName]?.ToString();
            if (string.IsNullOrEmpty(strValue))
            {
                return default(T);
            }

            return (T)Convert.ChangeType(strValue, typeof(T));
        }

        public static T DeserializePropertyValue<T>(this IExtendedProperties obj, string propertyName)
        {
            var strValue = obj.Properties[propertyName]?.ToString();
            if (!string.IsNullOrWhiteSpace(strValue))
            {
                return JsonConvert.DeserializeObject<T>(strValue);
            }

            return default(T);
        }

        public static string GetStringProperty(this IExtendedProperties obj, string propertyName)
        {
            if (obj == null) return string.Empty;

            var value = obj.Properties[propertyName];
            return value?.ToString() ?? string.Empty;
        }

        public static bool GetBooleanProperty(this IExtendedProperties obj, string propertyName)
        {
            if (obj == null) return false;

            var value = obj.Properties[propertyName];
            var valueStr = value?.ToString() ?? string.Empty;

            bool valueBool;

            return bool.TryParse(valueStr, out valueBool) && valueBool;
        }

        public static int GetIntegerProperty(this IExtendedProperties obj, string propertyName)
        {
            if (obj == null) return 0;

            var value = obj.Properties[propertyName];
            var valueStr = value?.ToString() ?? string.Empty;

            int valueInt;

            return int.TryParse(valueStr, out valueInt) ? valueInt : 0;
        }

        public static decimal GetDecimalProperty(this IExtendedProperties obj, string propertyName)
        {
            if (obj == null) return 0;

            var value = obj.Properties[propertyName];
            var valueStr = value?.ToString() ?? string.Empty;

            decimal valueDec;

            return decimal.TryParse(valueStr, out valueDec) ? valueDec : 0;
        }
    }
}