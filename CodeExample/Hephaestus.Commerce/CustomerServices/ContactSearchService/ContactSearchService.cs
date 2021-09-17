using System;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.CustomerServices.ContactSearchService
{
    public class ContactSearchService : IAmContactSearchService
    {
        public IAmCustomerContact FindContact(Guid contactId)
        {
            var contact = CustomerContext.Current.GetContactById(contactId);

            var customerContact = new HephaestusCustomerContact(contact);
            return customerContact;
        }
    }
}