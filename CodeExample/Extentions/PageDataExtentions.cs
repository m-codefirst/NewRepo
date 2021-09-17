using System.Collections.Generic;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace TRM.Web.Extentions
{
    public static class PageDataExtentions
    {
        //This is used in the localization pages, please don't use on any page with a controller ie ANY page 
        public static IEnumerable<PageData> GetChildren(this PageData page)
        {
            return ServiceLocator.Current.GetInstance<IContentRepository>().GetChildren<PageData>(page.PageLink);
        }
    }
}