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
    public class MetaPriceChartMonthDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.Month;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddDays(-31);
        protected override int NumberOfDataPoints => 372;

        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.HOUR, 2);
        protected override List<DateParts> FilteredDateParts
            => new List<DateParts> { DateParts.YEAR, DateParts.MONTH, DateParts.DAY, DateParts.HOUR };

        public MetaPriceChartMonthDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}