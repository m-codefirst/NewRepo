using EPiServer.ServiceLocation;
using System.Net;
using System.Web.Mvc;
using Mediachase.Commerce.Customers;
using TRM.Web.Helpers;
using TRM.Web.Services;

namespace TRM.Web.Business.Authentication
{
    public class CustomerServiceFilterForImpersonatingAttribute  : ActionFilterAttribute
    {
#pragma warning disable 649
        private readonly IUserService _userService;
        private readonly IAmBullionContactHelper _bullionContactHelper;
#pragma warning restore 649

        public bool IsAuthorized { get; set; }

        public CustomerServiceFilterForImpersonatingAttribute()
        {
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            _userService = ServiceLocator.Current.GetInstance<IUserService>();

            IsAuthorized = true;
        }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Is impersonating to another user but is not customer service
            var contactBeforeImpersonating = _userService.GetContactBeforeImpersonating();

            if (_userService.IsImpersonating() && (IsAuthorized == false || !_bullionContactHelper.IsCustomerServiceAccount(contactBeforeImpersonating)))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, "You're not authorized to performance this action");
            }
        }
    }

    public class PensionCustomerFilterAttribute : ActionFilterAttribute
    {
#pragma warning disable 649
        private readonly CustomerContext _customerContext;
        private readonly IAmBullionContactHelper _bullionContactHelper;
#pragma warning restore 649

        public PensionCustomerFilterAttribute()
        {
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            _customerContext = CustomerContext.Current;
        }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Is impersonating to another user but is not customer service
            if (_bullionContactHelper.IsSippContact(_customerContext.CurrentContact))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, "You're not authorized to performance this action");
            }
        }
    }
}