using System;
using Mediachase.Commerce.Customers;

namespace TRM.Web.Helpers
{
    public interface IAmVatHelper
    {
        void UpdateCustomerGroup(string countryCode);
        void UpdateCustomerGroup(string countryCode, CustomerContact customer);
        bool IsNoneVatPricedDeliveryCountry(string countryCode);
    }
}