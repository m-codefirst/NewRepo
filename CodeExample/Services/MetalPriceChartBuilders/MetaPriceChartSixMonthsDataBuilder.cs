using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Models.DDS;

namespace TRM.Web.Services.MetalPriceChartBuilders
{
    public class MetaPriceChartSixMonthsDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.SixMonths;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddDays(-183);
        protected override int NumberOfDataPoints => 558;

        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.HOUR, 8);
        protected override List<DateParts> FilteredDateParts
            => new List<DateParts> { DateParts.YEAR, DateParts.MONTH, DateParts.DAY, DateParts.HOUR };

        public MetaPriceChartSixMonthsDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}