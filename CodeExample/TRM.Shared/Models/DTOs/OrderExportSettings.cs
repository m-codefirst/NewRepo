namespace TRM.Shared.Models.DTOs
{
    public class OrderExportSettings
    {
        public int OrdersToRetrieve { get; set; }
        public bool DefaultShipComplete { get; set; }
        public string DefaultPromotionCode { get; set; }

    }
}
