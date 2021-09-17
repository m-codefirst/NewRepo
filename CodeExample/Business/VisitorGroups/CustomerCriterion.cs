using System.Security.Principal;
using System.Web;
using EPiServer.Personalization.VisitorGroups;
using Mediachase.Commerce.Security;
using TRM.Shared.Extensions;

namespace TRM.Web.Business.VisitorGroups
{
    [VisitorGroupCriterion(
        Category = "Visitor Groups",
        DisplayName = "Matching Customer",
        Description = "Matching Customer based on Customer Field")]
    public class CustomerCriterion : CriterionBase<CustomerCriterionModel>
    {
        public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
        {
            if (principal == null || !principal.Identity.IsAuthenticated) return false;

            var customerContact = principal.GetCustomerContact();
            if (customerContact == null) return false;

            return customerContact.GetStringProperty(Model.CustomerField) == Model.Value;
        }
    }
}