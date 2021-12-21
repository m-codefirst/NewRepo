using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Managers;
using System;
using static TRM.Shared.Constants.StringConstants;

namespace MetapackShippingProvider.Gateway
{
    [ServiceConfiguration(ServiceType = typeof(IShippingPlugin))]
    public class MetapackGateway : IShippingPlugin
    {
        public ShippingRate GetRate(IMarket market, Guid methodId, IShipment shipment, ref string message)
        {
            var bookingCode = shipment.Properties[CustomFields.ShippingBookingCode];
            var price = shipment.Properties[CustomFields.ShippingPrice];

            if (bookingCode == null || price == null)
                return null;

            var method = ShippingManager
                .GetShippingMethods(SiteContext.Current.LanguageName)
                .ShippingMethod.FindByShippingMethodId(methodId);

            return new ShippingRate(methodId, bookingCode.ToString(), new Money(decimal.Parse(price.ToString()), new Currency(method.Currency)));
        }
    }
}
