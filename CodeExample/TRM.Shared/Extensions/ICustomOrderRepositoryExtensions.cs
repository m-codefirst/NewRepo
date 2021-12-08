using EPiServer.Commerce.Order;
using EPiServer.Framework;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using System;
using System.Linq;
using TRM.Shared.Constants;

namespace TRM.Shared.Extensions
{
    public static class ICustomOrderRepositoryExtensions
    {
        private static IMarketService _marketService = ServiceLocator.Current.GetInstance<IMarketService>();

        public static TCart LoadOrCreateCart<TCart>(this IOrderRepository orderRepository, Guid customerId, string orderTypeName, CustomerContact customerContact) where TCart : class, ICart
        {
            Validator.ThrowIfNull("customerId", customerId);
            MarketId currentMarketId = customerContact.GetCurrentMarket().MarketId;
            return orderRepository.Load<TCart>(customerId, orderTypeName).FirstOrDefault((TCart c) => c.MarketId == currentMarketId) ?? orderRepository.Create<TCart>(customerId, orderTypeName);
        }

        public static IMarket GetCurrentMarket(this CustomerContact customerContact)
        {
            return customerContact?.Properties[Shared.Constants.StringConstants.CustomFields.MarketIdFieldName].Value != null ?
                _marketService.GetMarket(customerContact.Properties[Shared.Constants.StringConstants.CustomFields.MarketIdFieldName].Value.ToString()) :
                _marketService.GetMarket(MarketId.Default);
        }

        public static string GetDefaultCurrencyCode(this CustomerContact currentCustomer)
        {
            if (currentCustomer == null) return StringConstants.DefaultValues.CurrencyCode;
            return string.IsNullOrEmpty(currentCustomer.PreferredCurrency) ? StringConstants.DefaultValues.CurrencyCode : currentCustomer.PreferredCurrency;
        }
    }
}
