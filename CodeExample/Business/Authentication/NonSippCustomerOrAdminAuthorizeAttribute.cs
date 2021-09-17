using System;
using System.Web.Mvc;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Web.Extentions;
using TRM.Web.Helpers;

namespace TRM.Web.Business.Authentication
{
    public class NonSippCustomerOrAdminAuthorizeAttribute : AuthorizeAttribute
    {
        public bool JsRedirect { get; set; }
        private readonly CustomerContext _customerContext;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public NonSippCustomerOrAdminAuthorizeAttribute()
        {
            _customerContext = ServiceLocator.Current.GetInstance<CustomerContext>();
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            const string homepageRedirectUrl = "/";

            
            if (filterContext == null)
            {
                throw new ArgumentNullException(nameof(filterContext));
            }

            if (PageEditing.PageIsInEditMode)
            {
                return;
            }

            if (!AuthorizeCore(filterContext.HttpContext))
            {
                filterContext.Redirect(JsRedirect, homepageRedirectUrl);
                return;
            }

            var currentContact = _customerContext.CurrentContact;
            if (_bullionContactHelper.IsPensionProvider(currentContact) || _bullionContactHelper.IsSippContact(currentContact))
            {
                filterContext.Redirect(JsRedirect, homepageRedirectUrl);
            }
        }
    }
}