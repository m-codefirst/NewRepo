using System;
using System.Collections.Generic;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Models.DDS;
using Dapper;
using System.Data.SqlClient;
using System.Linq;

namespace TRM.Web.Services.MetalPriceChartBuilders
{
    public class MetaPriceChartYearDataBuilder : MetalPriceChartDataBuilderBase
    {
        protected override HistoricPeriod HistoricPeriodKey => HistoricPeriod.Year;
        protected override DateTime PastDateByPeriod => DateTime.UtcNow.AddDays(-366);
        protected override int NumberOfDataPoints => 366;
        protected override KeyValuePair<DateParts, int> Granularity => new KeyValuePair<DateParts, int>(DateParts.DAY, 1);

        public MetaPriceChartYearDataBuilder(PampMetalPriceSyncRepository repository) : base(repository)
        {
        }
    }
}