using System.Collections.Generic;
using TRM.Web.Constants;

namespace TRM.Web.Services.MetalPriceChartBuilders
{
    public interface IMetalPriceChartDataBuilder
    {
        List<ChartDataViewModel> BuildChartData(string currency, string commodity);
        ChartDataSummaryViewModel PopulateChartDataWithLowHigh(ref List<ChartDataViewModel> chartData, string currency, string commodity);
        bool CanBuild(HistoricPeriod period);
    }
}