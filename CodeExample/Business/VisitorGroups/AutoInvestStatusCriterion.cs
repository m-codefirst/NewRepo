using System;
using System.Security.Principal;
using System.Web;
using EPiServer.Find.Helpers.Text;
using EPiServer.Personalization.VisitorGroups;
using Mediachase.Commerce.Security;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;

namespace TRM.Web.Business.VisitorGroups
{
    [VisitorGroupCriterion(
        Category = "Visitor Groups",
        DisplayName = "Auto Invest status group",
        Description = "Auto Invest status group based on Auto Invest status")]
    public class AutoInvestStatusCriterion : CriterionBase<AutoInvestStatusCriterionModel>
    {
        public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
        {
            if (principal == null || !principal.Identity.IsAuthenticated || Model.CustomerAutoInvestStatus.IsNullOrWhiteSpace())
            {
                return false;
            }

            var customerContact = principal.GetCustomerContact();
            if (customerContact == null) return false;

            var status = customerContact.GetEnumProperty<Constants.Enums.AutoInvestUpdateOrderStatus>(StringConstants.CustomFields.LastAutoInvestStatus);

            return ((int) status).ToString().Equals(Model.CustomerAutoInvestStatus, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}