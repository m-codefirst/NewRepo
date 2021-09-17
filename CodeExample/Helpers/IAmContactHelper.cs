using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Helpers
{
    public interface IAmContactHelper
    {
        CustomerContact GetCheckoutCustomerContact(string contactId = null);
        CustomerContact CreateCustomerContact(Guid id, UserRegistration user);
        CustomerContact ApplyPreferredAddresses(CustomerContact contact, IOrderGroup orderGroup);
        CustomerContact GetOrCreateCheckoutCustomerContact(IOrderGroup orderGroup);
        void UpdateGuestContact(UserRegistration user, CustomerContact contact);
        bool UpgradeBullionContactToBullionConsumerUser(CustomerContact contact);
        List<string> GetAllContactGroups();
    }
}