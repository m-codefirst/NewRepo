using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using Dapper;
using Mediachase.Commerce;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using static PricingAndTradingService.Models.Constants;
using TRM.Shared.Extensions;

namespace TRM.Web.Services.MetalPriceChartBuilders
{
    public enum DateParts
    {
        YEAR,
        MONTH,
        DAY,
        HOUR,
        MINUTE,
        SECOND
    }

    public abstract class MetalPriceChartDataBuilderBase : IMetalPriceChartDataBuilder
    {
        protected readonly ILogger Logger = LogManager.GetLogger(typeof(MetalPriceChartDataBuilderBase));

        protected readonly string ConnectionString = ConfigurationManager.ConnectionStrings[Shared.Constants.StringConstants.BullionCustomDatabaseName].ConnectionString;

        protected Lazy<ISynchronizedObjectInstanceCache> SynchronizedObjectInstanceCache =
            new Lazy<ISynchronizedObjectInstanceCache>(() => ServiceLocator.Current.GetInstance<ISynchronizedObjectInstanceCache>());


        private const string SqlWhere = "Currency = @currency AND CustomerBuy = 1 AND CreatedDate>=@PastDateByPeriod";

        protected readonly PampMetalPriceSyncRepository Repository;
        private readonly LocalizationService _localizationService;

        protected MetalPriceChartDataBuilderBase(PampMetalPriceSyncRepository repository)
        {
            _localizationService = ServiceLocator.Current.GetInstance<LocalizationService>();
        }

        protected MetalPriceChartDataBuilderBase(PampMetalPriceSyncRepository repository, LocalizationService localizationService)
        {
            Repository = repository;
            _localizationService = localizationService;
        }

        public virtual ChartDataSummaryViewModel PopulateChartDataWithLowHigh(ref List<ChartDataViewModel> chartData, string currency, string commodity)
        {
            var sql = $@";WITH CTE
                        AS
                        (
	                        SELECT 
	                        Max({MetalColumnMapper[commodity]}) as MaxPrice, Min({MetalColumnMapper[commodity]}) as MinPrice
	                        FROM custom_PampMetalPriceSync WITH(NOLOCK)
	                        WHERE {SqlWhere} AND {MetalColumnMapper[commodity]}<>0
                        )
                        SELECT F1.{MetalColumnMapper[commodity]} as Value, MIN(F1.CreatedDate) as Time, 0 as IsCurrentPrice
                        FROM custom_PampMetalPriceSync F1 WITH(NOLOCK)
                        WHERE {SqlWhere}
                        AND exists(SELECT 1 FROM CTE WHERE F1.{MetalColumnMapper[commodity]} = CTE.MaxPrice OR F1.{MetalColumnMapper[commodity]} = CTE.MinPrice)
                        GROUP BY F1.{MetalColumnMapper[commodity]}
                        UNION

                        SELECT *FROM
                        (
                            SELECT TOP 1 F2.{MetalColumnMapper[commodity]}, f2.CreatedDate as Time, 1 as IsCurrentPrice
                            FROM custom_PampMetalPriceSync F2 WITH(NOLOCK)
                            WHERE CustomerBuy = 1 and currency = @currency AND F2.{MetalColumnMapper[commodity]} <> 0
                            ORDER BY F2.CreatedDate desc
                        )currentPrice

                        ORDER BY Value";

            List<ChartDataViewModel> tempResult = null;
            using (var db = new SqlConnection(ConnectionString))
            {
                tempResult = db.Query<ChartDataViewModel>(sql, new { currency, PastDateByPeriod }).ToList();
            }

            // Min price
            var minMaxPrices = tempResult.Where(x => !x.IsCurrentPrice).OrderBy(x => x.Value).ToList();
            if (minMaxPrices.Any())
            {
                if (!chartData.Any(x => x.Value == minMaxPrices.First().Value))
                {
                    chartData.Add(minMaxPrices.First());
                }

                // Max price
                if (!chartData.Any(x => x.Value == minMaxPrices.Last().Value))
                {
                    chartData.Add(minMaxPrices.Last());
                }
            }

            // Current price
            var currentPrice = tempResult.FirstOrDefault(x => x.IsCurrentPrice);
            if (!chartData.Any(x => x.Time == currentPrice.Time))
            {
                chartData.Add(currentPrice);
            }


            // Re-order chart data list
            chartData = chartData.OrderBy(x => x.Time).ToList();

            var change = decimal.Zero;
            if (chartData.Any() && chartData.Count >= 2)
            {
                change = chartData.Last().Value - chartData.First().Value;
            }

            return new ChartDataSummaryViewModel
            {
                High = tempResult != null ? new Money(tempResult.Last().Value, currency).ToString() : string.Empty,
                Low = tempResult != null ? new Money(tempResult.First().Value, currency).ToString() : string.Empty,
                Current = chartData.Any() ? new Money(chartData.Last().Value, currency).ToString() : string.Empty,
                Change = $"{(change > 0 ? "+" : string.Empty)}{new Money(change, currency)}",
                ChangeNumber = change,
                TableHeading = GetTableSummaryHeading(commodity, currency),
                TimePeriodLegendName = GetPeriodLegendName()
            };
        }

