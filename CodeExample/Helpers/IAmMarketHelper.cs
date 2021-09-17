using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Mediachase.Commerce;

namespace TRM.Web.Helpers
{
    public interface IAmMarketHelper
    {
        IMarket GetMarketFromCountryCode(string countryCode);
        Dictionary<ILineItem, List<ValidationIssue>> ValidateForSpecificCountry(IOrderGroup orderToValidate, string countryCode);
        Dictionary<ILineItem, List<ValidationIssue>> ValidateForSpecificMarket(IOrderGroup orderToValidate, IMarket market);
        IMarket GetMarket(string marketId);
        void UpdateCustomerAndCartMarket(string countryCode, IOrderGroup cart);
        string GetPriceReference();

        IEnumerable<IMarket> GetAllAvailableMarkets();
        Dictionary<string, List<IMarket>> GetAllAvailableCurrenciesWithMarkets();

        IMarket GetCurrentMarket();
    }
}