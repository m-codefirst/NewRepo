using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.Shared.Facades
{
    [ServiceConfiguration(typeof(CustomerContextFacade), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CustomerContextFacade
    {
        public CustomerContextFacade()
        {
            CurrentContact = new CurrentContactFacade();
        }
        public virtual CurrentContactFacade CurrentContact { get; private set; }
        public virtual Guid CurrentContactId { get { return CustomerContext.Current.CurrentContactId; } }
        public virtual CustomerContact GetContactById(Guid contactId)
        {
            return CustomerContext.Current.GetContactById(contactId);
        }
    }
}
