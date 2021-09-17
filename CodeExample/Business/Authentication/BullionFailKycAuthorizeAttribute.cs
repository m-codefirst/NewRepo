using System;
using System.Web.Mvc;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Web.Extentions;
using TRM.Web.Helpers;

namespace TRM.Web.Business.Authentication
{
    public class BullionFailKycAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly CustomerContext _customerContext;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionFailKycAuthorizeAttribute()
        {
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            _customerContext = CustomerContext.Current;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var startPage = filterContext.GetStartPage();
            filterContext.Result = new RedirectResult(startPage);
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            if (PageEditing.PageIsInEditMode) return;

            var myAccountUrl = filterContext.GetMyAccountPageUrl("~/");

            var currentContact = _customerContext.CurrentContact;

            var bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            if (!_bullionContactHelper.IsBullionAccount(currentContact) || !bullionContactHelper.HasFailedStage1(currentContact))
            {
                filterContext.Result = new RedirectResult(myAccountUrl);
            }
        }
    }
}