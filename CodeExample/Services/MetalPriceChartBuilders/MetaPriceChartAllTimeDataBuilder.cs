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
    public class MetaPriceChartAllTimeDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.AllTime;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddYears(-60);
        protected override int NumberOfDataPoints => 2000;

        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.DAY, 30);

        public MetaPriceChartAllTimeDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}