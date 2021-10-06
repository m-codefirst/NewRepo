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
    public class MetaPriceChartThreeYearsDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.ThreeYears;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddDays(-796);
        protected override int NumberOfDataPoints => 549;

        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.DAY, 2);

        public MetaPriceChartThreeYearsDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}