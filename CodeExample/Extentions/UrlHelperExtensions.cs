using System.Web.Mvc;
using EPiServer;
using EPiServer.Find.Helpers.Text;
using EPiServer.Forms.Helpers.Internal;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc.Html;
using Mediachase.Commerce.Catalog;

namespace TRM.Web.Extentions
{
    public static class UrlHelperExtensions
    {
        private const string Epsremainingpath = "epsremainingpath";
        public static string GetRouteAddress(this Url url, string contextLanguage = null)
        {
            var theUrl = url.GetUrlString();

            if (string.IsNullOrEmpty(theUrl))
                return string.Empty;

            if (theUrl.Contains("?"))
                return theUrl;

            var theQuery = url.QueryCollection[Epsremainingpath];

            return theUrl +
                (!string.IsNullOrWhiteSpace(theQuery)
                    ? '?' + theQuery
                    : url.Query);
        }

        public static string ContentUrl(this UrlHelper urlHelper, string productCode)
        {
            if (productCode.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
            var contentReference = referenceConverter.GetContentLink(productCode);
            
            return urlHelper.ContentUrl(contentReference);
        }
    }
}