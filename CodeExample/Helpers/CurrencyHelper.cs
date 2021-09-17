using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Web.Extentions;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public class CurrencyHelper : IAmCurrencyHelper
    {
        private readonly CustomerContext _customerContext;
        public CurrencyHelper(CustomerContext customerContext)
        {
            _customerContext = customerContext;
        }
        public IEnumerable<Currency> GetAllCurrencies()
        {
            var store = DynamicDataStoreFactory.Instance.GetStore(typeof(Currency));

            var currencies = store.Items<Currency>();

            foreach (var currency in currencies)
            {
                yield return currency;
            }
        }

        public Dictionary<string, string> GetCurrencies()
        {
            var store = DynamicDataStoreFactory.Instance.GetStore(typeof(Currency));
            var currencyDic = new Dictionary<string, string> { { "Select", "Select" } };
            var currencies = store.Items<Currency>();

            foreach (var currency in currencies)
            {
                currencyDic.TryAdd(currency.Value, currency.DisplayName);
            }

            return currencyDic;
        }

        public IEnumerable<Currency> GetCurrenciesByCodes(string[] currencyCodes)
        {
            if (null == currencyCodes)
                return null;
            var store = DynamicDataStoreFactory.Instance.GetStore(typeof(Currency));
            var currencies = store.Items<Currency>().Where(x => currencyCodes.Contains(x.Value));
            return currencies;
        }

        public Currency GetCurrencyByCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }
            var store = DynamicDataStoreFactory.Instance.GetStore(typeof(Currency));
            return store.Items<Currency>().FirstOrDefault(x => x.Value == code.Trim());
        }

        public Currency GetDefaultCurrency()
        {
            return GetCurrencyByCode(GetDefaultCurrencyCode());
        }

        public string GetDefaultCurrencyCode()
        {
            return GetDefaultCurrencyCode(null);
        }
        public string GetDefaultCurrencyCode(CustomerContact customerContact)
        {
            var currentCustomer = customerContact ?? _customerContext.CurrentContact;
            if (currentCustomer == null || string.IsNullOrEmpty(currentCustomer.PreferredCurrency))
                return StringConstants.DefaultValues.CurrencyCode;

            return currentCustomer.PreferredCurrency;
        }

        public string GetDefaultCurrencySymbol()
        {
            var currencyCode = GetDefaultCurrencyCode();
            var currency = new Mediachase.Commerce.Currency(currencyCode);
            var mediachaseCurrencySymbol = currency.Format?.CurrencySymbol;
            return !string.IsNullOrEmpty(mediachaseCurrencySymbol)
                ? mediachaseCurrencySymbol
                : GetCurrencyByCode(currencyCode).Symbol ?? currencyCode.ToCurrencySymbol();
        }
    }
}