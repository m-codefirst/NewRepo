using System.Globalization;
using System.Web;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using EPiServer.Web.Routing;

namespace TRM.Web.Extentions
{
    public static class ContentReferenceExt
    {
        public static string GetFriendlyCanonicalUrl(this ContentReference contentReference, CultureInfo language, 
            UrlResolver urlResolver, ISiteDefinitionResolver siteDefinitionResolver, HttpRequestBase request)
        {
            if (contentReference == null) return string.Empty;

            var virtualPath = urlResolver.GetVirtualPath(contentReference)?.VirtualPath;

            var primaryHost = siteDefinitionResolver.Get(request)?.GetPrimaryHost(language);
            if (primaryHost != null) return primaryHost.Url + virtualPath;

            return string.Empty;
        }

        public static string GetAbsoluteUrl(this ContentReference contentReference)
        {
            var internalUrl = UrlResolver.Current.GetUrl(contentReference);

            var url = new UrlBuilder(internalUrl);
            Global.UrlRewriteProvider.ConvertToExternal(url, null, System.Text.Encoding.UTF8);

            return UriSupport.AbsoluteUrlBySettings(url.ToString());
        }
        public static string ToAbsoluteUrl(this string relativeUrl)
        {
            var url = new UrlBuilder(relativeUrl);
            Global.UrlRewriteProvider.ConvertToExternal(url, null, System.Text.Encoding.UTF8);
            return UriSupport.AbsoluteUrlBySettings(url.ToString());
        }

        public static T Resolve<T>(this ContentReference contentReference) where T : ContentData
        {
            if (contentReference == ContentReference.EmptyReference)
            {
                return null;
            }

            return DataFactory.Instance.Get<T>(contentReference);
        }
    }
}