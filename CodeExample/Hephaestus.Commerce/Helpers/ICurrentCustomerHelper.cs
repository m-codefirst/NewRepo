using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Customers.Profile;

namespace Hephaestus.Commerce.Helpers
{
    public interface ICurrentCustomerHelper
    {
        CustomerContact CustomerContact { get; }

        CustomerContext CustomerContext { get; }

        CustomerProfileWrapper CustomerProfile { get; }
    }
}