using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using PricingAndTradingService.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using TRM.Shared.Extensions;
using TRM.Web.Business.DataAccess;
using TRM.Web.Extentions;
using TRM.Web.Extentions.MetalPrice;
using TRM.Web.Helpers;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.DDS;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.MetalPrice;

namespace TRM.Web.Services
{
    public interface IMetalPriceService
    {
        TrmMetalBlockPartialViewModel BuildPampMetalPriceModel(TrmMetalBlock currentBlock);

        TrmMetalBlockPartialViewModel BuildPampMetalPriceModel();

        List<PampMetalPriceSync> GetIndicativePriceByDate(DateTime date);

        IEnumerable<PampMetalPriceItemViewModel> GetPampMetalPrices();
    }

    [ServiceConfiguration(typeof(IMetalPriceService), Lifecycle = ServiceInstanceScope.Transient)]
    public class MetalPriceService : IMetalPriceService
    {
        protected readonly ILogger Logger = LogManager.GetLogger(typeof(MetalPriceService));

        protected readonly ISynchronizedObjectInstanceCache _epiCache;

        private readonly PampMetalPriceSyncRepository _pampMetalPriceSyncRepository;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly CustomerContext _customerContext;
        private readonly HttpContextBase _httpContext;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IContentLoader _contentLoader;

        public MetalPriceService(
            HttpContextBase httpContext,
            ISynchronizedObjectInstanceCache epiCache,
            IAmCurrencyHelper currencyHelper,
            PampMetalPriceSyncRepository pampMetalPriceSyncRepository,
            CustomerContext customerContext, 
            IAmBullionContactHelper bullionContactHelper,
            IContentLoader contentLoader)
        {
            _httpContext = httpContext;
            _epiCache = epiCache;
            _currencyHelper = currencyHelper;
            _pampMetalPriceSyncRepository = pampMetalPriceSyncRepository;
            _customerContext = customerContext;
            _bullionContactHelper = bullionContactHelper;
            _contentLoader = contentLoader;
        }

        public TrmMetalBlockPartialViewModel BuildPampMetalPriceModel(TrmMetalBlock currentBlock)
        {
            try
            {
                var startPage = currentBlock.GetAppropriateStartPageForSiteSpecificProperties();

                decimal? cookieExpiredTime;
                ContentReference infoLink;
                ContentReference balanceLink;
                string balanceCopy;
                ContentReference anonymousInvestingLink;
                string anonymousInvestingCopy;
                ContentReference consumerInvestingLink;
                string consumerInvestingCopy;
                bool? hideBullionBalance;
                ContentReference bullionBalanceHiddenInvestingLink;
                string bullionBalanceHiddenInvestingCopy;

                if (startPage?.MetalPriceSettingsPage != null)
                {
                    var metalPriceSettingsPage = _contentLoader.Get<MetalPriceSettingsPage>(startPage.MetalPriceSettingsPage);
                    cookieExpiredTime = metalPriceSettingsPage?.CookieExpiredTime;
                    infoLink = metalPriceSettingsPage?.InfoLink;
                    balanceLink = metalPriceSettingsPage?.BalanceLink;
                    balanceCopy = metalPriceSettingsPage?.BalanceCopy;
                    anonymousInvestingLink = metalPriceSettingsPage?.AnonymousInvestingLink;
                    anonymousInvestingCopy = metalPriceSettingsPage?.AnonymousInvestingCopy;
                    consumerInvestingLink = metalPriceSettingsPage?.ConsumerInvestingLink;
                    consumerInvestingCopy = metalPriceSettingsPage?.ConsumerInvestingCopy;
                    hideBullionBalance = metalPriceSettingsPage?.HideBullionBalance;
                    bullionBalanceHiddenInvestingLink = metalPriceSettingsPage?.BullionBalanceHiddenInvestingLink;
                    bullionBalanceHiddenInvestingCopy = metalPriceSettingsPage?.BullionBalanceHiddenInvestingCopy;
                }    
                else
                {
                    cookieExpiredTime = startPage?.CookieExpiredTime;
                    infoLink = startPage?.InfoLink;
                    balanceLink = startPage?.BalanceLink;
                    balanceCopy = startPage?.BalanceCopy;
                    anonymousInvestingLink = startPage?.AnonymousInvestingLink;
                    anonymousInvestingCopy = startPage?.AnonymousInvestingCopy;
                    consumerInvestingLink = startPage?.ConsumerInvestingLink;
                    consumerInvestingCopy = startPage?.ConsumerInvestingCopy;
                    hideBullionBalance = startPage?.HideBullionBalance;
                    bullionBalanceHiddenInvestingLink = startPage?.BullionBalanceHiddenInvestingLink;
                    bullionBalanceHiddenInvestingCopy = startPage?.BullionBalanceHiddenInvestingCopy;
                }

                return new TrmMetalBlockPartialViewModel
                {
                    ShowPriceAlertIcon = ShowAlertIcon(),
                    AlertPageUrl = _customerContext.GetPriceAlertPageUrl(),
                    MetalChartPageUrl = _customerContext.GetPriceChartPageUrl(),
                    ShouldDisplayHeaderView = currentBlock.ShouldDisplayTableHeader,
                    CustomCssClass = currentBlock.CustomWidthClass,
                    DisplayOption = currentBlock.DisplayOption.ToString(),
                    PampMetalPriceItems = GetPampMetalPrices(),
                    UpdatedTime = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss \"GMT\""),
                    IsSameColumnWidth = currentBlock.IsSameColumnWidth,
                    DisplayMetals = ConvertEnumValueToEnumString(currentBlock.DisplayMetal),
                    BalanceCopy = balanceCopy,
                    BalanceUrl = balanceLink?.GetExternalUrl_V2(),
                    AnonymousInvestingCopy = anonymousInvestingCopy,
                    AnonymousInvestingUrl = anonymousInvestingLink?.GetExternalUrl_V2(),
                    ConsumerInvestingCopy = consumerInvestingCopy,
                    ConsumerInvestingUrl = consumerInvestingLink?.GetExternalUrl_V2(),
                    HideBullionBalance = hideBullionBalance ?? false,
                    BullionBalanceHiddenInvestingCopy = bullionBalanceHiddenInvestingCopy,
                    BullionBalanceHiddenInvestingLink = bullionBalanceHiddenInvestingLink?.GetExternalUrl_V2(),
                    InfoUrl = infoLink?.GetExternalUrl_V2(),
                    CookieExpiredTime = cookieExpiredTime ?? 1,
                    AvailableToInvest = GetAvailableToInvestInternal()
                };
            }
            catch (Exception ex)
            {
                Logger.Error("BuildPampMetalPriceModel", ex);
                return null;
            }
        }

