using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using PricingAndTradingService.Models;
using PricingAndTradingService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EPiServer;
using EPiServer.Web;
using TRM.Web.Models.DDS;
using TRM.Web.Models.Pages;
using TRM.Shared.Extensions;
using static PricingAndTradingService.Models.Constants;

namespace TRM.Web.Business.DataAccess
{
    [ServiceConfiguration(typeof(PampMetalPriceSyncRepository), Lifecycle = ServiceInstanceScope.Transient)]
    [ServiceConfiguration(typeof(PampMetalSyncRepositoryBase<PampMetalPriceSync>), Lifecycle = ServiceInstanceScope.Transient)]
    public class PampMetalPriceSyncRepository : PampMetalSyncRepositoryBase<PampMetalPriceSync>
    {
        private readonly IContentLoader _contentLoader;
        private readonly ISynchronizedObjectInstanceCache _synchronizedObjectInstanceCache;

        private readonly IAmLocalPriceDataHelper _localPriceDataHelper;

        public PampMetalPriceSyncRepository(IContentLoader contentLoader, ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache, IAmLocalPriceDataHelper localPriceDataHelper)
        {
            _contentLoader = contentLoader;
            _synchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
            _localPriceDataHelper = localPriceDataHelper;
        }

        protected Lazy<IPricingAndTradingService> PricingAndTradingService => new Lazy<IPricingAndTradingService>(() =>
        {
            return ServiceLocator.Current.GetInstance<IPricingAndTradingService>();
        });

        protected AuthenticationDetails AuthenticationDetails
        {
            get
            {
                var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
                if (startPage == null) return new AuthenticationDetails();

                return new AuthenticationDetails
                {
                    BaseUrl = startPage.PampApiUrl,
                    Password = startPage.PampApiPassword,
                    UserId = startPage.PampApiUsername
                };
            }
        }

        public IEnumerable<MetalPrice> GetLastMetalPriceListInCurrency(string currency)
        {
            var latestMetaPricesByCurrency = GetList()
                .Where(x => x.Currency == currency)
                .OrderByDescending(x => x.CreatedDate)
                .Take(2)
                .AsNoTracking()// One raw json generates 6 records, 2 record same currency
                .ToList();

            return ConvertMetalPriceList(latestMetaPricesByCurrency);
        }

        public IEnumerable<MetalPrice> ConvertMetalPriceList(List<PampMetalPriceSync> dbRecords)
        {
            var result = new List<MetalPrice>();

            var groups = dbRecords.GroupBy(x => x.Currency);

            var goldCode = MetalType.Gold.GetDescriptionAttribute();
            var silverCode = MetalType.Silver.GetDescriptionAttribute();
            var platinumCode = MetalType.Platinum.GetDescriptionAttribute();

            foreach (var g in groups)
            {
                // Gold prices
                result.Add(new MetalPrice
                {
                    CurrencyPair = $"{goldCode}/{g.Key}",
                    SellPrice = g.Any(x => x.CustomerBuy) ? g.FirstOrDefault(x => x.CustomerBuy).GoldPrice : decimal.Zero,
                    BuyPrice = g.Any(x => !x.CustomerBuy) ? g.FirstOrDefault(x => !x.CustomerBuy).GoldPrice : decimal.Zero
                });

                // Silver prices
                result.Add(new MetalPrice
                {
                    CurrencyPair = $"{silverCode}/{g.Key}",
                    SellPrice = g.Any(x => x.CustomerBuy) ? g.FirstOrDefault(x => x.CustomerBuy).SilverPrice : decimal.Zero,
                    BuyPrice = g.Any(x => !x.CustomerBuy) ? g.FirstOrDefault(x => !x.CustomerBuy).SilverPrice : decimal.Zero
                });

                // Platinum prices
                result.Add(new MetalPrice
                {
                    CurrencyPair = $"{platinumCode}/{g.Key}",
                    SellPrice = g.Any(x => x.CustomerBuy) ? g.FirstOrDefault(x => x.CustomerBuy).PlatinumPrice : decimal.Zero,
                    BuyPrice = g.Any(x => !x.CustomerBuy) ? g.FirstOrDefault(x => !x.CustomerBuy).PlatinumPrice : decimal.Zero
                });
            }

            return result;
        }

