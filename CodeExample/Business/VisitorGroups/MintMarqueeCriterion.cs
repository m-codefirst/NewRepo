using System.Security.Principal;
using System.Web;
using EPiServer.Personalization.VisitorGroups;
using Mediachase.Commerce.Security;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;

namespace TRM.Web.Business.VisitorGroups
{
    [VisitorGroupCriterion(
        Category = "Visitor Groups",
        DisplayName = "Mint Marquee Group",
        Description = "Mint Marquee Group based on Customer Classification Id")]
    public class MintMarqueeCriterion : CriterionBase<MintMarqueeGroupModel>
    {
        public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
        {
            var currentContact = principal.GetCustomerContact();

            return !string.IsNullOrWhiteSpace(currentContact.GetStringProperty(StringConstants.CustomFields.CustClassificationId));
        }
    }
}