using System;
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
        DisplayName = "Customer Type Group",
        Description = "Customer Type Group based on Customer Type")]
    public class CustomerTypeCriterion : CriterionBase<CustomerTypeModel>
    {
        public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
        {
            if (principal == null || !principal.Identity.IsAuthenticated) return false;

            var customerContact = principal.GetCustomerContact();
            if (customerContact == null) return false;
            var customerType = customerContact.GetStringProperty(StringConstants.CustomFields.CustomerType);
            if (Model.IsCustomerType == StringConstants.CustomerType.Consumer && 
                string.IsNullOrEmpty(customerType)) return true;
            
            if (string.IsNullOrEmpty(Model.IsNotCustomerType)) return customerType.Contains(Model.IsCustomerType);

            if (Model.IsNotCustomerType == StringConstants.CustomerType.Consumer &&
                string.IsNullOrEmpty(customerType)) return false;

            return customerType.Contains(Model.IsCustomerType) && !customerType.Contains(Model.IsNotCustomerType);
        }
    }
}