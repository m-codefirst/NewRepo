using EPiServer.Core;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Pricing;
using System.Collections.Generic;
using PricingAndTradingService.Models.APIResponse;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Business.DataAccess
{
    public interface IAmLocalPriceDataHelper
    {
        PriceDetailValue SetEpiPriceValue(decimal price, Currency currency, MarketId marketId, CatalogKey catalogKey, string saleCode);
        IEnumerable<T> UpdateEpiPricesForBullionVariants<T>(IEnumerable<T> bullionVariants) where T : PreciousMetalsVariantBase;
        Dictionary<string, decimal> GetEpiPrices(ContentReference content);
        string GetPriceReference();
        void HandleInvalidIndicativePrices(GetPricesResponse metalPrices);
    }
}
