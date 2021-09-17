using EPiServer;
using EPiServer.Business.Commerce.Payment.Mastercard.Mastercard;
using EPiServer.Commerce.Order;
using EPiServer.Globalization;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Extensions;
using TRM.Web.Extentions;

namespace TRM.Web.Helpers
{
    public class PaymentMethodHelper : IAmAPaymentMethodHelper
    {
        private readonly ICurrentMarket _currentMarket;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public PaymentMethodHelper(ICurrentMarket currentMarket, IAmBullionContactHelper bullionContactHelper)
        {
            _currentMarket = currentMarket;
            _bullionContactHelper = bullionContactHelper;
        }

        public bool IsSupportedForShippingMethod(PaymentMethodDto.PaymentMethodRow paymentMethod,
            Guid shippingMethodId)
        {
            var shippingRestrictionRows = paymentMethod.GetShippingPaymentRestrictionRows();
            return shippingRestrictionRows.All(sr => sr.ShippingMethodId != shippingMethodId && sr.RestrictShippingMethods == false);
        }

        public List<PaymentMethodDto.PaymentMethodRow> GetAvailablePaymentMethods()
        {
            return GetAvailablePaymentMethods(null);
        }
        public List<PaymentMethodDto.PaymentMethodRow> GetAvailablePaymentMethods(CustomerContact customerContact)
        {
            var languageId = ContentLanguage.PreferredCulture.Name;
            var marketId = customerContact != null ? customerContact.GetCurrentMarket().MarketId.Value : _currentMarket.GetCurrentMarket().MarketId.Value;
            return PaymentManager.GetPaymentMethodsByMarket(marketId, languageId, false)
                                 .PaymentMethod
                                 .AsQueryable()
                                 .Where(p => p.IsActive).ToList();
        }

        public List<PaymentMethodDto.PaymentMethodRow> GetAvailableCapturePaymentMethods()
        {
            var languageId = ContentLanguage.PreferredCulture.Name;
            var marketId = _currentMarket.GetCurrentMarket().MarketId.Value;
            return PaymentManager.GetPaymentMethodsByMarket(marketId, languageId, false)
                .PaymentMethod
                .AsQueryable()
                .Where(p => p.IsActive &&
                            p.GetPaymentMethodParameterRows().FirstOrDefault(x => x.Parameter == MastercardPaymentGateway.CapturePayment) != null &&
                            p.GetPaymentMethodParameterRows().First(x => x.Parameter == MastercardPaymentGateway.CapturePayment).Value == "1").ToList();
        }

        public PaymentMethodDto.PaymentMethodRow GetPaymentMethodByName(string paymentMethodName)
        {
            var availableCapturePaymentMethods = GetAvailablePaymentMethods();
            return availableCapturePaymentMethods?.FirstOrDefault(c => c.Name == paymentMethodName);
        }

        public PaymentMethodDto.PaymentMethodRow GetBullionPaymentMethodByCurrency(CustomerContact contact)
        {
            var startPage = contact.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null) return null;

            var currency = _bullionContactHelper.GetDefaultCurrencyCode(contact);
            var paymentMethod = startPage.BullionCurrencyPayments.FirstOrDefault(x => x.CurrencyCode == currency)?.PaymentMethod;
            if (string.IsNullOrEmpty(paymentMethod)) return null;

            return PaymentManager.GetPaymentMethod(Guid.Parse(paymentMethod), false).PaymentMethod.AsQueryable().FirstOrDefault();
        }

        public PaymentMethodDto.PaymentMethodRow GetDefaultBullionPaymentMethod(IOrderGroup cart) 
        {
            return GetDefaultBullionPaymentMethod(cart, null);
        }
        public PaymentMethodDto.PaymentMethodRow GetDefaultBullionPaymentMethod(IOrderGroup cart, CustomerContact customerContact)
        {
            var startPage = cart.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null) return null;

            var shipment = cart.GetFirstShipment();
            var paymentMethods = GetAvailablePaymentMethods(customerContact).Where(p => IsSupportedForShippingMethod(p, shipment.ShippingMethodId)).ToList();

            return paymentMethods.FirstOrDefault(x => startPage.PaymentMethodList != null && startPage.PaymentMethodList.Contains(x.PaymentMethodId.ToString()));
        }

        public List<PaymentMethodDto.PaymentMethodRow> GetAvailableConsumerPaymentMethods()
        {
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage?.ConsumerCheckoutPaymentMethodsList == null) return new List<PaymentMethodDto.PaymentMethodRow>();

            var paymentMethods = GetAvailablePaymentMethods().Where(p => startPage.ConsumerCheckoutPaymentMethodsList.Contains(p.PaymentMethodId.ToString())).ToList();

            return paymentMethods;
        }
    }
}