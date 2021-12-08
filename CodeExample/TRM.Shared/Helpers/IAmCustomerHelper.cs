using Mediachase.Commerce.Customers;
using System.Collections.Generic;

namespace TRM.Shared.Helpers
{
    public interface IAmCustomerHelper
    {
        string GetStringProperty(CustomerContact customer, string propertyName);
        bool GetBoolProperty(CustomerContact customer, string propertyName);
        int GetIntProperty(CustomerContact customer, string propertyName);
        decimal GetDecimalProperty(CustomerContact customer, string propertyName);
        bool UpdateIntegerPropertyIfPossible(CustomerContact contact, string propertyName, string valueStr);
        bool UpdateBooleanPropertyIfPossible(CustomerContact contact, string propertyName, string valueStr);
        List<string> GetAllCustomerGroups();
    }
}