using Mediachase.Commerce.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using EPiServer.Framework.Cache;
using EPiServer;

namespace TRM.Web.Business.Pricing
{
    /// <summary>
    /// Solution to allow Bullion variant buy premiums to be configured
    /// </summary>
    public class TrmPriceService : IPriceService
    {
        private readonly IPriceService _mediachasePricingService;
        private readonly ICurrentMarket _currentMarket;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;

        public TrmPriceService(
            IPriceService mediachasePricingService,
            ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache,
            ICurrentMarket currentMarket,
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader)
        {
            _mediachasePricingService = mediachasePricingService;
            _currentMarket = currentMarket;
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
        }

        #region IPriceService

        public virtual bool IsReadOnly { get; set; }

        public virtual IEnumerable<IPriceValue> GetCatalogEntryPrices(CatalogKey catalogKey)
        {
            return _mediachasePricingService.GetCatalogEntryPrices(catalogKey);
        }

        public virtual IEnumerable<IPriceValue> GetCatalogEntryPrices(IEnumerable<CatalogKey> catalogKeys)
        {
            return _mediachasePricingService.GetCatalogEntryPrices(catalogKeys);
        }

        public virtual IPriceValue GetDefaultPrice(MarketId market, DateTime validOn, CatalogKey catalogKey, Currency currency)
        {
            return _mediachasePricingService.GetDefaultPrice(market, validOn, catalogKey, currency);
        }

        public virtual IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, CatalogKey catalogKey, PriceFilter filter)
        {
            return _mediachasePricingService.GetPrices(market, validOn, catalogKey, filter);
        }

        public virtual IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, IEnumerable<CatalogKey> catalogKeys, PriceFilter filter)
        {
            return _mediachasePricingService.GetPrices(market, validOn, catalogKeys, filter);
        }

        public virtual IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, IEnumerable<CatalogKeyAndQuantity> catalogKeysAndQuantities, PriceFilter filter)
        {
            return _mediachasePricingService.GetPrices(market, validOn, catalogKeysAndQuantities, filter);
        }

        public virtual void ReplicatePriceDetailChanges(IEnumerable<CatalogKey> catalogKeys, IEnumerable<IPriceValue> priceValues)
        {
            _mediachasePricingService.ReplicatePriceDetailChanges(catalogKeys, priceValues);
        }

        public virtual void SetCatalogEntryPrices(CatalogKey catalogKey, IEnumerable<IPriceValue> priceValues)
        {
            _mediachasePricingService.SetCatalogEntryPrices(catalogKey, priceValues);
        }

        public virtual void SetCatalogEntryPrices(IEnumerable<CatalogKey> catalogKeys, IEnumerable<IPriceValue> priceValues)
        {
            _mediachasePricingService.SetCatalogEntryPrices(catalogKeys, priceValues);
        }

        #endregion
    }
}