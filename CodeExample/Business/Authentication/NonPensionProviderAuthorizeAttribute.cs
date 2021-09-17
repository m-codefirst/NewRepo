using System;
using System.Web.Mvc;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Web.Constants;
using TRM.Web.Controllers.Blocks;
using TRM.Web.Controllers.Pages;
using TRM.Web.Controllers.Pages.Bullion;
using TRM.Web.Extentions;
using TRM.Web.Helpers;

namespace TRM.Web.Business.Authentication
{
    public class NonPensionProviderAuthorizeAttribute : AuthorizeAttribute
    {
        public bool JsRedirect { get; set; }
        private readonly CustomerContext _customerContext;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        public NonPensionProviderAuthorizeAttribute()
        {
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            _customerContext = CustomerContext.Current;
        }
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            if (PageEditing.PageIsInEditMode) return;

            var myAccountUrl = filterContext.GetMyAccountPageUrl("~/");

            var currentContact = _customerContext.CurrentContact;

            if (currentContact != null && _bullionContactHelper.IsPensionProvider(currentContact))
            {
                if (filterContext.Controller != null) MessageForPensionProviderRetriction(filterContext.Controller);

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

        private void MessageForPensionProviderRetriction(ControllerBase controllerBase)
        {
            if ((controllerBase as CheckoutPageController) != null)
            {
                CreateMessageCookie(StringResources.NotBeAbleToCheckoutAsAConsumerGuestTitle, StringResources.NotBeAbleToCheckoutAsAConsumerGuest);
                return;
            }

            if ((controllerBase as BasketPageController) != null 
                || (controllerBase as BullionQuickCheckoutPageController) != null
                || (controllerBase as BullionOnlyCheckoutPageController) != null
                || (controllerBase as MixedCheckoutPageController) != null)
            {
                CreateMessageCookie(StringResources.CanNotAccessToBasketTitle, StringResources.CanNotAccessToBasket);
                return;
            }
            if ((controllerBase as BullionPortfolioPageController) != null)
            {
                CreateMessageCookie(StringResources.CanNotAccessToPortfolioTitle, StringResources.CanNotAccessToPortfolio);
                return;
            }

            if ((controllerBase as MyAccountManageAddressesBlockController) != null)
            {
                CreateMessageCookie(StringResources.CanNotAccessToAddressesTitle, StringResources.CanNotAccessToAddresses);
            }
        }

        public void CreateMessageCookie(string title, string content)
        {
            CookieHelper.CreateMessageCookie(Enums.eMessageType.Danger, title, content);
        }
    }
}