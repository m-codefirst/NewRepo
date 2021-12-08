using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Shared.Helpers
{
    public class CustomerHelper : IAmCustomerHelper
    {
        public T GetPropertyValue<T>(CustomerContact customer, string propertyName, IFormatProvider provider = null) where T : IConvertible
        {
            var defaultVal = default(T);
            if (customer == null || customer.Properties == null || !customer.Properties.Any())
                return defaultVal;

            var prop = customer.Properties[propertyName];
            if (prop == null || prop.Value == null) return defaultVal;

            return (T)Convert.ChangeType(prop.Value, typeof(T));
        }

        public string GetStringProperty(CustomerContact customer, string propertyName)
        {
            if (customer?.Properties?[propertyName] != null && customer?.Properties?[propertyName].Value != null)
            {
                return customer.Properties[propertyName].Value.ToString();
            }

            return string.Empty;
        }

        public bool GetBoolProperty(CustomerContact customer, string propertyName)
        {
            var returnValue = false;

            if (customer?.Properties?[propertyName] != null && customer.Properties[propertyName].Value != null)
            {
                if (bool.TryParse(customer.Properties[propertyName].Value.ToString(), out returnValue))
                {
                    return returnValue;
                }
            }

            return returnValue;
        }

        public int GetIntProperty(CustomerContact customer, string propertyName)
        {
            var returnValue = 0;

            if (customer?.Properties?[propertyName] != null && customer.Properties[propertyName].Value != null)
            {
                if (int.TryParse(customer.Properties[propertyName].Value.ToString(), out returnValue))
                {
                    return returnValue;
                }
            }

            return returnValue;
        }

        public decimal GetDecimalProperty(CustomerContact customer, string propertyName)
        {
            var returnValue = decimal.Zero;

            if (customer?.Properties?[propertyName] != null && customer.Properties[propertyName].Value != null)
            {
                if (decimal.TryParse(customer.Properties[propertyName].Value.ToString(), out returnValue))
                {
                    return returnValue;
                }
            }

            return returnValue;
        }

        public bool UpdateIntegerPropertyIfPossible(CustomerContact contact, string propertyName, string valueStr)
        {
            if (contact == null || !contact.Properties.Contains(propertyName)) return false;

            int valueInt;
            if (int.TryParse(valueStr, out valueInt))
            {
                contact.Properties[propertyName].Value = valueInt;
            }
            else
            {
                contact.Properties[propertyName].Value = valueStr;
            }

            return true;
        }

        public bool UpdateBooleanPropertyIfPossible(CustomerContact contact, string propertyName, string valueStr)
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

        public List<string> GetAllCustomerGroups()
        {
            MetaFieldType contactGroupEnumType = null;
            foreach (MetaFieldType fieldType in DataContext.Current.MetaModel.RegisteredTypes)
            {
                if (fieldType.Name == "ContactGroup")
                {
                    contactGroupEnumType = fieldType;
                    break;
                }
            }

            return contactGroupEnumType != null ? contactGroupEnumType.EnumItems.Select(x => x.Name).ToList() : new List<string>();
        }
    }
}