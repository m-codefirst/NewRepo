using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.Shared.Services;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Objects;
using Mediachase.Commerce.Pricing;
using System.Collections.Generic;
//using InternalStoreHelper = Mediachase.Commerce.Website.Helpers.StoreHelper;

namespace Hephaestus.Commerce.Helpers
{
    [ServiceConfiguration(typeof(IAmStoreHelper), Lifecycle = ServiceInstanceScope.Singleton)]
    public class StoreHelper : IAmStoreHelper
    {
        protected readonly ICurrentMarket _currentMarket;
        protected readonly IPricingService _pricingService;
        protected readonly IContentLoader _contentLoader;
        private readonly IPromotionService _promotionService;

        public StoreHelper(
            ICurrentMarket currentMarket,
            IPricingService priceService,
            IContentLoader contentLoader,
            IPromotionService promotionService)
        {
            _currentMarket = currentMarket;
            _pricingService = priceService;
            _contentLoader = contentLoader;
            _promotionService = promotionService;
        }

        public IList<Price> GetOriginalAndDiscountPrices(ContentReference contentReference, decimal quantity)
        {
            IMarket market = _currentMarket.GetCurrentMarket();
            Currency currency = market.DefaultCurrency;
            List<Price> prices = new List<Price>();

            if (!_contentLoader.TryGet(contentReference, out EntryContentBase entryBase)) 
            { 
                return prices; 
            }

            var currentPrice = _pricingService.GetPrice(entryBase.Code, market.MarketId, currency);

            if (currentPrice.HasValue)
            {
                var priceList = _promotionService.GetDiscountPriceList(new[] { new CatalogKey(entryBase.Code) }, market.MarketId, currency, returnOriginalPrice: true);

                if (priceList?.Count == 2)
                {
                    foreach (IPriceValue priceListItem in priceList)
                    {
                        prices.Add(new Price(priceListItem.UnitPrice));
                    }
                }
                else
                {
                    // If no discount price is found, add current price to both original and discount price slots in price list
                    Price originalAndDiscountPrice = new Price(currentPrice.Value);
                    prices.Add(originalAndDiscountPrice); // original price
                    prices.Add(originalAndDiscountPrice); // discount price
                }
            }

            return prices;
        }

        public Price GetDiscountPrice(ContentReference contentReference, decimal quantity)
        {
            return GetDiscountPrice(contentReference, quantity, _currentMarket.GetCurrentMarket());
        }

        public Price GetDiscountPrice(ContentReference contentReference, decimal quantity, IMarket market)
        {
            return GetDiscountPrice(contentReference, quantity, market, market.DefaultCurrency);
        }

        public Price GetDiscountPrice(ContentReference contentReference, decimal quantity, IMarket market, Currency currency)
        {
            EntryContentBase entryBase;
            if (!_contentLoader.TryGet<EntryContentBase>(contentReference, out entryBase)) return new Price();

            var currentPrice = _pricingService.GetPrice(entryBase.Code, market.MarketId, currency);
            var discountedPrice = currentPrice.HasValue ? GetDiscountPrice(entryBase.Code, market, currency, currentPrice.Value) : new Money(0, currency);
            return new Price(discountedPrice);
        }

        public string GetPriceAsString(decimal price, Currency currency)
        {
            var priceAsString = MoneyFromAmount(price, currency).ToString(); //CommonHelper.GetMoneyString(price, currency);
            return priceAsString;
        }
        private Money MoneyFromAmount(decimal amount, string currency)
        {
            Currency currency2 = new Currency(currency);
            return new Money(amount, currency2);
        }

        private Money GetDiscountPrice(string variantCode, IMarket market, Currency currency, Money originalPrice)
        {
            var discountedPrice = _promotionService.GetDiscountPrice(new CatalogKey(variantCode), market.MarketId, currency);
            if (discountedPrice != null)
            {
                return discountedPrice.UnitPrice;
            }

            return originalPrice;
        }

        //public Price GetSalePrice(ContentReference contentReference, decimal quantity, IMarket market, Currency currency)
        //{
        //    var entryId = RefConverter.GetObjectId(contentReference);
        //    //return GetSalePrice(CatalogContext.Current.GetCatalogEntry(entryId), quantity, market, currency);
        //    return GetSalePrice(contentReference, quantity, market, currency);
        //}

        //public Price GetSalePrice(EntryContentBase entry, decimal quantity)
        //{
        //    return GetSalePrice(entry, quantity, CurrentMarket.GetCurrentMarket());
        //}

        //public Price GetSalePrice(EntryContentBase entry, decimal quantity, IMarket market)
        //{
        //    return GetSalePrice(entry, quantity, market, market.DefaultCurrency);
        //}

        //public Price GetDiscountPrice(EntryContentBase entry, Decimal quantity, IMarket market, Currency currency)
        //{
        //    var contentType = ServiceLocator.Current.GetInstance<IContentTypeRepository>().Load(entry.Code);
        //    //return InternalStoreHelper.GetDiscountPrice(entry);
        //    return new Price();
        //}

        //private void TestGetDiscountPrice(EntryContentBase entry)
        //{
        //    var contentType = ServiceLocator.Current.GetInstance<IContentTypeRepository>().Load(entry.Code);
        //    //if (entry == null)
        //    //{
        //    //    throw new NullReferenceException("entry can't be null");
        //    //}
        //    //decimal num = 1m;
        //    //if (entry.ItemAttributes != null)
        //    //{
        //    //    num = entry.ItemAttributes.MinQuantity;
        //    //}
        //    //if (num <= 0m)
        //    //{
        //    //    num = 1m;
        //    //}
        //}

