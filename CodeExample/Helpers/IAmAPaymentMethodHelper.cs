using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders.Dto;

namespace TRM.Web.Helpers
{
    public interface IAmAPaymentMethodHelper
    {
        bool IsSupportedForShippingMethod(PaymentMethodDto.PaymentMethodRow paymentMethod, Guid shippingMethodId);
        List<PaymentMethodDto.PaymentMethodRow> GetAvailablePaymentMethods();
        List<PaymentMethodDto.PaymentMethodRow> GetAvailablePaymentMethods(CustomerContact customerContact);
        List<PaymentMethodDto.PaymentMethodRow> GetAvailableCapturePaymentMethods();
        PaymentMethodDto.PaymentMethodRow GetPaymentMethodByName(string paymentMethodName);
        PaymentMethodDto.PaymentMethodRow GetBullionPaymentMethodByCurrency(CustomerContact contact);
        PaymentMethodDto.PaymentMethodRow GetDefaultBullionPaymentMethod(IOrderGroup cart);
        PaymentMethodDto.PaymentMethodRow GetDefaultBullionPaymentMethod(IOrderGroup cart, CustomerContact customerContact);
        List<PaymentMethodDto.PaymentMethodRow> GetAvailableConsumerPaymentMethods();
    }
}
