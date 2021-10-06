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
    public class MetalPriceChartTodayDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.Today;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddHours(-24);
        protected override int NumberOfDataPoints => 480;
        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.MINUTE, 3);

        protected override List<DateParts> FilteredDateParts
            => new List<DateParts> { DateParts.YEAR, DateParts.MONTH, DateParts.DAY, DateParts.HOUR, DateParts.MINUTE };

        public MetalPriceChartTodayDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}