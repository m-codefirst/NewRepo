using Hephaestus.Commerce.Constants;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.Extensions
{
    public static class CustomerAddressExtensions
    {
        public static bool CompareAddress(this CustomerAddress customerAddress, CustomerAddress comparatorCustomerAddress)
        {
            return customerAddress.AddressType == comparatorCustomerAddress.AddressType
                   && (customerAddress[CustomerAddressFields.TitleConstant] 
                        ?? string.Empty).Equals(comparatorCustomerAddress[CustomerAddressFields.TitleConstant])
                   && customerAddress.FirstName == comparatorCustomerAddress.FirstName
                   && customerAddress.LastName == comparatorCustomerAddress.LastName
                   && customerAddress.DaytimePhoneNumber == comparatorCustomerAddress.DaytimePhoneNumber
                   && customerAddress.EveningPhoneNumber == comparatorCustomerAddress.EveningPhoneNumber
                   && customerAddress.OrganizationName == comparatorCustomerAddress.OrganizationName
                   && customerAddress.Line1 == comparatorCustomerAddress.Line1
                   && customerAddress.Line2 == comparatorCustomerAddress.Line2
                   && (customerAddress[CustomerAddressFields.AddressLine3Constant]
                        ?? string.Empty).Equals(comparatorCustomerAddress[CustomerAddressFields.AddressLine3Constant])
                   && customerAddress.City == comparatorCustomerAddress.City
                   && customerAddress.State == comparatorCustomerAddress.State
                   && customerAddress.PostalCode == comparatorCustomerAddress.PostalCode
                   && customerAddress.CountryCode == comparatorCustomerAddress.CountryCode;
        }
    }
}
