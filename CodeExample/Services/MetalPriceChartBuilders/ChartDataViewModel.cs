using System;

namespace TRM.Web.Services.MetalPriceChartBuilders
{
    public class ChartDataViewModel
    {
        public DateTime Time { get; set; }
        public decimal Value { get; set; }
        public bool IsCurrentPrice { get; set; }

        public string StringTime => Time.ToString("dd/MM/yyyy HH:mm:ss \"GMT\"");
    }

    public class ChartDataSummaryViewModel
    {
        public string Current { get; set; }
        public string High { get; set; }
        public string Low { get; set; }
        public string Change { get; set; }
        public decimal ChangeNumber { get; set; }
        public string TableHeading { get; set; }

        public string TimePeriodLegendName { get; set; }
    }
}