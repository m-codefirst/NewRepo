using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using System;
using System.Web.Mvc;
using TRM.Web.Helpers;

namespace TRM.Web.Extentions
{
    public static class BullionContactHelperExtensions
    {
        public static bool IsSippContact(this HtmlHelper html)
        {
            var bullionContactHelper = ServiceLocator.Current.GetInstance<IAmBullionContactHelper>();

            return bullionContactHelper != null ? bullionContactHelper.IsSippContact(CustomerContext.Current.CurrentContact) : false;
        }

        public static Guid GetNewGuid(this HtmlHelper htmlHelper)
        {
            return Guid.NewGuid();
        }
    }
}