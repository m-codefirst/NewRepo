using System;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;

namespace TRM.Shared.Extensions
{
    public static class EntityObjectExtensions
    {
        public static string GetStringProperty(this EntityObject contact, string propertyName)
        {
            if (contact == null) return string.Empty;

            var value = contact.Properties[propertyName]?.Value;
            return value?.ToString() ?? string.Empty;
        }

        public static bool GetBooleanProperty(this EntityObject contact, string propertyName)
        {
            if (contact == null) return false;

            var value = contact.Properties[propertyName]?.Value;
            var valueStr = value?.ToString() ?? string.Empty;

            bool valueBool;

            return bool.TryParse(valueStr, out valueBool) && valueBool;
        }

        public static int GetIntegerProperty(this EntityObject contact, string propertyName)
        {
            if (contact == null) return 0;

            var value = contact.Properties[propertyName]?.Value;
            var valueStr = value?.ToString() ?? string.Empty;

            int valueInt;

            return int.TryParse(valueStr, out valueInt) ? valueInt : 0;
        }

        public static T GetEnumProperty<T>(this EntityObject contact, string propertyName) where T : struct
        {
            if (contact == null) return default;

            var value = contact.Properties[propertyName]?.Value;
            var valueStr = value?.ToString() ?? string.Empty;

            if (!Enum.TryParse<T>(valueStr, out var parsed))
            {
                return default;
            }

            return parsed;
        }

        public static decimal GetDecimalProperty(this EntityObject contact, string propertyName)
        {
            var value = contact.Properties[propertyName]?.Value;
            var valueStr = value?.ToString() ?? string.Empty;

            decimal valueDec;

            return decimal.TryParse(valueStr, out valueDec) ? valueDec : 0;
        }

        public static DateTime? GetDatetimeProperty(this EntityObject contact, string propertyName)
        {
            var value = contact.Properties[propertyName]?.Value;
            var valueStr = value?.ToString();
            if (string.IsNullOrEmpty(valueStr)) return null;

            DateTime valueDate;

            return DateTime.TryParse(valueStr, out valueDate) ? valueDate : (DateTime?)null;
        }

        public static bool UpdateProperty(this EntityObject contact, string propertyName, object valueObj)
        {
            if (contact == null || !contact.Properties.Contains(propertyName) || valueObj == null) return false;

            var typeOf = valueObj.GetType();
            if (typeOf == typeof(DateTime))
            {
                var oldDateValue = contact.GetDatetimeProperty(propertyName);
                if (oldDateValue.HasValue)
                {
                    var newDateValue = ((DateTime)valueObj);
                    if (newDateValue == DateTime.MinValue) return false;
                    if (oldDateValue.Value.ToString("yyyy-MM-dd HH:mm:ss") == newDateValue.ToString("yyyy-MM-dd HH:mm:ss")) return false;
                }
            }
            else if (typeOf == typeof(decimal))
            {
                var oldDecValue = contact.GetDecimalProperty(propertyName);
                if (oldDecValue == (decimal)valueObj) return false;
            }
            else
            {
                var oldStrValue = contact.GetStringProperty(propertyName);
                var newStrValue = valueObj.ToString();
                if (oldStrValue == newStrValue) return false;
            }

            contact.Properties[propertyName].Value = valueObj;

            return true;
        }

        public static bool UpdateBooleanPropertyIfPossible(this EntityObject contact, string propertyName, string valueStr)
        {
            if (contact == null || !contact.Properties.Contains(propertyName)) return false;

            bool valueBln;
            if (bool.TryParse(valueStr, out valueBln))
            {
                contact.Properties[propertyName].Value = valueBln;
            }
            else
            {
                contact.Properties[propertyName].Value = valueStr;
            }
            return true;
        }

        public static bool UpdateIntegerPropertyIfPossible(this EntityObject contact, string propertyName, string valueStr)
        {
            if (contact == null || !contact.Properties.Contains(propertyName)) return false;

            int valueInt;
            return int.TryParse(valueStr, out valueInt) ? contact.UpdateProperty(propertyName, valueInt) : contact.UpdateProperty(propertyName, valueStr);
        }

        public static string Set3dsTransactionIdOnCustomer(this CustomerContact customer)
        {
            var customer3dsTransactionId = Guid.NewGuid().ToString();
            
            customer[StringConstants.CustomFields.MyAccountPayment3DsId] = customer3dsTransactionId;

            return customer3dsTransactionId;
        }
    }
}
