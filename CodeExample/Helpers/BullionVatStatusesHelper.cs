using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public class BullionVatStatusesHelper : IBullionVatStatusesHelper
    {
        protected readonly DynamicDataStore Store;
        public BullionVatStatusesHelper()
        {
            Store = typeof(BullionVatStatuses).GetStore();
        }

        public List<BullionVatStatuses> GetBullionVatStatuses()
        {
            var statuses = Store.Items<BullionVatStatuses>().ToList();

            if (statuses.Any()) return statuses;
            statuses.Add(new BullionVatStatuses("Zero", "Zero"));
            statuses.Add(new BullionVatStatuses("Exempt", "Exempt"));
            statuses.Add(new BullionVatStatuses("Standard", "Standard"));
            
            return statuses;
        }
    }
}