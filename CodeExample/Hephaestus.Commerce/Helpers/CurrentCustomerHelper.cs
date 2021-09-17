using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Customers.Profile;
using Mediachase.Commerce.Security;

namespace Hephaestus.Commerce.Helpers
{
    [ServiceConfiguration(typeof(ICurrentCustomerHelper), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CurrentCustomerHelper : ICurrentCustomerHelper
    {
        public CustomerContact CustomerContact
        {
            get
            {
                return CustomerContext.CurrentContact;
            }
        }

        public CustomerContext CustomerContext
        {
            get
            {
                return CustomerContext.Current;
            }
        }

        public CustomerProfileWrapper CustomerProfile
        {
            get
            {
                return SecurityContext.Current.CurrentUserProfile as CustomerProfileWrapper;
            }
        }
    }
}