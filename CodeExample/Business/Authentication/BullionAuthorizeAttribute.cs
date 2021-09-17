using System;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Commerce.Order;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Business.Cart;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Services;

namespace TRM.Web.Business.Authentication
{
    public class BullionAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly CustomerContext _customerContext;
        private readonly IUserService _userService;
        private readonly IKycHelper _kycHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public bool CheckKycPassed { get; set; }
        public bool CheckSippAccount { get; set; }
        public bool NeedCustomerBullionObsAccountNumber { get; set; }
        public bool CheckBullionUnableToDepositViaCard { get; set; }
        public bool AbleWhenImpersionating { get; set; }

        public bool JsRedirect { get; set; }

        public BullionAuthorizeAttribute()
        {
            _bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();
            _userService = ServiceLocator.Current.GetInstance<IUserService>();
            _customerContext = CustomerContext.Current;
            _kycHelper = ServiceLocator.Current.GetInstance<IKycHelper>();
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            if (PageEditing.PageIsInEditMode) return;

            //var redirectUrl = filterContext.GetRegistrationPage();
            var redirectPage = filterContext.GetBullionExistingAccountLandingPage();

            if (!AuthorizeCore(filterContext.HttpContext))
            {
                if (JsRedirect)
                {
                    filterContext.Result = new JavaScriptResult
                    {
                        Script = $"<script type=\"text/javascript\">window.location='{redirectPage}';</script>"
                    };
                }
                else
                {
                    filterContext.Result = new RedirectResult(redirectPage);
                }

                return;
            }

            if ((AbleWhenImpersionating && _userService.IsImpersonating()) || !ValidateBullionRestriction()) return;

            var redirectUrl = filterContext.GetMyAccountPageUrl(redirectPage);

            var cartService = ServiceLocator.Current.GetInstance<ITrmCartService>();

            var buyNowCart = cartService.LoadCart(cartService.DefaultBuyNowCartName);
            if (buyNowCart != null && buyNowCart.GetAllLineItems().Any())
                return;

            var bullionCart = cartService.LoadCart(cartService.DefaultBullionCartName);
            if (bullionCart != null && bullionCart.GetAllLineItems().Any())
                return;

            var basketCart = cartService.LoadCart(cartService.DefaultCartName);
            if (basketCart != null && basketCart.GetAllLineItems().Any())
                return;

            if (JsRedirect)
            {
                filterContext.Result = new JavaScriptResult
                {
                    Script = $"<script type=\"text/javascript\">window.location='{redirectUrl}';</script>"
                };
            }
            else
            {
                filterContext.Result = new RedirectResult(redirectUrl);
            }
        }

        private bool ValidateBullionRestriction()
        {
            var currentContact = _customerContext.CurrentContact;

            var bullionAccountNumber = currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber);

            var unableToDepositViaCard = currentContact.GetBooleanProperty(StringConstants.CustomFields.BullionUnableToDepositViaCard);
            
            return !_bullionContactHelper.IsBullionAccount(currentContact) 
                   || CheckKycPassed && !_kycHelper.KycCanProceed(currentContact) 
                   || CheckSippAccount && _bullionContactHelper.IsSippContact(currentContact)
                   || NeedCustomerBullionObsAccountNumber && string.IsNullOrEmpty(bullionAccountNumber)
                   || CheckBullionUnableToDepositViaCard && unableToDepositViaCard;
        }
    }
}