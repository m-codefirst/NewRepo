using Hephaestus.Commerce.Shared.Models;
using TRM.Web.Helpers.Converters.Interfaces;
using TRM.Web.Models.EntityFramework.Orders;

namespace TRM.Web.Helpers.Converters
{
    public class AddressConverterByOrder : IAddressConverterByOrder
    {
        public AddressModel ConvertToDelivery(Order order)
        {
            return new AddressModel
            {
                FirstName = order.DeliveryName,
                Line1 = order.DeliveryStreet,
                City = order.DeliveryCity,
                CountryRegion = new CountryRegionModel
                { 
                    Region = order.DeliveryCounty
                },
                CountryCode = order.DeliveryCountryCode,
                PostalCode = order.DeliveryPostCode
            };
        }
        public AddressModel ConvertToBilling(Order order)
        {
            return new AddressModel
            {
                FirstName = order.BillingName,
                Line1 = order.BillingStreet,
                City = order.BillingCity,
                CountryRegion = new CountryRegionModel
                {
                    Region = order.BillingState
                },
                CountryCode = order.BillingCountryCode,
                PostalCode = order.BillingPostCode
            };
        }
    }
}