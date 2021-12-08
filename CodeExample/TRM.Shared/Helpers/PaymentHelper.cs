using System;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Extensions;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Exceptions;
using Mediachase.Commerce.Orders.Managers;
using TRM.Shared.Constants;

namespace TRM.Shared.Helpers
{
    public class PaymentHelper : IAmPaymentHelper
    {
        private readonly IOrderGroupCalculator _orderGroupCalculator;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IOrderRepository _orderRepository;

        public PaymentHelper(IOrderGroupCalculator orderGroupCalculator, IOrderGroupFactory orderGroupFactory, CustomerContext customerContext, IOrderRepository orderRepository)
        {
            _orderGroupCalculator = orderGroupCalculator;
            _orderGroupFactory = orderGroupFactory;
            _orderRepository = orderRepository;
        }

        public Money GetCartTotalWithoutRecuringItems(IOrderGroup cart)
        {
            var cartToChange = new SerializableCart
            {
                Name = cart.Name,
                CustomerId = cart.CustomerId
            };

            cartToChange.CopyFrom(cart,_orderGroupFactory);
            
            foreach (var item in cartToChange.GetAllLineItems())
            {
                if ((item.Properties[StringConstants.CustomFields.MandatoryFieldName]?.Equals(true) ?? false) ||
                    (item.Properties[StringConstants.CustomFields.SubscribedFieldName]?.Equals(true) ?? false))
                {
                    item.IsGift = true;
                }
            }

            var amount = _orderGroupCalculator.GetTotal(cartToChange);
            
            return amount;
        }

        public bool DoesCartHaveRecurringItems(IOrderGroup cart)
        {
            return cart.GetAllLineItems().Any(item => item.Properties[StringConstants.CustomFields.MandatoryFieldName].Equals(true) || item.Properties[StringConstants.CustomFields.SubscribedFieldName].Equals(true));
        }

        public PaymentMethodDto.PaymentMethodRow GetPaymentMethod(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId)) return null;

            var id = Guid.Parse(paymentId);
            var paymentMethodDto = PaymentManager.GetPaymentMethod(id);
            var paymentMethod = paymentMethodDto.PaymentMethod.AsQueryable().FirstOrDefault();
            if (paymentMethod == null)
            {
                throw new PaymentException(PaymentException.ErrorType.ConfigurationError, "001",
                    "Unknown payment method.");
            }
            return paymentMethod;
        }

        public void SavePayment(IPayment payment, IOrderGroup orderGroup)
        {
            var cartPayment = orderGroup.GetFirstForm()?.Payments.FirstOrDefault();
            if (cartPayment == null) return;

            cartPayment.CopyPropertiesWithOverwriteFrom(payment);
            cartPayment.Status = payment.Status;
            _orderRepository.Save(orderGroup);
        }
        public void UpdatePaymentStatus(IPayment payment, IOrderGroup orderGroup, PaymentStatus newPaymentStatus)
        {
            payment.Status = newPaymentStatus.ToString();
            _orderRepository.Save(orderGroup);
        }
    }
}
