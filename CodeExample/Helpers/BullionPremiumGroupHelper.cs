using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public class BullionPremiumGroupHelper : IBullionPremiumGroupHelper
    {
        protected readonly DynamicDataStore Store;
        public BullionPremiumGroupHelper()
        {
            Store = typeof(BullionPremiumGroup).GetStore();
        }

        public List<BullionPremiumGroup> GetBullionPremiumGroup()
        {
            return Store.Items<BullionPremiumGroup>().ToList();
        }

        public string GetCustomerBullionPremiumGroupDisplayName(int valueToGet)
        {
            var premiumGroup = GetBullionPremiumGroup().FirstOrDefault(pg => pg.Value == valueToGet.ToString());
            
            return premiumGroup != null ? premiumGroup.DisplayName : string.Empty;
        }
    }

    public interface IBullionPremiumGroupHelper
    {
        List<BullionPremiumGroup> GetBullionPremiumGroup();
        string GetCustomerBullionPremiumGroupDisplayName(int valueToGet);
    }
}