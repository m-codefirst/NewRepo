using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using EPiServer.Personalization.VisitorGroups;
using Mediachase.Commerce.Security;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;

namespace TRM.Web.Business.VisitorGroups
{
    [VisitorGroupCriterion(
        Category = "Visitor Groups",
        DisplayName = "Credit Limit",
        Description = "Visitor group based on credit limit")]
    public class CreditLimitCriterion : CriterionBase<CreditLimitModel>
    {
        public override bool IsMatch(System.Security.Principal.IPrincipal principal, HttpContextBase httpContext)
        {
            if (principal == null || !principal.Identity.IsAuthenticated) return false;

            var customerContact = principal.GetCustomerContact();
            if (customerContact == null) return false;
            var creditLimit = customerContact.GetDecimalProperty(StringConstants.CustomFields.CreditLimitFieldName);

            return Model.HasCreditLimit ? creditLimit > 0 : creditLimit == 0;
        }
    }
}
