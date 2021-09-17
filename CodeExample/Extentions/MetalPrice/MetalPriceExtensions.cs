using EPiServer.Data.Dynamic;
using System.Linq;
using TRM.Web.Models.DDS;

namespace TRM.Web.Extentions.MetalPrice
{
    public static class MetalPriceExtensions
    {
        private static DynamicDataStore PampMetalStore => typeof(PampMetal).GetStore();

        public static PampMetal GetPampMetal(this PricingAndTradingService.Models.MetalPrice metalPrice)
        {
            var pampCode = metalPrice?.CurrencyPair.Substring(0, 3);
            return PampMetalStore.Items<PampMetal>().FirstOrDefault(x => x.Code == pampCode);
        }
    }
}
