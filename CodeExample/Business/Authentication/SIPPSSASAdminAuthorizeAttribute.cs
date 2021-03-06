using System;
using System.Web.Mvc;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Web.Extentions;
using TRM.Web.Helpers;

namespace TRM.Web.Business.Authentication
{
    public class SIPPSSASAdminAuthorizeAttribute : AuthorizeAttribute
    {
        public bool JsRedirect { get; set; }
        private readonly CustomerContext _customerContext;
        private IAmBullionContactHelper _bullionContactHelper;

        public SIPPSSASAdminAuthorizeAttribute()
        {
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            _customerContext = CustomerContext.Current;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            if (PageEditing.PageIsInEditMode) return;

            if (!AuthorizeCore(filterContext.HttpContext))
            {
                if (JsRedirect)
                {
                    filterContext.Result = new JavaScriptResult()
                    {
                        Script = $"<script type=\"text/javascript\">window.location='/';</script>"
                    };
                }
                else
                {
                    filterContext.Result = new RedirectResult("~/");
                }

                return;
            }

            var myAccountUrl = filterContext.GetMyAccountPageUrl("~/");

            var currentContact = _customerContext.CurrentContact;

            if (!_bullionContactHelper.IsPensionProvider(currentContact))
            {
                if (JsRedirect)
                {
                    filterContext.Result = new JavaScriptResult()
                    {
                        Script = $"<script type=\"text/javascript\">window.location='{myAccountUrl}';</script>"
                    };
                }
                else
                {
                    filterContext.Result = new RedirectResult(myAccountUrl);
                }
            }
        }
    }
}