using System.Collections.Generic;
using EPiServer.Commerce.Catalog.ContentTypes;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Pricing;

namespace Hephaestus.Commerce.Shared.Services
{
    public interface IPromotionService
    {
        IList<IPriceValue> GetDiscountPriceList(IEnumerable<CatalogKey> catalogKeys, MarketId marketId, Currency currency, bool returnOriginalPrice);
        IPriceValue GetDiscountPrice(CatalogKey catalogKey, MarketId marketId, Currency currency);
        IPriceValue GetDiscountPrice(IPriceValue price, EntryContentBase entry, Currency currency, IMarket market);
    }
}
