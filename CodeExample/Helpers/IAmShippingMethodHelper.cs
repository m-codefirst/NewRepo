using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using TRM.Web.Models;

namespace TRM.Web.Helpers
{
    public interface IAmShippingMethodHelper
    {
        List<ShippingMethodSummary> GetAvailableShippingMethodsForConsumerCart(IOrderGroup orderGroup, string countryCode);
        ShippingMethodSummary GetBullionDefaultShippingMethod(IOrderGroup orderGroup, string countryCode, bool isToVault);
        ShippingMethodSummary GetBullionDefaultShippingMethod(IOrderGroup orderGroup, string countryCode, bool isToVault, CustomerContact customerContact);
        ShippingMethodSummary GetShippingMethodForDeliverFromVault(CustomerContact contact, decimal amount, decimal weight, string currencyCode);
        void UpdateBullionShippingMethod(IOrderGroup cart, int? shipmentId);
        void UpdateBullionShippingMethod(IOrderGroup cart, int? shipmentId, CustomerContact customerContact);
        bool ValidateShipping(IOrderGroup orderGroup);
    }
}