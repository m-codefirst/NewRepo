using PricingAndTradingService.Models;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.Pricing
{
    public class PampMetalPriceResult
    {
        public MetalPrice MetalPrice { get; set; }
        public PampMetal PampMetal { get; set; }
    }
}