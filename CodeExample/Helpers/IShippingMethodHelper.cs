using System.Collections.Generic;
using Mediachase.Commerce.Orders.Dto;

namespace TRM.Web.Helpers
{
    public interface IShippingMethodHelper
    {
        List<ShippingMethodDto.ShippingMethodRow> GetAvailableShippingMethods();
    }
}