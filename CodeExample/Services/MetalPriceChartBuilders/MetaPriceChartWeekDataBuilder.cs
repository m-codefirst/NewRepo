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
    public class MetaPriceChartWeekDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.Week;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddDays(-7);
        protected override int NumberOfDataPoints => 504;

        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.MINUTE, 20);
        protected override List<DateParts> FilteredDateParts
            => new List<DateParts> { DateParts.YEAR, DateParts.MONTH, DateParts.DAY, DateParts.HOUR, DateParts.MINUTE };

        public MetaPriceChartWeekDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}