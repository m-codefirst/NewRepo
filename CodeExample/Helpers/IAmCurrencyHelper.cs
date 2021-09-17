using Mediachase.Commerce.Customers;
using System.Collections.Generic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public interface IAmCurrencyHelper
    {
        Dictionary<string, string> GetCurrencies();
        Currency GetCurrencyByCode(string code);
        IEnumerable<Currency> GetAllCurrencies();
        IEnumerable<Currency> GetCurrenciesByCodes(string[] currencyCodes);
        string GetDefaultCurrencyCode();
        string GetDefaultCurrencyCode(CustomerContact customerContact);
        Currency GetDefaultCurrency();
        string GetDefaultCurrencySymbol();
    }
}