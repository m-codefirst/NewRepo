using TRM.Web.Models.Blocks.Bullion;

namespace TRM.Web.Business.Pricing
{
    public interface IPremiumCalculatorResult
    {
        IAmBullionPremiumSetting BullionPremiumSetting { get; set; }
        IAmQuantityBreakSetting QuantityBreakSetting { get; set; }
        decimal PriceIncludedPremium { get; set; }
        decimal PremiumValue { get; set; }
    }

    public class PremiumCalculatorResult : IPremiumCalculatorResult
    {
        public PremiumCalculatorResult(
            IAmBullionPremiumSetting bullionPremiumSetting,
            IAmQuantityBreakSetting quantityBreakSetting,
            decimal priceIncludedPremium)
        {
            BullionPremiumSetting = bullionPremiumSetting;
            QuantityBreakSetting = quantityBreakSetting;
            PriceIncludedPremium = priceIncludedPremium;
        }
        public virtual IAmBullionPremiumSetting BullionPremiumSetting { get; set; }
        public virtual IAmQuantityBreakSetting QuantityBreakSetting { get; set; }
        public virtual decimal PriceIncludedPremium { get; set; } //Price included premium
        public virtual decimal PremiumValue { get; set; } //Only Premium

    }
}