using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TRM.Web.Extentions;

namespace TRM.Web.Business.Authentication
{
    public class CheckoutProcessAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.RouteData.Values["controller"].ToString().ToLower();
            var action = filterContext.RouteData.Values["action"].ToString().ToLower();

            if (filterContext.Controller.TempData["CheckoutState"] == null)
            {
                if (controller == "bullionquickcheckoutpage")
                {
                    if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
                    {
                        filterContext.Result = new RedirectResult(filterContext.GetBullionExistingAccountLandingPage());
                        filterContext.Controller.TempData["CheckoutState"] = "Quick:Started";
                    }
                    else filterContext.Controller.TempData["CheckoutState"] = "Quick:AuthChecked";
                }
                return;
            }

            if ((string)filterContext.Controller.TempData.Peek("CheckoutState") == "Quick:Started")
            {
                if (controller == "bullionexistingaccountlandingpage")
                {
                    if (filterContext.HttpContext.User.Identity.IsAuthenticated)
                    {
                        filterContext.Result = new RedirectResult(filterContext.GetQuickCheckoutPageUrl());
                        filterContext.Controller.TempData["CheckoutState"] = "Quick:AuthChecked";
                    }
                }
                if (controller == "login" && action == "internallogin")
                {
                    filterContext.Result = new RedirectResult(filterContext.GetQuickCheckoutPageUrl());
                    filterContext.Controller.TempData["CheckoutState"] = "Quick:LoggedIn";
                }
                if (controller == "bullionregistrationpage" && action == "step5")
                {
                    filterContext.Result = new RedirectResult(filterContext.GetQuickCheckoutPageUrl());
                    filterContext.Controller.TempData["CheckoutState"] = "Quick:Registered";
                }
                return;
            }

            if ((string)filterContext.Controller.TempData.Peek("CheckoutState") == "Quick:AuthChecked" || 
                (string)filterContext.Controller.TempData.Peek("CheckoutState") == "Quick:LoggedIn" || 
                (string)filterContext.Controller.TempData.Peek("CheckoutState") == "Quick:Registered")
            {
                if (controller == "bullionquickcheckoutpage" && action == "confirmation")
                {
                    filterContext.Result = new RedirectResult(filterContext.GetMyAccountPageUrl());
                    filterContext.Controller.TempData.Remove("CheckoutState");
                }
                return;
            }
        }
    }

    public class CheckoutProcess
    {
    }
}