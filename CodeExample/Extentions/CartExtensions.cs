using System;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using TRM.Web.Business.Cart;
using TRM.Web.Constants;

namespace TRM.Web.Extentions
{
    public static class CartExtensions
    {
        public static T DeserializeProperty<T>(this IOrderGroup cart, string propertyName)
        {
            var serializeProperty = string.Empty;
            if (cart != null && !string.IsNullOrWhiteSpace(propertyName))
            {
                serializeProperty = cart.Properties[propertyName]?.ToString();
            }

            return string.IsNullOrWhiteSpace(serializeProperty) ? default(T) : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serializeProperty);
        }

        public static void SerializeProperty<T>(this IOrderGroup cart, string propertyName, T value)
        {
            var serializeProperty = Newtonsoft.Json.JsonConvert.SerializeObject(value);

            cart.Properties[propertyName] = serializeProperty;
        }

        public static void SerializePropertyAndSave<T>(this IOrderGroup cart, string propertyName, T value)
        {
            var serializeProperty = Newtonsoft.Json.JsonConvert.SerializeObject(value);

            cart.Properties[propertyName] = serializeProperty;

            var orderRepository = ServiceLocator.Current.GetInstance<IOrderRepository>();

            orderRepository.Save(cart);
        }

        public static IShipment GetSecondShipment(this IOrderGroup cart)
        {
            var firstForm = cart?.GetFirstForm();
            if (firstForm == null) return null;
            return firstForm.Shipments != null && firstForm.Shipments.Count >= 2 ? firstForm.Shipments.ElementAt(1) : null;
        }

        public static bool HasDeliveryAddress(this IOrderGroup cart)
        {
            return cart?.GetFirstShipment() != null &&
                   cart.GetFirstShipment().ShippingAddress != null &&
                   !string.IsNullOrWhiteSpace(cart.GetFirstShipment().ShippingAddress.Id);
        }

        public static bool HasPaymentMethod(this IOrderGroup cart)
        {
            if (cart?.GetFirstForm() == null) return false;

            var firstPayment = cart.GetFirstForm().Payments.FirstOrDefault();
            if (firstPayment == null) return false;

            return !string.IsNullOrWhiteSpace(firstPayment.PaymentMethodName) &&
                   firstPayment.PaymentMethodId != Guid.Empty;
        }

        public static string GetCartType(this IOrderGroup orderGroup)
        {
            var cartService = ServiceLocator.Current.GetInstance<ITrmCartService>();
            var cartName = orderGroup.Name;

            if (cartName.Equals(cartService.DefaultBullionCartName) || cartName.Equals(cartService.DefaultBuyNowCartName))
                return StringConstants.CartType.Bullion;

            return cartName.Equals(cartService.DefaultCartName) ? StringConstants.CartType.Consumer : string.Empty;
        }

        public static bool IsBuyNowCart(this IOrderGroup orderGroup)
        {
            var cartService = ServiceLocator.Current.GetInstance<ITrmCartService>();
            var cartName = orderGroup.Name;
            return cartName.Equals(cartService.DefaultBuyNowCartName);
        }

        public static bool IsConsumerCart(this IOrderGroup orderGroup)
        {
            var cartService = ServiceLocator.Current.GetInstance<ITrmCartService>();
            var cartName = orderGroup.Name;
            return cartName.Equals(cartService.DefaultCartName);
        }
    }
    }