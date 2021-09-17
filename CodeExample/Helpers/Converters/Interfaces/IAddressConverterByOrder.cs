using Hephaestus.Commerce.Shared.Models;
using TRM.Web.Models.EntityFramework.Orders;

namespace TRM.Web.Helpers.Converters.Interfaces
{
    public interface IAddressConverterByOrder
    {
        AddressModel ConvertToDelivery(Order order);
        AddressModel ConvertToBilling(Order order);
    }
}
