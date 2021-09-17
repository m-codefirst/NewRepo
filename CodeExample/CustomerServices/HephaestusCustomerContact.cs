using System;
using EPiServer.Logging;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.CustomerServices
{
    public class HephaestusCustomerContact : IAmCustomerContact
    {
        protected readonly ILogger Logger = LogManager.GetLogger(typeof(HephaestusCustomerContact));

        public const string ErrorMessage = "Customer Contact does not exist";

        private readonly CustomerContact _customerContact;

        public HephaestusCustomerContact()
        {
            
        }

        public HephaestusCustomerContact(CustomerContact customerContact)
        {
            _customerContact = customerContact;
        }

        public virtual CustomerContact CurrentContact
        {
            get
            {
                if (_customerContact != null)
                    return _customerContact;

                if (CustomerContext.Current.CurrentContact != null)
                {
                    return CustomerContext.Current.CurrentContact;
                }
                Logger.Error(ErrorMessage);
                return null;
            }
        }
    }
}
