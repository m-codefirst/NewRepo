using EPiServer.Data.Dynamic;
using EPiServer.Personalization.VisitorGroups;

namespace TRM.Web.Business.VisitorGroups
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class BullionKycCheckModel : CriterionModelBase
    {
        [DojoWidget]
        public bool FailedKycCheck { get; set; }
        
        public override ICriterionModel Copy()
        {
            return ShallowCopy();
        }
    }
}