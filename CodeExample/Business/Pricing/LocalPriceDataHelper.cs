using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Internal;
using EPiServer;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Find.Commerce;
using EPiServer.Logging.Compatibility;
using EPiServer.Security;
using EPiServer.Web;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Pricing;
using PricingAndTradingService.Models.APIResponse;
using TRM.Shared.Models;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using Currency = Mediachase.Commerce.Currency;
using TRM.Web.Models.Pages;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.Pricing
{
    public class LocalPriceDataHelper : IAmLocalPriceDataHelper
    {
        private const int DefaultMinQuantity = 1;
        private const int DefaultValidDurationInDays = 7;

        /// <summary>
        /// As comment in BULL-1774, using the GBP only for sorting the price.
        /// </summary>
        public static readonly Currency DefaultCurrencyForIndexingPrice = Currency.GBP;

        private readonly IPriceDetailService _priceDetailService;
        private readonly IContentRepository _contentRepo;
        private readonly IAmMarketHelper _marketHelper;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly CustomerContext _customerContext;
        private readonly IAmContactHelper _contactHelper;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(LocalPriceDataHelper));
        private readonly IContentLoader _contentLoader;
        private readonly IEmailHelper _emailHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IBullionPremiumGroupHelper _bullionPremiumGroupHelper;

        public LocalPriceDataHelper(
            IPriceDetailService priceDetailService,
            IContentRepository contentRepo,
            IAmMarketHelper marketHelper,
            IBullionPriceHelper bullionPriceHelper,
            CustomerContext customerContext,
            IAmContactHelper contactHelper, 
            IContentLoader contentLoader, 
            IEmailHelper emailHelper, IAmBullionContactHelper bullionContactHelper, IBullionPremiumGroupHelper bullionPremiumGroupHelper)
        {
            _priceDetailService = priceDetailService;
            _contentRepo = contentRepo;
            _marketHelper = marketHelper;
            _bullionPriceHelper = bullionPriceHelper;
            _contentLoader = contentLoader;
            _emailHelper = emailHelper;
            _bullionContactHelper = bullionContactHelper;
            _bullionPremiumGroupHelper = bullionPremiumGroupHelper;
            _customerContext = customerContext;
            _contactHelper = contactHelper;
        }

        public PriceDetailValue SetEpiPriceValue(decimal price, Currency currency, MarketId marketId, CatalogKey catalogKey, string saleCode)
        {
            var result = new PriceDetailValue()
            {
                MarketId = marketId,
                CatalogKey = catalogKey,
                CustomerPricing = new CustomerPricing(CustomerPricing.PriceType.AllCustomers, saleCode),
                MinQuantity = DefaultMinQuantity,
                ValidFrom = DateTime.Now,
                ValidUntil = DateTime.Now.AddDays(DefaultValidDurationInDays),
                UnitPrice = new Money(price, currency)
            };
            return result;
        }

        public IEnumerable<T> UpdateEpiPricesForBullionVariants<T>(IEnumerable<T> bullionVariants) where T : PreciousMetalsVariantBase
        {
            var allCurrenciesWithMarkets = _marketHelper.GetAllAvailableCurrenciesWithMarkets();
            var allMaketsWithGBPCurrency = allCurrenciesWithMarkets.ContainsKey(DefaultCurrencyForIndexingPrice.CurrencyCode)
                ? allCurrenciesWithMarkets[DefaultCurrencyForIndexingPrice.CurrencyCode]
                : null;

            var variants = bullionVariants as T[] ?? bullionVariants.ToArray();
            if (variants.IsNullOrEmpty() || allMaketsWithGBPCurrency.IsNullOrEmpty()) return Enumerable.Empty<T>();

            var allPremiumGroups = _bullionPremiumGroupHelper.GetBullionPremiumGroup();
            var currencyPremiumCombinations = MakePriceInfoCombinations(allMaketsWithGBPCurrency, allPremiumGroups).ToList();
            var updatedBullionVariants = new List<T>();
            foreach (var bullionVariant in variants)
            {
                var bullionVariantContent = _contentRepo.Get<PreciousMetalsVariantBase>(bullionVariant.ContentLink);
                if (bullionVariantContent == null) continue;
                var isUpdated = UpdateEpiPricesForVariant(bullionVariantContent, currencyPremiumCombinations);
                if (isUpdated)
                {
                    updatedBullionVariants.Add(bullionVariant);
                }
            }
            return updatedBullionVariants;
        }

        private bool UpdateEpiPricesForVariant<T>(T bullionVariant, IEnumerable<EpiPriceBullionKeyInfoModel> currencyPremiumCombinations)
            where T : PreciousMetalsVariantBase
        {
            try
            {
                var priceInfoModels = new List<PriceInfoModel>();
                foreach (var combination in currencyPremiumCombinations)
                {
                    int valueAsInt;
                    var parseSuccess = int.TryParse(combination.PremiumGroupValue, out valueAsInt);
                    var fromPrice = _bullionPriceHelper.GetFromPriceForBullionVariant(
                        bullionVariant,
                        combination.Currency,
                        parseSuccess ? valueAsInt : 0);

                    priceInfoModels.Add(new PriceInfoModel(fromPrice, bullionVariant.Code, combination));
                }
                if (priceInfoModels.IsNullOrEmpty()) return false;

                var currentPrices = _priceDetailService.List(bullionVariant.ContentLink);
                _priceDetailService.Delete(currentPrices.Select(x => x.PriceValueId));

                var newPrices = new List<PriceDetailValue>();

                foreach (var priceInfo in priceInfoModels)
                {
                    var saleCode = priceInfo.PriceKeyInfoModel.PremiumGroup;
                    var thisVariant = new CatalogKey(priceInfo.VariantId);
                    var newPrice = SetEpiPriceValue(priceInfo.Price, priceInfo.PriceKeyInfoModel.Currency, priceInfo.PriceKeyInfoModel.MarketId, thisVariant, saleCode);
                    newPrices.Add(newPrice);
                }
                _priceDetailService.Save(newPrices);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        private IEnumerable<EpiPriceBullionKeyInfoModel> MakePriceInfoCombinations(Dictionary<string, List<IMarket>> allCurrenciesWithMarkets, List<BullionPremiumGroup> allPremiumGroups)
        {
            var allCurrencies = allCurrenciesWithMarkets.Keys;
            var result = new List<EpiPriceBullionKeyInfoModel>();
            foreach (var currency in allCurrencies)
            {
                var markets = allCurrenciesWithMarkets[currency];
                foreach (var market in markets)
                {
                    result.AddRange(allPremiumGroups.Select(pg => new EpiPriceBullionKeyInfoModel(currency, market.MarketId, pg.DisplayName, pg.Value.ToString())));
                    // add default combination with empty premiumGroup( incase annyoumous user)
                    result.Add(new EpiPriceBullionKeyInfoModel(currency, market.MarketId, string.Empty, string.Empty));
                }
            }
            return result;
        }

        private IEnumerable<EpiPriceBullionKeyInfoModel> MakePriceInfoCombinations(IList<IMarket> allMarketsUsingGBP, List<BullionPremiumGroup> allPremiumGroups)
        {
            var result = new List<EpiPriceBullionKeyInfoModel>();

            var currency = DefaultCurrencyForIndexingPrice;
            foreach (var market in allMarketsUsingGBP)
            {                
                result.AddRange(allPremiumGroups.Select(pg => new EpiPriceBullionKeyInfoModel(currency, market.MarketId, pg.DisplayName, pg.Value.ToString())));
                // add default combination with empty premiumGroup( incase annyoumous user)
                result.Add(new EpiPriceBullionKeyInfoModel(currency, market.MarketId, string.Empty, string.Empty));
            }           

            return result;
        }

        private string BuildTheSaleCode(Currency currency, string marketId, string customerGroup, string premiumGroup)
        {
            return $"{currency.CurrencyCode}_{marketId}_{customerGroup}_{premiumGroup ?? string.Empty}";
            //return $"{currency.CurrencyCode}_{customerGroup}_{premiumGroup ?? string.Empty}";
        }

        public Dictionary<string, decimal> GetEpiPrices(ContentReference content)
        {
            var prices = new Dictionary<string, decimal>();
            var variant = _contentRepo.Get<TrmVariationBase>(content);

            if (variant == null) return prices;
            var isBullionVariant = variant is IAmPremiumVariant;
            return isBullionVariant ? GetBullionEpiPricesForIndexing(variant.Prices()) : GetConsumerEpiPricesForIndexing(variant.Prices());
        }

        private Dictionary<string, decimal> GetConsumerEpiPricesForIndexing(IEnumerable<Price> variantPrices)
        {
            var prices = new Dictionary<string, decimal>();

            var pricesAsEnumerable = variantPrices as Price[] ?? variantPrices.ToArray();
            var customerGroupsToAddMorePricingOnMarket = GetCustomerGroupsToAddMorePricingOnMarket(pricesAsEnumerable);
            var allPremiumGroups = _bullionPremiumGroupHelper.GetBullionPremiumGroup().Select(pg => pg.DisplayName).ToList();
            
            allPremiumGroups.Add(string.Empty);
            foreach (var price in pricesAsEnumerable)
            {
                if (PriceExpired(price) || price.UnitPrice.Currency != DefaultCurrencyForIndexingPrice) continue;
                var amount = Math.Round(price.UnitPrice.Amount, 2);
                foreach (var premiumGroup in allPremiumGroups)
                {
                    prices.TryAddIfNotExist(BuildTheSaleCode(price.UnitPrice.Currency, price.MarketId.Value, price.CustomerPricing.PriceCode, premiumGroup), amount);
                    if (!customerGroupsToAddMorePricingOnMarket.ContainsKey(price.MarketId.ToString())) continue;
                    if (price.CustomerPricing.PriceTypeId != CustomerPricing.PriceType.AllCustomers
                        || !string.IsNullOrWhiteSpace(price.CustomerPricing.PriceCode))
                    {
                        continue;
                    }
                    foreach (var customerGroup in customerGroupsToAddMorePricingOnMarket[price.MarketId.ToString()])
                    {
                        prices.TryAddIfNotExist(BuildTheSaleCode(price.UnitPrice.Currency, price.MarketId.Value, customerGroup, premiumGroup), amount);
                    }
                }

            }

            return prices;
        }

        private bool PriceExpired(Price price)
        {
            if (price == null) return false;
            return price.ValidUntil.HasValue && price.ValidUntil.Value < DateTime.Now;
        }

        private Dictionary<string, IEnumerable<string>> GetCustomerGroupsToAddMorePricingOnMarket(IEnumerable<Price> variantPrices)
        {
            var prices = variantPrices as Price[] ?? variantPrices.ToArray();
            if (prices.IsNullOrEmpty()) return new Dictionary<string, IEnumerable<string>>();

            var result = new Dictionary<string, IEnumerable<string>>();
            var variantPricesByMarket = prices.GroupBy(x => x.MarketId);
            var allCustomerGroups = _contactHelper.GetAllContactGroups();

            foreach (var priceGroup in variantPricesByMarket)
            {
                var allCustomerPricingSetting = priceGroup.FirstOrDefault(x => x.CustomerPricing.PriceTypeId == CustomerPricing.PriceType.AllCustomers && string.IsNullOrWhiteSpace(x.CustomerPricing.PriceCode));
                if (allCustomerPricingSetting == null) continue;
                var customerGroupsHavingPricingSetting = priceGroup.Where(x => x.CustomerPricing.PriceTypeId == CustomerPricing.PriceType.PriceGroup).Select(x => x.CustomerPricing.PriceCode);

                result.Add(priceGroup.Key.ToString(), allCustomerGroups.Where(x => !string.IsNullOrEmpty(x) && !customerGroupsHavingPricingSetting.Contains(x)));
            }
            return result;
        }

        private Dictionary<string, decimal> GetBullionEpiPricesForIndexing(IEnumerable<Price> variantPrices)
        {
            var prices = new Dictionary<string, decimal>();
            //return prices;

            var allCustomerGroups = _contactHelper.GetAllContactGroups();
            allCustomerGroups.Add(string.Empty);

            foreach (var price in variantPrices)
            {
                foreach (var customerGroup in allCustomerGroups)
                {
                    var saleCode = BuildTheSaleCode(price.UnitPrice.Currency, price.MarketId.ToString(), customerGroup, price.CustomerPricing.PriceCode);
                    prices.TryAddIfNotExist(saleCode, Math.Round(price.UnitPrice.Amount, 2));
                }
            }

            return prices;
        }

        public string GetPriceReference()
        {
            var customerContact = _customerContext.CurrentContact;
            var marketId = _marketHelper.GetCurrentMarket().MarketId;
            var bullionPremiumGroup = _bullionContactHelper.GetBullionPremiumGroup(customerContact);           
            var currency = DefaultCurrencyForIndexingPrice;

            return BuildTheSaleCode(currency, marketId.ToString(), customerContact?.CustomerGroup, bullionPremiumGroup);
        }

        public void HandleInvalidIndicativePrices(GetPricesResponse metalPrices)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            Logger.Error(metalPrices.Result.ErrorMessage);

            if (!startPage.StopTrading)
            {
                var writableStartPage = startPage.CreateWritableClone() as StartPage;
                if (writableStartPage != null)
                {
                    writableStartPage.StopTrading = true;
                    _contentRepo.Save(writableStartPage, SaveAction.Publish, AccessLevel.NoAccess);
                }

                var warningEmailPage = _contentLoader.Get<TRMEmailPage>(startPage.InvalidIndicativePricesWarningEmail);
                if (warningEmailPage != null &&
                    !string.IsNullOrWhiteSpace(startPage.InvalidIndicativePricesWarningRecipients))
                {
                    _emailHelper.SendInvalidIndicativePricesEmail(warningEmailPage, startPage.InvalidIndicativePricesWarningRecipients, metalPrices.MetalPriceList);
                }
            }
        }
    }
}
