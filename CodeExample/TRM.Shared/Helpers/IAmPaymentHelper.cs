using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;

namespace TRM.Shared.Helpers
{
    public interface IAmPaymentHelper
    {
        Money GetCartTotalWithoutRecuringItems(IOrderGroup cart);
        bool DoesCartHaveRecurringItems(IOrderGroup cart);
        PaymentMethodDto.PaymentMethodRow GetPaymentMethod(string paymentId);
        void SavePayment(IPayment payment, IOrderGroup orderGroup);
        void UpdatePaymentStatus(IPayment payment, IOrderGroup orderGroup, PaymentStatus newPaymentStatus);
    }
}