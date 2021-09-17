using Mediachase.Commerce;

namespace TRM.Web.Business.DataAccess
{
    public class EpiPriceBullionKeyInfoModel
    {
        public EpiPriceBullionKeyInfoModel(Currency currency, MarketId market, string premiumGroupName, string premiumGroupValue)
        {
            PremiumGroup = premiumGroupName;
            PremiumGroupValue = premiumGroupValue;
            MarketId = market;
            Currency = currency;
        }       

        public string PremiumGroup { get; set; }
        public string PremiumGroupValue { get; set; }
        public Currency Currency { get; set; }
        public MarketId MarketId { get; set; }
    }

}