        //public string GetPriceAsString(ContentReference contentReference, decimal quantity)
        //{
        //    var price = GetSalePrice(contentReference, quantity);

        //    if (price == null)
        //    {
        //        return string.Empty;
        //    }

        //    var priceAsString = MoneyFromAmount(price.Money.Amount, price.Money.Currency).ToString();// CommonHelper.GetMoneyString(price.Money.Amount, price.Money.Currency);
        //    return priceAsString;
        //}

        //public Price GetSalePrice(EntryContentBase entry, Decimal quantity, IMarket market, Currency currency)
        //{
        //    var catalogKey = new CatalogKey(entry.Code);
        //    var filter = CreatePriceFilter(quantity, currency);
        //    var price =
        //        PriceService.GetPrices(market.MarketId, FrameworkContext.Current.CurrentDateTime, catalogKey, filter)
        //            .OrderBy(p => p.UnitPrice)
        //            .FirstOrDefault();
        //    return price == null ? null : new Price(price.UnitPrice);
        //}

        //public Price GetSalePrice(ContentReference contentReference, decimal quantity)
        //{
        //    return GetSalePrice(contentReference, quantity, CurrentMarket.GetCurrentMarket());
        //}

        //public Price GetSalePrice(ContentReference contentReference, decimal quantity, IMarket market)
        //{
        //    return GetSalePrice(contentReference, quantity, market, market.DefaultCurrency);
        //}

        //public IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity,
        //    IEnumerable<string> furtherPriceCodes)
        //{
        //    return GetSalePrices(entry, quantity, CurrentMarket.GetCurrentMarket(), furtherPriceCodes);
        //}

        //public IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IMarket market, IEnumerable<string> furtherPriceCodes)
        //{
        //    return GetSalePrices(entry, quantity, market, market.DefaultCurrency, furtherPriceCodes);
        //}

        //public IEnumerable<IPriceValue> GetSalePrices(EntryContentBase entry, decimal quantity, IMarket market, Currency currency, IEnumerable<string> furtherPriceCodes)
        //{
        //    var catalogKey = new CatalogKey(entry.Code);
        //    var filter = CreatePriceFilter(quantity, currency,
        //        new List<CustomerPricing>(
        //            furtherPriceCodes.Select(
        //                priceCode => new CustomerPricing(CustomerPricing.PriceType.AllCustomers, priceCode))), true);
        //    return
        //        PriceService.GetPrices(market.MarketId, FrameworkContext.Current.CurrentDateTime, catalogKey, filter)
        //            .OrderBy(p => p.UnitPrice);
        //}

        //public IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity,
        //    IEnumerable<string> furtherPriceCodes)
        //{
        //    return GetSalePrices(contentReference, quantity, CurrentMarket.GetCurrentMarket(), furtherPriceCodes);
        //}

        //public IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity,
        //    IMarket market,
        //    IEnumerable<string> furtherPriceCodes)
        //{
        //    return GetSalePrices(contentReference, quantity, market, market.DefaultCurrency, furtherPriceCodes);
        //}

        //public IEnumerable<IPriceValue> GetSalePrices(ContentReference contentReference, decimal quantity,
        //    IMarket market, Currency currency,
        //    IEnumerable<string> furtherPriceCodes)
        //{
        //    var entryId = RefConverter.GetObjectId(contentReference);
        //    //return GetSalePrices(CatalogContext.Current.GetCatalogEntry(entryId), quantity, market, currency, furtherPriceCodes);
        //    return GetSalePrices(contentReference, quantity, market, currency, furtherPriceCodes);
        //}

        //private static PriceFilter CreatePriceFilter(decimal quantity, Currency currency)
        //{
        //    return CreatePriceFilter(quantity, currency, new List<CustomerPricing>(), false);
        //}

        //private static PriceFilter CreatePriceFilter(decimal quantity, Currency currency,
        //    ICollection<CustomerPricing> customerPricing, bool returnCustomerPricing)
        //{
        //    customerPricing.Add(CustomerPricing.AllCustomers);
        //    var currentPrincipal = EPiServer.Security.PrincipalInfo.CurrentPrincipal;

        //    // Updated conditions to ensure principal is found and is populated
        //    if (currentPrincipal != null && currentPrincipal.Identity != null && !string.IsNullOrEmpty(currentPrincipal.Identity.Name))
        //    {
        //        // Moved to inside the if as this was causing exception
        //        var currentUser = Membership.GetUser(currentPrincipal.Identity.Name);

        //        if (!string.IsNullOrEmpty(currentUser.UserName))
        //        {
        //            customerPricing.Add(new CustomerPricing(CustomerPricing.PriceType.UserName, currentUser.UserName));
        //        }
        //        var contactForUser = CustomerContext.Current.GetContactForUser(currentUser);
        //        if (contactForUser != null && !string.IsNullOrEmpty(contactForUser.EffectiveCustomerGroup))
        //        {
        //            customerPricing.Add(new CustomerPricing(CustomerPricing.PriceType.PriceGroup,
        //                contactForUser.EffectiveCustomerGroup));
        //        }
        //    }
        //    var filter = new PriceFilter
        //    {
        //        Quantity = quantity,
        //        Currencies = new[] { currency },
        //        CustomerPricing = customerPricing,
        //        ReturnCustomerPricing = returnCustomerPricing
        //    };
        //    return filter;
        //}
    }
}