        private string GetPeriodLegendName()
        {
            if (HistoricPeriodKey == HistoricPeriod.Live)
                return string.Empty;
            var resourceKey = string.Format(StringResources.PeriodLegendName, HistoricPeriodKey);

            var defaultText = "";
            switch (HistoricPeriodKey)
            {
                case HistoricPeriod.Week:
                case HistoricPeriod.Month:
                case HistoricPeriod.Year:
                    defaultText= HistoricPeriodKey.ToString() + "ly";
                    break;
                case HistoricPeriod.Today:
                    defaultText= "Daily";
                    break;
                case HistoricPeriod.ThreeMonths:
                    defaultText = "Three months";
                    break;
                case HistoricPeriod.SixMonths:
                    defaultText = "Six months";
                    break;
                case HistoricPeriod.ThreeYears:
                    defaultText = "Three years";
                    break;
                case HistoricPeriod.FiveYears:
                    defaultText = "Five years";
                    break;
                case HistoricPeriod.AllTime:
                    defaultText = "All time";
                    break;
                default:
                    defaultText = HistoricPeriodKey.ToString();
                    break;
            }

            return _localizationService.GetStringByCulture(resourceKey, defaultText, ContentLanguage.PreferredCulture) + " ";
        }

        private string GetTableSummaryHeading(string commodity, string currency)
        {
            var defaultCurrencyText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "gbp", "Pounds"},
                { "eur", "Euros"},
                { "usd", "Dollars"}
            };

            var currencyText = _localizationService.GetStringByCulture(string.Format(StringResources.Currency, currency), defaultCurrencyText[currency.Trim()], ContentLanguage.PreferredCulture);
            var metalText = _localizationService.GetStringByCulture(string.Format(StringResources.MetalName, commodity), GetMetalTypeText(commodity), ContentLanguage.PreferredCulture);
            var period = _localizationService.GetStringByCulture(string.Format(StringResources.Period, HistoricPeriodKey), GetDefaultPeriodTextInTableSummaryHeading(), ContentLanguage.PreferredCulture);

