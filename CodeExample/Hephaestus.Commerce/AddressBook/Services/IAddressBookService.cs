using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.AddressBook.Services
{
    public interface IAddressBookService
    {
        IList<AddressModel> List();
        bool CanSave(AddressModel addressModel);
        void Save(AddressModel addressModel);
        void Delete(string addressId);
        void SetPreferredBillingAddress(string addressId);
        void SetPreferredShippingAddress(string addressId);
        CustomerAddress GetPreferredBillingAddress();
        void LoadAddress(AddressModel addressModel);
        void LoadCountriesAndRegionsForAddress(AddressModel addressModel);
        IEnumerable<string> GetRegionsByCountryCode(string countryCode);
        void MapToAddress(AddressModel addressModel, IOrderAddress orderAddress);
        void MapToAddress(AddressModel addressModel, CustomerAddress customerAddress);
        void MapToModel(CustomerAddress customerAddress, AddressModel addressModel);
        void MapToModel(CustomerAddress customerAddress, AddressModel addressModel, CustomerContact customerContact);
        IOrderAddress ConvertToAddress(AddressModel addressModel, IOrderGroup orderGroup);
        AddressModel ConvertToModel(IOrderAddress orderAddress);
        bool UseBillingAddressForShipment();
    }
}

