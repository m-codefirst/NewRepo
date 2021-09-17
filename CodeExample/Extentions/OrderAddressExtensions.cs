using EPiServer.Commerce.Order;
using Hephaestus.Commerce.Shared.Models;
using TRM.IntegrationServices.Models.Export.Orders;

namespace TRM.Web.Extentions
{
    public static class OrderAddressExtensions
    {
        public static string ToFullAddressText(this IOrderAddress address)
        {
            return address == null ?
                string.Empty 
                : $"{DisplayParam(address.Line1)}{DisplayParam(address.Line2)}{DisplayParam(address.City)}{DisplayParam(address.CountryName)}{DisplayParam(address.PostalCode, false)}";
        }

        public static string ToFullAddressText(this AddressDto address)
        {
            var stringAddress = string.Empty;

            if (address == null) return string.Empty;

            if (!string.IsNullOrEmpty(address.Line1)) stringAddress += address.Line1 + ", ";

            if (!string.IsNullOrEmpty(address.Line2)) stringAddress += address.Line2 + ", ";
            
            if (!string.IsNullOrEmpty(address.City)) stringAddress += address.City + ", ";

            if (!string.IsNullOrEmpty(address.PostCode)) stringAddress += address.PostCode;

            return stringAddress;
        }

        public static string ToFullAddressText(this AddressModel address)
        {
            return address == null ?
                string.Empty
                : $"{DisplayParam(address.Line1)}{DisplayParam(address.Line2)}{DisplayParam(address.City)}{DisplayParam(address.CountryName)}{DisplayParam(address.PostalCode, false)}";
        }

        private static string DisplayParam(string value, bool space = true)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value + (space ? ", " : "");
        }
    }
}