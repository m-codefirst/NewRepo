using System.Collections.Generic;
using Hephaestus.Commerce.Shared.Models;

namespace TRM.Web.Helpers
{
    public interface IAmAddressHelper
    {
        IEnumerable<AddressModel> GetAddressModelList();
        IEnumerable<AddressModel> GetAddressModelListForConsumerCheckout();
    }
}