        private Money GetAvailableToInvestInternal()
        {
            // Try to get availableToInvest from authenticated user
            if (!_httpContext.Request.IsAuthenticated) return new Money(0, _currencyHelper.GetDefaultCurrencyCode());

            var currentContact = _customerContext.CurrentContact;
            if (currentContact != null)
            {
                var availableToInvest = _bullionContactHelper.GetMoneyAvailableToInvest(currentContact);
                return availableToInvest;
            }
            // Can't get available to invest from authenticated user
            Logger.Error($"Error GetMoneyAvailableToInvest by authenticatedUser. CurrentContact IS NULL {_customerContext.CurrentContact == null}");

            return new Money(0, _currencyHelper.GetDefaultCurrencyCode());
        }

        public TrmMetalBlockPartialViewModel BuildPampMetalPriceModel()
        {
            return new TrmMetalBlockPartialViewModel
            {
                PampMetalPriceItems = GetPampMetalPrices(),
                UpdatedTime = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss \"GMT\""),
            };
        }

        public List<PampMetalPriceSync> GetIndicativePriceByDate(DateTime date)
        {
            //return _pampMetalPriceSyncRepository.GetList()
            //    .Where(x => x.CustomerBuy && DbFunctions.TruncateTime(x.CreatedDate) == date.Date)
            //    .GroupBy(x => x.Currency)
            //    .Select(x => x.FirstOrDefault())
            //    .ToList();

            Logger.Error($"Entered - GetIndicativePriceByDate: Date: {date.Date}");
            // not sure how effective this would be as we are still searching by currency which isn't indexed?
            var usd = _pampMetalPriceSyncRepository.GetList()
                .FirstOrDefault(x => x.CustomerBuy &&
                DbFunctions.TruncateTime(x.CreatedDate) == date.Date &&
                x.Currency == "USD");

            var eur = _pampMetalPriceSyncRepository.GetList()
                .FirstOrDefault(x => x.CustomerBuy &&
                DbFunctions.TruncateTime(x.CreatedDate) == date.Date &&
                x.Currency == "EUR");

            var gbp = _pampMetalPriceSyncRepository.GetList()
                .FirstOrDefault(x => x.CustomerBuy &&
                DbFunctions.TruncateTime(x.CreatedDate) == date.Date &&
                x.Currency == "GBP");

            Logger.Error($"Exist - GetIndicativePriceByDate: Date: {date.Date}");
            if (usd == null || eur == null || gbp == null) return null;

            return new List<PampMetalPriceSync>(new[] { usd, eur, gbp });
        }

        public IEnumerable<PampMetalPriceItemViewModel> GetPampMetalPrices()
        {
            try
            {
                var currency = _currencyHelper.GetDefaultCurrencyCode();

                return _epiCache.ReadThrough($"MetalLivePrice_{currency}", () =>
                {
                    var priceList = _pampMetalPriceSyncRepository.GetList();

                    if (priceList == null || !priceList.Any()) return new List<PampMetalPriceItemViewModel>();

                    // Getting topTwoPam price
                    var prices = priceList.Where(x => x.Currency == currency)
                        .OrderByDescending(x => x.CreatedDate)
                        .Take(4) // One raw json generates 6 records in pamMetalSync table, 2 records for each currencies
                        .AsNoTracking()
                        .ToList();

                    var previousPrices =
                        _pampMetalPriceSyncRepository.ConvertMetalPriceList(prices.Skip(2).Take(2).ToList());
                    var latestPrices = _pampMetalPriceSyncRepository.ConvertMetalPriceList(prices.Take(2).ToList());

                    return latestPrices.Select(x => ConvertToMetalPriceViewModel(previousPrices, x, currency));

                }, () => new CacheEvictionPolicy(TimeSpan.FromSeconds(GetCacheTime()), CacheTimeoutType.Absolute));
            }
            catch (Exception err)
            {
                Logger.Error(err.Message, err);
                return new List<PampMetalPriceItemViewModel>();
            }
        }

        private int GetCacheTime()
        {
            var startPage = _customerContext.GetAppropriateStartPageForSiteSpecificProperties();

            var cacheTime = (startPage?.MetalPriceSettingsPage != null) ?
                            _contentLoader.Get<MetalPriceSettingsPage>(startPage.MetalPriceSettingsPage).CacheTime :
                            startPage?.CacheTime;

            if (cacheTime == null || cacheTime <= 0) return 60;

            return cacheTime.Value;
        }

        private IEnumerable<string> ConvertEnumValueToEnumString(string displayMetal)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(displayMetal)) return result;

            var displayMetals = displayMetal.SplitValueTo<string>();
            foreach (var metal in displayMetals)
            {
                PricingAndTradingService.Models.Constants.MetalType metalName;
                if (Enum.TryParse(metal, true, out metalName))
                {
                    if (Enum.IsDefined(typeof(PricingAndTradingService.Models.Constants.MetalType), metalName))
                    {
                        result.Add(metalName.ToString());
                    }
                }
            }

            return result;
        }

        private PampMetalPriceItemViewModel ConvertToMetalPriceViewModel(IEnumerable<MetalPrice> previousMetalPrices, MetalPrice metalPrice, string currency)
        {
            var matchPreviousMetal = previousMetalPrices?.FirstOrDefault(x =>
                x.GetPampMetal().Name.Equals(metalPrice.GetPampMetal().Name));

            return new PampMetalPriceItemViewModel
            {
                Metal = metalPrice.GetPampMetal(),
                BuyPriceChange = GetPampMetalPriceItemChangeModel(metalPrice.SellPrice, matchPreviousMetal?.SellPrice ?? 0, currency),
                SellPriceChange = GetPampMetalPriceItemChangeModel(metalPrice.BuyPrice, matchPreviousMetal?.BuyPrice ?? 0, currency)
            };
        }

        private PampMetalPriceItemChangeModel GetPampMetalPriceItemChangeModel(decimal currentPrice, decimal previousPrice, string currency)
        {
            return new PampMetalPriceItemChangeModel
            {
                UpOrDown = GetUpOrDown(currentPrice, previousPrice),
                Amount = GetDifferenceAmount(currentPrice, previousPrice),
                Percent = GetDifferencePercent(currentPrice, previousPrice),
                CurrentPrice = new Money(currentPrice, currency),
                CurrentPricePerKg = new Money(ConvertPricePerOzToPricePerKg(currentPrice), currency)
            };
        }

        private decimal ConvertPricePerOzToPricePerKg(decimal pricePerOz)
        {
            decimal troyOunceToKgDividend = (decimal)32.151;
            return pricePerOz / troyOunceToKgDividend;
        }

        private int GetUpOrDown(decimal currentPrice, decimal previousPrice)
        {
            // UP: 1// KEEP: 0// DOWN: -1
            if (previousPrice == 0) return 0;
            if (currentPrice > previousPrice) return 1;
            if (currentPrice < previousPrice) return -1;
            return 0;
        }

        private decimal GetDifferenceAmount(decimal currentPrice, decimal previousPrice)
        {
            return currentPrice - previousPrice;
        }

        private decimal GetDifferencePercent(decimal currentPrice, decimal previousPrice)
        {
            if (previousPrice == 0) return 0;
            return Math.Abs(Math.Round((currentPrice - previousPrice) / previousPrice * 100, 2));
        }

        private bool ShowAlertIcon()
        {
            var currentContact = _customerContext.CurrentContact;
            if (currentContact == null) return false;

            return _bullionContactHelper.IsBullionAccount(currentContact);
        }
    }
}