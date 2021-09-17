using System.Collections.Generic;
using Mediachase.Commerce;

namespace Hephaestus.Commerce.Shared.Services
{
    public interface ICurrencyService
    {
        IEnumerable<Currency> GetAvailableCurrencies();
        Currency GetCurrentCurrency();
        Currency GetCurrentCurrency(string currencyCurency);
        bool SetCurrentCurrency(string currencyCode);
    }
}
