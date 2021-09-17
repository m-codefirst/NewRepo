using System;
using System.Web.Mvc;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Web.Extentions;
using TRM.Web.Helpers;

namespace TRM.Web.Business.Authentication
{
    public class RestrictAccessForBullionUsersAttribute : AuthorizeAttribute
    {
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly CustomerContext _customerContext;

        public RestrictAccessForBullionUsersAttribute()
        {
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            _customerContext = CustomerContext.Current;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            if (PageEditing.PageIsInEditMode) return;

            var currentContact = _customerContext.CurrentContact;

            var myAccountUrl = filterContext.GetMyAccountPageUrl();
            if (string.IsNullOrEmpty(myAccountUrl)) return;

            var authrizeCore = AuthorizeCore(filterContext.HttpContext);
            
            if (!authrizeCore || PageEditing.PageIsInEditMode) return;

            if (currentContact == null || !_bullionContactHelper.IsBullionAccount(currentContact)) return;

            filterContext.Result = new RedirectResult(myAccountUrl);
        }
    }
}