using Mediachase.Commerce.Orders;

namespace Hephaestus.Commerce.CustomerServices
{
    public interface IAmOrderContext
    {
        OrderContext CurrentOrderContext { get; }
    }
}
