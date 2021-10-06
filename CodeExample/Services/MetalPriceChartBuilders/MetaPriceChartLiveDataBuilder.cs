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
    public class MetaPriceChartLiveDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.Live;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddHours(-1);
        protected override int NumberOfDataPoints => 120;

        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.SECOND, 30);

        protected override List<DateParts> FilteredDateParts
            => new List<DateParts> { DateParts.YEAR, DateParts.MONTH, DateParts.DAY, DateParts.HOUR, DateParts.MINUTE, DateParts.SECOND };

        public MetaPriceChartLiveDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}