            switch (HistoricPeriodKey)
            {
                case HistoricPeriod.Today:
                    return $"{metalText} price {period} in {currencyText}";
                case HistoricPeriod.Week:
                case HistoricPeriod.Month:
                case HistoricPeriod.Year:
                    return $"{metalText} price this {period} in {currencyText}";
                default:
                    return $"{period} {metalText} price in {currencyText}";
            }
        }

        private string GetMetalTypeText(string metalTypeCode)
        {
            var types = Enum.GetValues(typeof(MetalType)).Cast<MetalType>();
            foreach (var type in types)
            {
                if (metalTypeCode.Equals(type.GetDescriptionAttribute(), StringComparison.OrdinalIgnoreCase))
                {
                    return type.ToString();
                }
            }

            return string.Empty;
        }

        private string GetDefaultPeriodTextInTableSummaryHeading()
        {
            var defaultPeriodText = new Dictionary<HistoricPeriod, string>()
            {
                { HistoricPeriod.ThreeMonths, "three month"},
                { HistoricPeriod.SixMonths, "six month"},
                {HistoricPeriod.ThreeYears, "three year"},
                {HistoricPeriod.FiveYears, "five year"},
                {HistoricPeriod.AllTime, "all year"}
            };

            if (defaultPeriodText.ContainsKey(HistoricPeriodKey))
            {
                return defaultPeriodText[HistoricPeriodKey];
            }

            return HistoricPeriodKey.ToString();
        }

        internal class ChartDataSummaryQueryResult
        {
            public decimal MinPrice { get; set; }
            public decimal MaxPrice { get; set; }
        }

        public virtual List<ChartDataViewModel> BuildChartData(string currency, string commodity)
        {
            var sqlSelect = BuildSqlSelect(commodity);
            using (var db = new SqlConnection(ConnectionString))
            {
                return db.Query<ChartDataViewModel>(sqlSelect,
                    param: new { PastDateByPeriod, currency },
                    commandTimeout: 60).ToList();
            }
        }

        public bool CanBuild(HistoricPeriod period) => HistoricPeriodKey.Equals(period);

        //The detail on https://maginus.atlassian.net/browse/BULL-1490
        //Time period Option 
        protected abstract HistoricPeriod HistoricPeriodKey { get; }

        //DateTime.Now - (Actual Duration)
        protected abstract DateTime PastDateByPeriod { get; }

        //Number of data points = Actual Duration / Granularity (must be same unit)
        protected abstract int NumberOfDataPoints { get; }

        /// <summary>
        /// The date parts used in sql
        /// </summary>
        protected virtual List<DateParts> FilteredDateParts => new List<DateParts> { DateParts.YEAR, DateParts.MONTH, DateParts.DAY };

        /// <summary>
        /// Granularity by date part
        /// </summary>
        protected abstract KeyValuePair<DateParts, int> Granularity { get; }

        protected virtual string BuildSqlSelect(string commodity)
        {

            // Build sql select
            var sqlSelectExt = string.Join(", ", FilteredDateParts.Select(x =>
            {
                if (x == Granularity.Key)
                {
                    return $"CAST(DATEPART({x}, CreatedDate) / {Granularity.Value} AS INT) as [{x}]";
                }
                return $"DATEPART({x}, CreatedDate) as [{x}]";
            }));

            // Build sql group
            var sqlGroupBy = string.Join(",", FilteredDateParts.Select(x =>
            {
                if (x == Granularity.Key)
                {
                    return $"CAST(DATEPART({x}, CreatedDate) / {Granularity.Value} AS INT)";
                }
                return $"DATEPART({x}, CreatedDate)";
            }));

            var sql = $@";WITH CTE
                        AS
                        (
	                        SELECT Min(CreatedDate) as MinCreatedDate, 
	                        {sqlSelectExt}
	                        FROM custom_PampMetalPriceSync WITH(NOLOCK) 
	                        WHERE {SqlWhere}
	                        GROUP BY {sqlGroupBy}
                        )
                        SELECT TOP {NumberOfDataPoints} F1.CreatedDate as Time, F1.[{MetalColumnMapper[commodity]}] as Value
                        FROM custom_PampMetalPriceSync F1 WITH(NOLOCK) 
						WHERE {SqlWhere}
                        AND F1.[{MetalColumnMapper[commodity]}] <> 0
						AND EXISTS(SELECT 1 FROM CTE WHERE CTE.MinCreatedDate = F1.CreatedDate)
                        ORDER BY F1.CreatedDate";

            return sql;
        }

        private readonly Dictionary<string, string> MetalColumnMapper = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "xau", "GoldPrice"},
            { "xag", "SilverPrice"},
            { "xpt", "PlatinumPrice"}
        };
    }
}