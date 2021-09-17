using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Order;
using Hephaestus.Commerce.AddressBook.Services;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Helpers
{
    public class ContactHelper : IAmContactHelper
    {
        private readonly CustomerContext _customerContext;
        private readonly IAddressBookService _addressBookService;

        public ContactHelper(CustomerContext customerContext, IAddressBookService addressBookService)
        {
            _customerContext = customerContext;
            _addressBookService = addressBookService;
        }

        public CustomerContact GetCheckoutCustomerContact(string contactId = null)
        {
            if (_customerContext.CurrentContact != null) return _customerContext.CurrentContact;
            if (contactId == null) return _customerContext.GetContactById(new Guid(HttpContext.Current.Request.AnonymousID));
            return string.IsNullOrEmpty(contactId) ? null : _customerContext.GetContactById(new Guid(contactId));
        }

        public CustomerContact CreateCustomerContact(Guid id, UserRegistration user)
        {
            var contact = CustomerContact.CreateInstance();
            contact.PrimaryKeyId = (PrimaryKeyId)id;
            contact[StringConstants.CustomFields.ContactId] = id;
            contact.FullName = $"{user.FirstName} {user.LastName}";
            contact.Email = user.EmailAddress;
            contact.RegistrationSource = StringConstants.RegistrationSource.CheckoutPageSource;
            contact[StringConstants.CustomFields.IsGuest] = true;
            contact[StringConstants.CustomFields.ContactTitleField] = string.Empty;

            var address = CustomerAddress.CreateInstance();
            //_addressBookService.MapToAddress(addressModel, address);

            if (address != null)
            {
                address.AddressType = CustomerAddressTypeEnum.Shipping;
                contact.AddContactAddress(address);
                contact.SaveChanges();
                //contact.PreferredShippingAddressId = contact.ContactAddresses.FirstOrDefault(x => x.AddressType == CustomerAddressTypeEnum.Shipping)?.AddressId;
            }

            return contact.SaveChanges();
        }

        public CustomerContact ApplyPreferredAddresses(CustomerContact contact, IOrderGroup orderGroup)
        {
            var doSave = false;

            var shippingAddress = orderGroup.GetFirstShipment().ShippingAddress;

            if (contact.PreferredShippingAddress == null)
            {
                var addressModel = _addressBookService.ConvertToModel(shippingAddress);
                var address = CustomerAddress.CreateInstance();
                _addressBookService.MapToAddress(addressModel, address);

                if (address != null)
                {
                    address.AddressType = CustomerAddressTypeEnum.Shipping;
                    contact.AddContactAddress(address);
                    contact.SaveChanges();
                    contact.PreferredShippingAddressId = contact.ContactAddresses.FirstOrDefault(x => x.AddressType == CustomerAddressTypeEnum.Shipping)?.AddressId;
                    doSave = true;
                }
            }

            if (contact.PreferredBillingAddress == null)
            {
                var billingAddress = orderGroup.GetFirstForm()?.Payments.FirstOrDefault()?.BillingAddress;

                if (billingAddress == null)
                {
                    contact.PreferredBillingAddressId = contact.PreferredShippingAddressId;
                }
                else
                {
                    var addressModel = _addressBookService.ConvertToModel(billingAddress);
                    var address = CustomerAddress.CreateInstance();
                    _addressBookService.MapToAddress(addressModel, address);

                    if (address != null)
                    {
                        address.AddressType = CustomerAddressTypeEnum.Shipping;
                        contact.AddContactAddress(address);
                        contact.SaveChanges();
                        contact.PreferredBillingAddressId = contact.ContactAddresses.FirstOrDefault(x => x.AddressType == CustomerAddressTypeEnum.Billing)?.AddressId;
                        doSave = true;
                    }
                }
            }

            if (doSave) contact = contact.SaveChanges();
            return contact;
        }

        public CustomerContact GetOrCreateCheckoutCustomerContact(IOrderGroup orderGroup)
        {
            if (_customerContext.CurrentContact != null) return _customerContext.CurrentContact;
            var contact = _customerContext.GetContactById(orderGroup.CustomerId);

            var shippingAddress = orderGroup.GetFirstShipment().ShippingAddress;

            if (contact == null)
            {
                contact = CustomerContact.CreateInstance();
                contact.PrimaryKeyId = (PrimaryKeyId)orderGroup.CustomerId;
                contact[StringConstants.CustomFields.ContactId] = orderGroup.CustomerId;
                contact.FullName = $"{shippingAddress.FirstName} {shippingAddress.LastName}";
                contact.RegistrationSource = StringConstants.RegistrationSource.CheckoutPageSource;
                contact[StringConstants.CustomFields.IsGuest] = true;
                contact[StringConstants.CustomFields.ContactTitleField] = string.Empty;
            }

            if (contact.PreferredShippingAddress == null)
            {
                var addressModel = _addressBookService.ConvertToModel(shippingAddress);
                var address = CustomerAddress.CreateInstance();
                _addressBookService.MapToAddress(addressModel, address);

                if (address != null)
                {
                    address.AddressType = CustomerAddressTypeEnum.Shipping;
                    contact.AddContactAddress(address);
                    contact.SaveChanges();
                    contact.PreferredShippingAddressId = contact.ContactAddresses.FirstOrDefault(x => x.AddressType == CustomerAddressTypeEnum.Shipping)?.AddressId;
                }
            }
            else
            {
                var addressModel = _addressBookService.ConvertToModel(shippingAddress);
                var address = contact.ContactAddresses.FirstOrDefault(x => x.AddressType == CustomerAddressTypeEnum.Shipping);
                if (address != null)
                {
                    _addressBookService.MapToAddress(addressModel, address);
                    contact.UpdateContactAddress(address);
                    contact.SaveChanges();
                }
            }

            if (contact.PreferredBillingAddress == null)
            {
                var billingAddress = orderGroup.GetFirstForm()?.Payments.FirstOrDefault()?.BillingAddress;

                if (billingAddress == null)
                {
                    contact.PreferredBillingAddressId = contact.PreferredShippingAddressId;
                }
                else
                {
                    var addressModel = _addressBookService.ConvertToModel(billingAddress);
                    var address = CustomerAddress.CreateInstance();
                    _addressBookService.MapToAddress(addressModel, address);

                    if (address != null)
                    {
                        address.AddressType = CustomerAddressTypeEnum.Shipping;
                        contact.AddContactAddress(address);
                        contact.SaveChanges();
                        contact.PreferredBillingAddressId = contact.ContactAddresses.FirstOrDefault(x => x.AddressType == CustomerAddressTypeEnum.Billing)?.AddressId;
                    }
                }
            }

            contact.SaveChanges();

            return contact;
        }

        public void UpdateGuestContact(UserRegistration user, CustomerContact contact)
        {
            contact.FirstName = user.FirstName;
            contact.MiddleName = user.MiddleName;
            contact.LastName = user.LastName;
            contact.FullName = $"{user.FirstName} {user.LastName}";
            contact.BirthDate = user.DateOfBirth;
            contact.Email = user.EmailAddress;
            contact.RegistrationSource = StringConstants.RegistrationSource.CheckoutPageSource;
            contact[StringConstants.CustomFields.SecondSurname] = user.SecondLastName;
            contact[StringConstants.CustomFields.Gender] = user.Gender;
            contact[StringConstants.CustomFields.ContactTitleField] = user.Title;
            contact[StringConstants.CustomFields.Telephone] = user.Telephone;
            contact[StringConstants.CustomFields.ContactByEmail] = user.ByEmail;
            contact[StringConstants.CustomFields.ContactByPhone] = user.ByTelephone;
            contact[StringConstants.CustomFields.ContactByPost] = user.ByPost;
            contact[StringConstants.CustomFields.TelephoneConsentDateTime] = DateTime.Now;
            contact[StringConstants.CustomFields.TelephoneConsentSource] = Constants.StringConstants.CustomerContactSourcePreference.CheckoutRegistation;
            contact[StringConstants.CustomFields.EmailConsentDateTime] = DateTime.Now;
            contact[StringConstants.CustomFields.EmailConsentSource] = Constants.StringConstants.CustomerContactSourcePreference.CheckoutRegistation;
            contact[StringConstants.CustomFields.PostConsentDateTime] = DateTime.Now;
            contact[StringConstants.CustomFields.PostConsentSource] = Constants.StringConstants.CustomerContactSourcePreference.CheckoutRegistation;
            contact[StringConstants.CustomFields.IsGuest] = true;
            contact[StringConstants.CustomFields.CustomerType] = StringConstants.CustomerType.Consumer;
            contact.SaveChanges();
        }

        /// <summary>
        /// Only upgrade in case contact has the field "CustomerCustomerType" = Bullion
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public bool UpgradeBullionContactToBullionConsumerUser(CustomerContact contact)
        {
            var customerType = contact.GetStringProperty(StringConstants.CustomFields.CustomerType);

            //If contact's CustomerCustomerType != Bullion do nothing
            if (customerType == null || !customerType.Equals(StringConstants.CustomerType.Bullion)) return false;

            contact[StringConstants.CustomFields.CustomerType] = StringConstants.CustomerType.ConsumerAndBullion;
            contact.SaveChanges();
            return true;
        }

        public List<string> GetAllContactGroups()
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