using EPiServer.Commerce.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using static TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Services.Metapack.Extensions
{
    public static class IPurchaseOrderExtensions
    {
        public static void ApplyShipment(this IPurchaseOrder purchaseOrder, IOrderGroup orderGroup)
        {
            var cartShipment = orderGroup.GetShipment();
            var shippingBookingCode = cartShipment.Properties[CustomFields.ShippingBookingCode];
            var shippingPrice = cartShipment.Properties[CustomFields.ShippingPrice];

            var orderShipment = purchaseOrder.Forms.First().Shipments.First();
            orderShipment.Properties[CustomFields.ShippingBookingCode] = shippingBookingCode;
            orderShipment.Properties[CustomFields.ShippingPrice] = shippingPrice;
        }
    }

    public static class IOrderGroupExtensions
    {
        public static IShipment GetShipment(this IOrderGroup orderGroup)
        {
            if (orderGroup.Forms == null || orderGroup.Forms.Count == 0)
                throw new InvalidOperationException("Forms cannot be null or empty");

            if (orderGroup.Forms.Any(x => x.Shipments == null || x.Shipments.Count == 0))
                throw new InvalidOperationException("Shipments cannot be null or empty");

            return orderGroup.Forms.First().Shipments.First();
        }

        public static void SetShipment(this IOrderGroup orderGroup, Guid methodId, string bookingCode, decimal price)
        {
            if (orderGroup.Forms == null || orderGroup.Forms.Count == 0)
                throw new InvalidOperationException("Forms cannot be null or empty");

            if (orderGroup.Forms.Any(x => x.Shipments == null || x.Shipments.Count == 0))
                throw new InvalidOperationException("Shipments cannot be null or empty");

            var shipment = orderGroup.GetShipment();

            if (string.IsNullOrWhiteSpace(shipment.ShippingMethodName))
            {
                shipment.ShippingMethodId = methodId;
                shipment.Properties[CustomFields.ShippingBookingCode] = bookingCode;
                shipment.Properties[CustomFields.ShippingPrice] = price;
            }
        }

        public static IShipment GetShipmentForVault(this IOrderGroup orderGroup)
        {
            if (orderGroup.Forms == null || orderGroup.Forms.Count == 0)
                throw new InvalidOperationException("Forms cannot be null or empty");

            if (orderGroup.Forms.Any(x => x.Shipments == null || x.Shipments.Count == 0))
                throw new InvalidOperationException("Shipments cannot be null or empty");

            return orderGroup.Forms.First().Shipments.Skip(1).First();
        }

        public static void SetShipmentForVault(this IOrderGroup orderGroup, Guid methodId)
        {
            if (orderGroup.Forms == null || orderGroup.Forms.Count == 0)
                throw new InvalidOperationException("Forms cannot be null or empty");

            if (orderGroup.Forms.Any(x => x.Shipments == null || x.Shipments.Count == 0))
                throw new InvalidOperationException("Shipments cannot be null or empty");

            var shipment = orderGroup.GetShipmentForVault();

            if (string.IsNullOrWhiteSpace(shipment.ShippingMethodName))
            {
                shipment.ShippingMethodId = methodId;
            }
        }
    }
}