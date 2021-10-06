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
    public class MetaPriceChartFiveYearsDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.FiveYears;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddDays(-1327);
        protected override int NumberOfDataPoints => 458;

        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.DAY, 4);

        public MetaPriceChartFiveYearsDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}