using System.Linq;
using System.Web;
using EPiServer.Web;
using EPiServer.Web.Routing;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Extentions
{
    public static class TrmCategoryExt
    {
        public static string GetCanonicalUrl(this TrmCategoryBase trmCategory, HttpRequestBase request, ISiteDefinitionResolver siteDefinitionResolver, UrlResolver urlResolver)
        {     
            var siteDefinition = siteDefinitionResolver.Get(request);
            if (siteDefinition == null) return string.Empty;

            var myPrimaryHost = siteDefinition.GetPrimaryHost(trmCategory.Language);
            if (myPrimaryHost == null) return string.Empty;

            string url = myPrimaryHost.Url.ToString();
            var relativePath = urlResolver.GetVirtualPath(trmCategory).VirtualPath;
            if (string.IsNullOrEmpty(relativePath)) return string.Empty;

            url += relativePath;

            return url.Contains('?') ? url.Split('?')[0] : url;
        }
    }
}