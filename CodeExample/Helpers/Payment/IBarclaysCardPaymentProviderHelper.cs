using System.Collections.Generic;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.Commerce.Orders.Dto;

namespace TRM.Web.Helpers.Payment
{
    public interface IBarclaysCardPaymentProviderHelper
    {
        Dictionary<string, string> GenerateParameters(PaymentMethodDto.PaymentMethodRow payMethod,
            AddressModel billingAddress, AddressModel deliveryAddress);

    }
}