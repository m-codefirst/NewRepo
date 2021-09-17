using EPiServer.Core;
using Hephaestus.Commerce.Helpers;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog.Objects;
using System.Collections.Generic;

namespace TRM.Web.Business.Pricing
{
    public class TrmStoreHelper : IAmStoreHelper
    {
        private readonly IAmStoreHelper _storeHelper;
        private readonly ICurrentMarket _currentMarket;

        public TrmStoreHelper(
            IAmStoreHelper storeHelper,
            ICurrentMarket currentMarket)
        {
            _storeHelper = storeHelper;
            _currentMarket = currentMarket;
        }

        public virtual IList<Price> GetOriginalAndDiscountPrices(ContentReference contentReference, decimal quantity)
        {
            return _storeHelper.GetOriginalAndDiscountPrices(contentReference, quantity);
        }

        public virtual Price GetDiscountPrice(ContentReference contentReference, decimal quantity)
        {
            return GetDiscountPrice(contentReference, quantity, _currentMarket.GetCurrentMarket());
        }

        public virtual Price GetDiscountPrice(ContentReference contentReference, decimal quantity, IMarket market)
        {
            return GetDiscountPrice(contentReference, quantity, market, market.DefaultCurrency);
        }

        public virtual Price GetDiscountPrice(ContentReference contentReference, decimal quantity, IMarket market, Currency currency)
        {
            return _storeHelper.GetDiscountPrice(contentReference, quantity, market, currency);
        }

        public virtual string GetPriceAsString(decimal price, Currency currency)
        {
            return _storeHelper.GetPriceAsString(price, currency);
        }

        //private Money GetDiscountPrice(string variantCode, IMarket market, Currency currency, Money originalPrice)
        //{
        //    var discountedPrice = _promotionService.GetDiscountPrice(new CatalogKey(variantCode), market.MarketId, currency);
        //    if (discountedPrice != null)
        //    {
        //        return discountedPrice.UnitPrice;
        //    }

        //    return originalPrice;
        //}

        //public virtual Price GetSalePrice(EntryContentBase entry, decimal quantity)
        //{
        //    return _storeHelper.GetSalePrice(entry, quantity);
        //}

        //public virtual Price GetSalePrice(EntryContentBase entry, decimal quantity, IMarket market)
        //{
        //    return _storeHelper.GetSalePrice(entry, quantity, market);
        //}

        //public virtual Price GetSalePrice(EntryContentBase entry, decimal quantity, IMarket market, Currency currency)
        //{
        //    return _storeHelper.GetSalePrice(entry, quantity, market, currency);
        //}

        //public virtual Price GetSalePrice(ContentReference contentReference, decimal quantity)
        //{
        //    return _storeHelper.GetSalePrice(contentReference, quantity);
        //}

        //public virtual Price GetSalePrice(ContentReference contentReference, decimal quantity, IMarket market)
        //{
        //    return _storeHelper.GetSalePrice(contentReference, quantity, market);
        //}

        //public virtual Price GetSalePrice(ContentReference contentReference, decimal quantity, IMarket market, Currency currency)
        //{
        //    return _storeHelper.GetSalePrice(contentReference, quantity, market);
        //}

        //public virtual IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IEnumerable<string> furtherPriceCodes)
        //{
        //    return _storeHelper.GetSalePrices(entry, quantity, furtherPriceCodes);
        //}

        //public virtual IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IMarket market, IEnumerable<string> furtherPriceCodes)
        //{
        //    return _storeHelper.GetSalePrices(entry, quantity, market, furtherPriceCodes);
        //}

        //public virtual IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IMarket market, Currency currency,
        //    IEnumerable<string> furtherPriceCodes)
        //{
        //    return _storeHelper.GetSalePrices(entry, quantity, market, currency, furtherPriceCodes);
        //}

        //public virtual IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity, IEnumerable<string> furtherPriceCodes)
        //{
        //    return _storeHelper.GetSalePrices(contentReference, quantity, furtherPriceCodes);
        //}

        //public virtual IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity, IMarket market,
        //    IEnumerable<string> furtherPriceCodes)
        //{
        //    return _storeHelper.GetSalePrices(contentReference, quantity, market, furtherPriceCodes);
        //}

        //public virtual IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity, IMarket market, Currency currency,
        //    IEnumerable<string> furtherPriceCodes)
        //{
        //    return _storeHelper.GetSalePrices(contentReference, quantity, market, currency, furtherPriceCodes);
        //}
        //public virtual Price GetDiscountPrice(EntryContentBase entry, decimal quantity, IMarket market, Currency currency)
        //{
        //    return _storeHelper.GetDiscountPrice(entry, quantity, market, currency);
        //}
        //public virtual string GetPriceAsString(ContentReference contentReference, decimal quantity)
        //{
        //    return _storeHelper.GetPriceAsString(contentReference, quantity);
        //}
    }
}