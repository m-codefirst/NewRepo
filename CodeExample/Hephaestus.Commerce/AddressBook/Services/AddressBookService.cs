using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.Shared.Facades;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace Hephaestus.Commerce.AddressBook.Services
{
    [ServiceConfiguration(typeof(IAddressBookService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class AddressBookService : IAddressBookService
    {
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly CustomerContextFacade _customerContext;
        private readonly CountryManagerFacade _countryManager;

        public AddressBookService(IOrderGroupFactory orderGroupFactory, CustomerContextFacade customerContext, CountryManagerFacade countryManager)
        {
            _orderGroupFactory = orderGroupFactory;
            _customerContext = customerContext;
            _countryManager = countryManager;
        }

        public void MapToModel(CustomerAddress customerAddress, AddressModel addressModel)
        {
            MapToModel(customerAddress, addressModel, null);
        }
        public void MapToModel(CustomerAddress customerAddress, AddressModel addressModel, CustomerContact customerContact)
        {
            addressModel.Line1 = customerAddress.Line1;
            addressModel.Line2 = customerAddress.Line2;
            addressModel.City = customerAddress.City;
            addressModel.CountryName = customerAddress.CountryName;
            if (!string.IsNullOrWhiteSpace(customerAddress.CountryCode))
            {
                addressModel.CountryCode = customerAddress.CountryCode;
            }
            addressModel.Email = customerAddress.Email;
            addressModel.FirstName = customerAddress.FirstName;
            addressModel.LastName = customerAddress.LastName;
            if (!string.IsNullOrWhiteSpace(customerAddress.PostalCode))
            {
                addressModel.PostalCode = customerAddress.PostalCode;
            }
            addressModel.CountryRegion = new CountryRegionModel
            {
                Region = customerAddress.RegionName ?? customerAddress.RegionCode ?? customerAddress.State
            };
            addressModel.ShippingDefault = customerContact != null ?
                                            (customerContact.PreferredShippingAddress != null && customerAddress.Name == customerContact.PreferredShippingAddress.Name) :
                                            (_customerContext.CurrentContact.PreferredShippingAddress != null && customerAddress.Name == _customerContext.CurrentContact.PreferredShippingAddress.Name);

            addressModel.BillingDefault = customerContact != null ?
                                            (customerContact.PreferredBillingAddress != null && customerAddress.Name == customerContact.PreferredBillingAddress.Name) :
                                            (_customerContext.CurrentContact.PreferredBillingAddress != null && customerAddress.Name == _customerContext.CurrentContact.PreferredBillingAddress.Name);

            addressModel.AddressId = customerAddress.Name;
            addressModel.Name = customerAddress.Name;
            addressModel.DaytimePhoneNumber = customerAddress.DaytimePhoneNumber;
        }

        public void MapToModel(IOrderAddress orderAddress, AddressModel addressModel)
        {
            addressModel.AddressId = orderAddress.Id;
            addressModel.Name = orderAddress.Id;
            addressModel.Line1 = orderAddress.Line1;
            addressModel.Line2 = orderAddress.Line2;
            addressModel.City = orderAddress.City;
            addressModel.CountryName = orderAddress.CountryName;
            if (!string.IsNullOrWhiteSpace(orderAddress.CountryCode))
            {
                addressModel.CountryCode = orderAddress.CountryCode;
            }
            addressModel.Email = orderAddress.Email;
            addressModel.FirstName = orderAddress.FirstName;
            addressModel.LastName = orderAddress.LastName;
            if (!string.IsNullOrWhiteSpace(orderAddress.PostalCode))
            {
                addressModel.PostalCode = orderAddress.PostalCode;
            }
            addressModel.CountryRegion = new CountryRegionModel
            {
                Region = orderAddress.RegionName ?? orderAddress.RegionCode
            };
            addressModel.DaytimePhoneNumber = orderAddress.DaytimePhoneNumber;
        }

        public void MapToAddress(AddressModel addressModel, IOrderAddress orderAddress)
        {
            orderAddress.Id = addressModel.Name;
            orderAddress.City = addressModel.City;
            orderAddress.CountryCode = addressModel.CountryCode;
            orderAddress.CountryName = _countryManager.GetCountries().Country.Where(x => x.Code == addressModel.CountryCode).Select(x => x.Name).FirstOrDefault();
            orderAddress.FirstName = addressModel.FirstName;
            orderAddress.LastName = addressModel.LastName;
            orderAddress.Line1 = addressModel.Line1;
            orderAddress.Line2 = addressModel.Line2;
            orderAddress.DaytimePhoneNumber = addressModel.DaytimePhoneNumber;
            orderAddress.PostalCode = addressModel.PostalCode;
            orderAddress.RegionName = addressModel.CountryRegion.Region;
            orderAddress.RegionCode = addressModel.CountryRegion.Region;
            orderAddress.Email = addressModel.Email;
            orderAddress.Organization = addressModel.Organization;
        }

        public void MapToAddress(AddressModel addressModel, CustomerAddress customerAddress)
        {
            customerAddress.Name = addressModel.Name;
            customerAddress.City = addressModel.City;
            if (!string.IsNullOrWhiteSpace(addressModel.CountryCode))
            {
                customerAddress.CountryCode = addressModel.CountryCode;
            }
            customerAddress.CountryName = _countryManager.GetCountries().Country.Where(x => x.Code == addressModel.CountryCode).Select(x => x.Name).FirstOrDefault();
            customerAddress.FirstName = addressModel.FirstName;
            customerAddress.LastName = addressModel.LastName;
            customerAddress.Line1 = addressModel.Line1;
            customerAddress.Line2 = addressModel.Line2;
            customerAddress.DaytimePhoneNumber = addressModel.DaytimePhoneNumber;
            if (!string.IsNullOrWhiteSpace(addressModel.PostalCode))
            { 
                customerAddress.PostalCode = addressModel.PostalCode; 
            }
            customerAddress.RegionName = addressModel.CountryRegion.Region;
            customerAddress.RegionCode = addressModel.CountryRegion.Region;
            // Commerce Manager expects State to be set for addresses in order management. Set it to be same as
            // RegionName to avoid issues.
            customerAddress.State = addressModel.CountryRegion.Region;
            customerAddress.Email = addressModel.Email;
            customerAddress.AddressType =
                CustomerAddressTypeEnum.Public |
                (addressModel.ShippingDefault ? CustomerAddressTypeEnum.Shipping : 0) |
                (addressModel.BillingDefault ? CustomerAddressTypeEnum.Billing : 0);
        }

        public IOrderAddress ConvertToAddress(AddressModel model, IOrderGroup orderGroup)
        {
            var address = orderGroup.CreateOrderAddress(_orderGroupFactory, model.Name);
            MapToAddress(model, address);

            return address;
        }

        public AddressModel ConvertToModel(IOrderAddress orderAddress)
        {
            var address = new AddressModel();

            if (orderAddress != null)
            {
                MapToModel(orderAddress, address);
            }

            return address;
        }


        public bool CanSave(AddressModel addressModel)
        {
            return !_customerContext.CurrentContact.ContactAddresses.Any(x =>
                x.Name.Equals(addressModel.Name, StringComparison.InvariantCultureIgnoreCase) &&
                x.Name != addressModel.AddressId);
        }

        public void Save(AddressModel addressModel)
        {
            var currentContact = _customerContext.CurrentContact;
            var customerAddress = CreateOrUpdateCustomerAddress(_customerContext.CurrentContact, addressModel);

            if (addressModel.BillingDefault)
            {
                currentContact.PreferredBillingAddress = customerAddress;
            }
            else if (currentContact.PreferredBillingAddress != null && currentContact.PreferredBillingAddress.Name.Equals(addressModel.AddressId))
            {
                currentContact.PreferredBillingAddressId = null;
            }

            if (addressModel.ShippingDefault)
            {
                currentContact.PreferredShippingAddress = customerAddress;
            }
            else if (currentContact.PreferredShippingAddress != null && currentContact.PreferredShippingAddress.Name.Equals(addressModel.AddressId))
            {
                currentContact.PreferredShippingAddressId = null;
            }

            currentContact.SaveChanges();
        }

        public void Delete(string addressId)
        {
            var currentContact = _customerContext.CurrentContact;
            var customerAddress = GetAddress(_customerContext.CurrentContact, addressId);
            if (customerAddress == null)
            {
                return;
            }
            if (currentContact.PreferredBillingAddressId == customerAddress.PrimaryKeyId || currentContact.PreferredShippingAddressId == customerAddress.PrimaryKeyId)
            {
                currentContact.PreferredBillingAddressId = currentContact.PreferredBillingAddressId == customerAddress.PrimaryKeyId ? null : currentContact.PreferredBillingAddressId;
                currentContact.PreferredShippingAddressId = currentContact.PreferredShippingAddressId == customerAddress.PrimaryKeyId ? null : currentContact.PreferredShippingAddressId;
                currentContact.SaveChanges();
            }
            currentContact.DeleteContactAddress(customerAddress);
            currentContact.SaveChanges();
        }

        public void SetPreferredBillingAddress(string addressId)
        {
            var currentContact = _customerContext.CurrentContact;
            var customerAddress = GetAddress(_customerContext.CurrentContact, addressId);
            if (customerAddress == null)
            {
                return;
            }
            currentContact.PreferredBillingAddress = customerAddress;
            currentContact.SaveChanges();
        }

        public void SetPreferredShippingAddress(string addressId)
        {
            var currentContact = _customerContext.CurrentContact;
            var customerAddress = GetAddress(_customerContext.CurrentContact, addressId);
            if (customerAddress == null)
            {
                return;
            }
            currentContact.PreferredShippingAddress = customerAddress;
            currentContact.SaveChanges();
        }

        public CustomerAddress GetPreferredBillingAddress()
        {
            return _customerContext.CurrentContact != null ?
                _customerContext.CurrentContact.PreferredBillingAddress
                : null;
        }

        public void LoadAddress(AddressModel addressModel)
        {
            addressModel.CountryOptions = GetAllCountries();

            var countryOptions = addressModel.CountryOptions.ToList();

            if (addressModel.CountryCode == null && countryOptions.Any())
            {
                addressModel.CountryCode = countryOptions.First().Code;
            }

            if (!string.IsNullOrEmpty(addressModel.AddressId))
            {
                var existingCustomerAddress = GetAddress(_customerContext.CurrentContact, addressModel.AddressId);

                if (existingCustomerAddress != null)
                {
                    MapToModel(existingCustomerAddress, addressModel);
                }
            }

            if (!string.IsNullOrEmpty(addressModel.CountryCode))
            {
                if (addressModel.CountryRegion == null)
                {
                    addressModel.CountryRegion = new CountryRegionModel();
                }
                addressModel.CountryRegion.RegionOptions = GetRegionsByCountryCode(addressModel.CountryCode);
            }
        }

        public IList<AddressModel> List()
        {
            var currentContact = _customerContext.CurrentContact;
            var addresses = new List<AddressModel>();

            if (currentContact != null)
            {
                addresses.AddRange(currentContact.ContactAddresses.Select(customerAddress => new AddressModel
                {
                    AddressId = customerAddress.Name,
                    Name = customerAddress.Name,
                    FirstName = customerAddress.FirstName,
                    LastName = customerAddress.LastName,
                    Line1 = customerAddress.Line1,
                    Line2 = customerAddress.Line2,
                    PostalCode = customerAddress.PostalCode,
                    City = customerAddress.City,
                    CountryCode = customerAddress.CountryCode,
                    CountryName = customerAddress.CountryName,
                    CountryRegion = new CountryRegionModel()
                    {
                        Region = customerAddress.RegionName ?? customerAddress.RegionCode ?? customerAddress.State
                    },
                    Email = customerAddress.Email,
                    ShippingDefault = _customerContext.CurrentContact.PreferredShippingAddress != null
                                            && customerAddress.AddressId == _customerContext.CurrentContact.PreferredShippingAddressId,
                    BillingDefault = _customerContext.CurrentContact.PreferredBillingAddress != null
                                            && customerAddress.AddressId == _customerContext.CurrentContact.PreferredBillingAddressId
                }));
            }

            return addresses;
        }

        public virtual CountryDto.CountryRow GetCountryByCountryCode(string countryCode)
        {
            var dataset = CountryManager.GetCountry(countryCode, false);
            var table = dataset.Country;

            return (table.Rows.Count == 1) ? table.Rows[0] as CountryDto.CountryRow : null;
        }

        public IEnumerable<string> GetRegionsByCountryCode(string countryCode)
        {
            var country = _countryManager.GetCountryByCountryCode(countryCode);
            return country != null ? GetRegionsForCountry(country) : Enumerable.Empty<string>();
        }

        public void LoadCountriesAndRegionsForAddress(AddressModel addressModel)
        {
            addressModel.CountryOptions = GetAllCountries();

            // Try get the address country first by country code, then by name, else use the first in list as final fallback.
            var selectedCountry = (GetCountryByCode(addressModel) ??
                                   GetCountryByName(addressModel)) ??
                                   addressModel.CountryOptions.FirstOrDefault();

            addressModel.CountryRegion.RegionOptions = selectedCountry != null ?
                GetRegionsByCountryCode(selectedCountry.Code) :
                Enumerable.Empty<string>();
        }

        public bool UseBillingAddressForShipment()
        {
            var customer = _customerContext.CurrentContact;
            return customer == null ||
                   (customer.PreferredShippingAddressId.HasValue &&
                    customer.PreferredShippingAddressId == customer.PreferredBillingAddressId);
        }

        private CountryModel GetCountryByCode(AddressModel addressModel)
        {
            var selectedCountry = addressModel.CountryOptions.FirstOrDefault(x => x.Code == addressModel.CountryCode);
            if (selectedCountry != null)
            {
                addressModel.CountryName = selectedCountry.Name;
            }
            return selectedCountry;
        }

        private CountryModel GetCountryByName(AddressModel addressModel)
        {
            var selectedCountry = addressModel.CountryOptions.FirstOrDefault(x => x.Name == addressModel.CountryName);
            if (selectedCountry != null)
            {
                addressModel.CountryCode = selectedCountry.Code;
            }
            return selectedCountry;
        }

        private IEnumerable<string> GetRegionsForCountry(CountryDto.CountryRow country)
        {
            return country == null ? Enumerable.Empty<string>() : country.GetStateProvinceRows().Select(x => x.Name).ToList();
        }

        private CustomerAddress CreateOrUpdateCustomerAddress(CurrentContactFacade contact, AddressModel addressModel)
        {

            var customerAddress = GetAddress(_customerContext.CurrentContact, addressModel.AddressId);
            var isNew = customerAddress == null;
            IEnumerable<PrimaryKeyId> existingId = contact.ContactAddresses.Select(a => a.AddressId).ToList();
            if (isNew)
            {
                customerAddress = CustomerAddress.CreateInstance();
            }

            MapToAddress(addressModel, customerAddress);

            if (isNew)
            {
                contact.AddContactAddress(customerAddress);
            }
            else
            {
                contact.UpdateContactAddress(customerAddress);
            }

            contact.SaveChanges();

            return customerAddress;
        }

        private CustomerAddress GetAddress(CurrentContactFacade contact, string addressId)
        {
            return contact.ContactAddresses.FirstOrDefault(x => x.Name == addressId);
        }

        private IEnumerable<CountryModel> GetAllCountries()
        {
            return _countryManager.GetCountries().Country.Select(x => new CountryModel { Code = x.Code, Name = x.Name });
        }
    }
}
