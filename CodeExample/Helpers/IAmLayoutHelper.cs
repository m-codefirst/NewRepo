using EPiServer.Core;
using System;
using System.Web.Mvc;

namespace TRM.Web.Helpers
{
    public interface IAmLayoutHelper
    {
        string RetrieveGlobalCustomerStylesheet();
        string RenderRazorViewToString(ControllerContext controllerContext, string viewName, object model);
        ContentArea GetAnnouncementBannerContentArea(IContent iContent);
    }
}