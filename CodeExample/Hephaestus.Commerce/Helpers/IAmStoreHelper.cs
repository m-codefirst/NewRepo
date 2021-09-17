using EPiServer.Core;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog.Objects;
using System.Collections.Generic;

namespace Hephaestus.Commerce.Helpers
{
    public interface IAmStoreHelper
    {
        //Price GetSalePrice(EntryContentBase entry, Decimal quantity);
        //Price GetSalePrice(EntryContentBase entry, Decimal quantity, IMarket market);
        //Price GetSalePrice(EntryContentBase entry, Decimal quantity, IMarket market, Currency currency);
        //Price GetSalePrice(ContentReference contentReference, Decimal quantity);
        //Price GetSalePrice(ContentReference contentReference, Decimal quantity, IMarket market);
        //Price GetSalePrice(ContentReference contentReference, Decimal quantity, IMarket market, Currency currency);
        //IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IEnumerable<string> furtherPriceCodes);
        //IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IMarket market,
        //    IEnumerable<string> furtherPriceCodes);
        //IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IMarket market, Currency currency,
        //    IEnumerable<string> furtherPriceCodes);
        //IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity,
        //    IEnumerable<string> furtherPriceCodes);
        //IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity, IMarket market,
        //    IEnumerable<string> furtherPriceCodes);
        //IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity, IMarket market,
        //    Currency currency, IEnumerable<string> furtherPriceCodes);

        IList<Price> GetOriginalAndDiscountPrices(ContentReference contentReference, decimal quantity);
        Price GetDiscountPrice(ContentReference contentReference, decimal quantity);
        Price GetDiscountPrice(ContentReference contentReference, decimal quantity, IMarket market);
        Price GetDiscountPrice(ContentReference contentReference, decimal quantity, IMarket market, Currency currency);
        //Price GetDiscountPrice(EntryContentBase entry, Decimal quantity, IMarket market, Currency currency);

        string GetPriceAsString(decimal price, Currency currency);
        //string GetPriceAsString(ContentReference contentReference, Decimal quantity);
    }
}