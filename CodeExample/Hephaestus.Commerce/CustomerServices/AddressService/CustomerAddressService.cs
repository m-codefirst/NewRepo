using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Logging.Compatibility;
using Hephaestus.Commerce.Extensions;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.CustomerServices.AddressService
{
    public class CustomerAddressService : ICustomerAddressService
    {
        public const string NoAddressToDeleteErrorMessage = "Customer Address does not exist against this customer to delete";

        protected readonly IAmCustomerContact CustomerContact;
        protected readonly ILog Logger = LogManager.GetLogger(typeof(CustomerAddressService));

        public CustomerAddressService(IAmCustomerContact customerContact)
        {
            CustomerContact = customerContact;
        }

        public virtual IEnumerable<CustomerAddress> AllAddresses
        {
            get
            {
                return CustomerContact.CurrentContact.ContactAddresses;
            }
        }

        public virtual CustomerAddress GetSpecificAddress(PrimaryKeyId? primaryKeyId)
        {
            return AllAddresses.SingleOrDefault(x => x.AddressId == primaryKeyId);
        }

        public virtual IEnumerable<CustomerAddress> GetSpecificAddresses(Func<CustomerAddress, bool> expr)
        {
            return AllAddresses.Where(expr);
        }

        public virtual PrimaryKeyId? DefaultShippingAddressId
        {
            get
            {
                return CustomerContact.CurrentContact.PreferredShippingAddressId;
            }
            set
            {
                var contact = CustomerContact.CurrentContact;
                contact.PreferredShippingAddressId = value;
                contact.SaveChanges();
            }
        }

        public virtual PrimaryKeyId? DefaultBillingAddressId
        {
            get
            {
                return CustomerContact.CurrentContact.PreferredBillingAddressId;
            }
            set
            {
                var contact = CustomerContact.CurrentContact;
                contact.PreferredBillingAddressId = value;
                contact.SaveChanges();
            }
        }

        public virtual void CreateAddress(CustomerAddress customerAddress)
        {
            var contact = CustomerContact.CurrentContact;

            if (contact.ContactAddresses.Any(x => x.CompareAddress(customerAddress)))
            {
                return;
            }

            contact.AddContactAddress(customerAddress);
            contact.SaveChanges();
        }

        public virtual void Update(CustomerAddress customerAddress)
        {
            var contact = CustomerContact.CurrentContact;
            contact.UpdateContactAddress(customerAddress);
            contact.SaveChanges();
        }

        public virtual void Delete(PrimaryKeyId? primaryKeyId)
        {
            var address = GetSpecificAddress(primaryKeyId);
            if (address == null)
            {
                Logger.Error(NoAddressToDeleteErrorMessage);
                throw new ApplicationException(NoAddressToDeleteErrorMessage);
            }

            if (DefaultBillingAddressId == address.PrimaryKeyId)
            {
                DefaultBillingAddressId = null;
            }

            if (DefaultShippingAddressId == address.PrimaryKeyId)
            {
                DefaultShippingAddressId = null;
            }

            address.Delete();
        }
    }
}
