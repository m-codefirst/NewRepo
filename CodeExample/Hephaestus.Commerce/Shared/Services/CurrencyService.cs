using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;

namespace Hephaestus.Commerce.Shared.Services
{
    [ServiceConfiguration(typeof(ICurrencyService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CurrencyService : ICurrencyService
    {
        private const string CurrencyCookie = "Currency";
        private readonly ICurrentMarket _currentMarket;
        
        public CurrencyService(ICurrentMarket currentMarket)
        {
            _currentMarket = currentMarket;
            
        }

        public IEnumerable<Currency> GetAvailableCurrencies()
        {
            return CurrentMarket.Currencies;
        }

        public virtual Currency GetCurrentCurrency()
        {
            var currencyCookie = HttpContext.Current.Request.Cookies[CurrencyCookie] == null ? null
                : HttpContext.Current.Request.Cookies[CurrencyCookie].Value;

            return GetCurrentCurrency(currencyCookie);
        
        }
        public virtual Currency GetCurrentCurrency(string currencyCurency)
        {
            Currency currency;
            return TryGetCurrency(currencyCurency, out currency) ? currency 
                : CurrentMarket.DefaultCurrency;
        }

        public bool SetCurrentCurrency(string currencyCode)
        {
            Currency currency;

            if (!TryGetCurrency(currencyCode, out currency))
            {
                return false;
            }

            if (HttpContext.Current != null)
            {
                var httpCookie = new HttpCookie(CurrencyCookie)
                {
                    Value = currencyCode,
                    Expires = DateTime.Now.AddYears(1)
                };

                HttpContext.Current.Response.Cookies.Add(httpCookie);
                HttpContext.Current.Request.Cookies.Set(httpCookie);
            }

            return true;
        }

        private bool TryGetCurrency(string currencyCode, out Currency currency)
        {
            var result = GetAvailableCurrencies()
                .Where(x => x.CurrencyCode == currencyCode)
                .Cast<Currency?>()
                .FirstOrDefault();

            if (result.HasValue)
            {
                currency = result.Value;
                return true;
            }

            currency = null;
            return false;
        }

        private IMarket CurrentMarket
        {
            get { return _currentMarket.GetCurrentMarket(); }
        }
    }
}
