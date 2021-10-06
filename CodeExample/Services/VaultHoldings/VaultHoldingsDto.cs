using Mediachase.Commerce;
using TRM.Web.Models.ViewModels.Bullion.Portfolio;
using TRM.Web.Models.ViewModels.MetalPrice;

namespace TRM.Web.Services.VaultHoldings
{
    public class VaultHoldingsDto
    {
        public VaultHoldingsMetalGroupDto Gold { get; set; }
        
        public VaultHoldingsMetalGroupDto Silver { get; set; }
        
        public VaultHoldingsMetalGroupDto Platinum { get; set; }
    }

    public class VaultHoldingsMetalGroupDto
    {
        public VaultHoldingsTypeGroupDto Coins { get; set; }
        
        public VaultHoldingsTypeGroupDto Bars { get; set; }
        
        public VaultHoldingsTypeGroupDto Digital { get; set; }

        public VaultHoldingsTypeGroupDto LittleTreasure { get; set; }

        public Money Amount { get; set; }

        public PampMetalPriceItemChangeModel PriceChange { get; set; }
    }

    public class VaultHoldingsTypeGroupDto
    {
        public Money Amount { get; set; }
    }
}