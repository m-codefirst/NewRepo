using EPiServer.Personalization.VisitorGroups;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Security;
using System.Security.Principal;
using System.Web;
using TRM.Web.Business.Cart;
using TRM.Web.Helpers.Bullion;

namespace TRM.Web.Business.VisitorGroups
{
    [VisitorGroupCriterion(
        Category = "Visitor Groups",
        DisplayName = "Bullion Kyc Check Fail/Pass Group",
        Description = "Customer Group based on BullionKYCStatus")]
    public class BullionKycCheckCriterion : CriterionBase<BullionKycCheckModel>
    {
        public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
        {
            bool isMatch = false;
            if (principal == null || !principal.Identity.IsAuthenticated) return isMatch;

            var trmCartService = ServiceLocator.Current.GetInstance<ITrmCartService>();

            var customerContact = principal.GetCustomerContact();
            if (customerContact == null) return isMatch;
            var kycHelper = ServiceLocator.Current.GetInstance<IKycHelper>();
            if (Model.FailedKycCheck)
            {
                isMatch = !kycHelper.KycCanProceed(customerContact);

                if (isMatch == true) return isMatch;

                return trmCartService.CheckMinInvestAmount(customerContact);
            }

            isMatch = kycHelper.KycCanProceed(customerContact);

            if (isMatch == false) return isMatch;

            return !trmCartService.CheckMinInvestAmount(customerContact);

        }
    }
}