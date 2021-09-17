using Castle.Core.Internal;
using EPiServer.Commerce.Order;
using EPiServer.Editor;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.Extensions;
using System;
using System.Web.Mvc;
using TRM.Web.Business.Cart;
using TRM.Web.Extentions;
using TRM.Web.Models.Pages;
using TRM.Shared.Extensions;

namespace TRM.Web.Business.Authentication
{
    public class BullionNonStopTradingAttribute : AuthorizeAttribute
    {
        private readonly Type _checkoutPageType;
        public BullionNonStopTradingAttribute(Type checkoutPageType)          
        {
            _checkoutPageType = checkoutPageType;
        }        

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            if (PageEditing.PageIsInEditMode) return;

            if (!AuthorizeCore(filterContext.HttpContext)) return;
            if (_checkoutPageType == null) return;

            var startPage = filterContext.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || !startPage.StopTrading) return;

            if (_checkoutPageType == typeof(MixedCheckoutPage))
            {
                var cartService = ServiceLocator.Current.GetInstance<ITrmCartService>();
                var bullionItems = cartService.LoadCart(cartService.DefaultBullionCartName)?.GetAllLineItems();
                if (bullionItems.IsNullOrEmpty()) return;
            }

            filterContext.Result = new RedirectResult(startPage.ContentLink.GetExternalUrl_V2());
        }
    }
}