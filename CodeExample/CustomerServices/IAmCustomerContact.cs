using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.CustomerServices
{
    public interface IAmCustomerContact
    {
        CustomerContact CurrentContact { get; }
    }
}