        public IEnumerable<MetalPrice> GetLivePrices(string currency = null)
        {
            var metalPrices = PricingAndTradingService.Value.GetPrices(AuthenticationDetails);

            if (metalPrices.Result.InvalidIndicativePricesStopTrading)
            {
                _localPriceDataHelper.HandleInvalidIndicativePrices(metalPrices);
                return null;
            }

            if (metalPrices?.MetalPriceList != null && metalPrices.MetalPriceList.Any())
            {
                // Expand raw json to 6 records
                var utcNow = DateTime.UtcNow;
                var pricesToSave = ConvertToListMetalPriceSync(metalPrices.MetalPriceList, utcNow);

                // Bulk insert into database
                BulkInsert(pricesToSave);

                // Clear today cache so next time it get new data
                _synchronizedObjectInstanceCache.Remove(GetTodayCacheKey());
            }

            return metalPrices?.MetalPriceList?.Where(x => currency == null || x.CurrencyPair.ToLower().Contains(currency.ToLower()));
        }

        private IEnumerable<PampMetalPriceSync> ConvertToListMetalPriceSync(List<MetalPrice> metalPrices, DateTime utcNow)
        {
            var result = new List<PampMetalPriceSync>();

            var currencyList = new[] { "USD", "EUR", "GBP" };
            foreach (var currency in currencyList)
            {
                var sellPrice = new PampMetalPriceSync()
                {
                    Id = Guid.NewGuid(),
                    CustomerBuy = true,
                    Currency = currency,
                    CreatedDate = utcNow
                };
                var buyPrice = new PampMetalPriceSync
                {
                    Id = Guid.NewGuid(),
                    CustomerBuy = false,
                    Currency = currency,
                    CreatedDate = utcNow
                };

                var pricesByCurrency = metalPrices.Where(x => x.CurrencyPair.EndsWith($"/{currency}", StringComparison.OrdinalIgnoreCase)).ToList();

                MapPrices(pricesByCurrency, sellPrice, buyPrice);

                result.Add(sellPrice);
                result.Add(buyPrice);
            }

            // 6 records
            return result;
        }

        private void MapPrices(List<MetalPrice> sourcePrices, PampMetalPriceSync sellPriceTarget, PampMetalPriceSync buyPriceTarget)
        {
            sourcePrices.ForEach(price =>
            {
                if (price.CurrencyPair.StartsWith($"{MetalType.Gold.GetDescriptionAttribute()}/", StringComparison.OrdinalIgnoreCase))
                {
                    sellPriceTarget.GoldPrice = price.SellPrice;
                    buyPriceTarget.GoldPrice = price.BuyPrice;
                }
                else if (price.CurrencyPair.StartsWith($"{MetalType.Silver.GetDescriptionAttribute()}/", StringComparison.OrdinalIgnoreCase))
                {
                    sellPriceTarget.SilverPrice = price.SellPrice;
                    buyPriceTarget.SilverPrice = price.BuyPrice;
                }
                else if (price.CurrencyPair.StartsWith($"{MetalType.Platinum.GetDescriptionAttribute()}/", StringComparison.OrdinalIgnoreCase))
                {
                    sellPriceTarget.PlatinumPrice = price.SellPrice;
                    buyPriceTarget.PlatinumPrice = price.BuyPrice;
                }
            });
        }

        public void CleanupData()
        {
            context.Database.CommandTimeout = 600;
            context.Database.ExecuteSqlCommand("sp_PampMetalPriceSync_DeleteUnneededRecords");
        }

        public override Guid Insert(PampMetalPriceSync pampMetalPrice)
        {
            context.PampMetalPriceSync.Add(pampMetalPrice);
            context.SaveChanges();
            return pampMetalPrice.Id;
        }

        public override IQueryable<PampMetalPriceSync> GetList()
        {
            return context.PampMetalPriceSync;
        }

        public int DeleteDuplicateRecords()
        {
            var cleanDuplicateRecords = @"WITH cte AS (
                                            SELECT 
                                                *,
                                                ROW_NUMBER() OVER (
                                                    PARTITION BY 
                                                        CreatedDate, 
                                                        Currency, 
                                                        CustomerBuy
                                                    ORDER BY 
                                                        CreatedDate, 
                                                        Currency, 
                                                        CustomerBuy
                                                ) row_num
                                            FROM 
                                                custom_PampMetalPriceSync
                                        )
                                        delete FROM cte
                                        WHERE row_num > 1;
                                    ";

            return context.Database.ExecuteSqlCommand(cleanDuplicateRecords);
        }
    }
}