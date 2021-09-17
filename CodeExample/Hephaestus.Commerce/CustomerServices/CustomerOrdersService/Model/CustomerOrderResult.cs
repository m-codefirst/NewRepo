using System;
using Mediachase.Commerce;

namespace Hephaestus.Commerce.CustomerServices.CustomerOrdersService.Model
{
    public class CustomerOrderResult
    {
        public int OrderNo { get; set; }
        public string OrderTracking { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }

        public string BillingCurrency { get; set; }
        public MarketId MarketId { get; set; }
    }
}
