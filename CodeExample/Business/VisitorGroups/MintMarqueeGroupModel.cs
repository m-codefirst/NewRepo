using EPiServer.Data.Dynamic;
using EPiServer.Personalization.VisitorGroups;

namespace TRM.Web.Business.VisitorGroups
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class MintMarqueeGroupModel : CriterionModelBase
    {
        public override ICriterionModel Copy()
        {
            return ShallowCopy();
        }
    }
}