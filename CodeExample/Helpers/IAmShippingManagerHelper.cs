using System;
using System.Collections.Generic;
using TRM.Web.Models;

namespace TRM.Web.Helpers
{
    public interface IAmShippingManagerHelper
    {
        List<ShippingMethodSummary> GetShippingMethodsByMarket(string marketid, bool returnInactive);

        ShippingMethodSummary GetShippingMethodSummary(Guid id);
    }
}