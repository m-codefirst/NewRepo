using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Internal;
using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using TRM.Shared.Helpers;
using TRM.Web.Extentions;

namespace TRM.Web.Helpers
{
    public class MarketHelper : IAmMarketHelper
    {
        private readonly IMarketService _marketService;
        private readonly IOrderRepository _orderRepository;
        private readonly ICurrentMarket _currentMarket;
        private readonly CustomerContext _customerContext;
        private readonly IAmOrderGroupAuditHelper _orderGroupAuditHelper;

        public MarketHelper(IMarketService marketService, ICurrentMarket currentMarket, IOrderRepository orderRepository, CustomerContext customerContext, IAmOrderGroupAuditHelper orderGroupAuditHelper)
        {
            _marketService = marketService;
            _orderRepository = orderRepository;
            _currentMarket = currentMarket;
            _customerContext = customerContext;
            _orderGroupAuditHelper = orderGroupAuditHelper;
        }

        public IMarket GetMarketFromCountryCode(string countryCode)
        {
            var allMarkets = _marketService.GetAllMarkets().ToList();

            var market = allMarkets.Where(m => m.IsEnabled && m.Countries.Contains(countryCode))
                    .OrderBy(m => m.Countries.Count())
                    .FirstOrDefault();

            if (market != null) return market;

            market = _marketService.GetMarket(MarketId.Default);

            return market;
        }

        public Dictionary<ILineItem, List<ValidationIssue>> ValidateForSpecificCountry(IOrderGroup orderToValidate, string countryCode)
        {

            return ValidateForSpecificMarket(orderToValidate, GetMarketFromCountryCode(countryCode));
        }

        public Dictionary<ILineItem, List<ValidationIssue>> ValidateForSpecificMarket(IOrderGroup orderToValidate, IMarket market)
        {

            var results = new Dictionary<ILineItem, List<ValidationIssue>>();

            foreach (var shipment in orderToValidate.GetFirstForm().Shipments)
            {
                foreach (var lineItem in shipment.LineItems)
                {
                    var variant = lineItem.GetEntryContent();

                    if (variant.MarketFilter.Contains(market.MarketId.Value))
                    {
                        results.Add(lineItem, new List<ValidationIssue>() { ValidationIssue.RemovedDueToNotAvailableInMarket });
                    }
                }
            }

            return results;

        }

        private IMarket GetMarket(MarketId marketId)
        {
            return _marketService.GetMarket(marketId);
        }

        public IMarket GetMarket(string marketId)
        {
            return GetMarket(new MarketId(marketId));
        }

        public void UpdateCustomerAndCartMarket(string countryCode, IOrderGroup cart)
        {
            //Move the cart to the new market and set the Current Market for the customer based on the Delivery address
            var cartMarket = GetMarketFromCountryCode(countryCode);
            if (cartMarket.MarketId == cart.MarketId) return;

            var currentMarket = cart.MarketId;

            cart.MarketId = cartMarket.MarketId;

            _orderGroupAuditHelper.WriteAudit(cart, "Market changed", string.Format("Market changed from {0} to {1}", currentMarket.Value, cart.MarketId.Value));

            _orderRepository.Save(cart);

            _currentMarket.SetCurrentMarket(cartMarket.MarketId);
        }

        public string GetPriceReference()
        {
            var customerContact = _customerContext.CurrentContact;
            var marketId = _currentMarket.GetCurrentMarket().MarketId;
            return marketId.Value + "_" + customerContact?.CustomerGroup;
        }

        public IEnumerable<IMarket> GetAllAvailableMarkets()
        {
            return _marketService.GetAllMarkets();
        }

        public Dictionary<string, List<IMarket>> GetAllAvailableCurrenciesWithMarkets()
        {
            var allMarkets = GetAllAvailableMarkets();
            if (allMarkets.IsNullOrEmpty()) return new Dictionary<string, List<IMarket>>();

            var result = new Dictionary<string, List<IMarket>>();
            foreach (var market in allMarkets)
            {
                var allCurrencies = market.Currencies;
                if (allCurrencies.IsNullOrEmpty()) continue;

                foreach (var currency in allCurrencies)
                {
                    if (result.ContainsKey(currency.CurrencyCode))
                    {
                        var listMarket = result[currency.CurrencyCode] ?? new List<IMarket>();
                        listMarket.Add(market);
                    }
                    else
                    {
                        result.Add(currency.CurrencyCode, new List<IMarket>() { market });
                    }
                }
            }
            return result;
        }

        public IMarket GetCurrentMarket()
        {
            return _currentMarket.GetCurrentMarket();
        }
    }
}