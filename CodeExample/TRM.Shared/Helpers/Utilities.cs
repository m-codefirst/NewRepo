using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace TRM.Shared.Helpers
{
    public static class Utilities
    {
        public static ContentReference GetStartPageReference()
        {
            if (SiteDefinition.Current.StartPage != null && SiteDefinition.Current.StartPage.ID != 0)
                return SiteDefinition.Current.StartPage;

            var siteDefinitionRepository = ServiceLocator.Current.GetInstance<ISiteDefinitionRepository>();
            var firstSiteDefinition = siteDefinitionRepository.List().FirstOrDefault();

            return firstSiteDefinition?.StartPage;
        }

        public static string GetUrlValueFromStartPage(string propertyName)
        {
            var startPageData = DataFactory.Instance.GetPage(ContentReference.StartPage);
            if (startPageData == null)
            {
                return ContentReference.StartPage.GetFriendlyUrl();
            }

            var result = string.Empty;
            var property = startPageData.Property[propertyName];
            if (property == null || property.IsNull)
                return string.IsNullOrEmpty(result) ? ContentReference.StartPage.GetFriendlyUrl() : result;

            if (property.PropertyValueType == typeof(PageReference))
            {
                var propertyValue = property.Value as PageReference;
                if (propertyValue != null)
                {
                    result = propertyValue.GetFriendlyUrl();
                }
            }

            if (property.PropertyValueType != typeof(ContentReference))
                return string.IsNullOrEmpty(result) ? ContentReference.StartPage.GetFriendlyUrl() : result;
            {
                var propertyValue = property.Value as ContentReference;
                var pr = propertyValue as PageReference;
                if (pr != null)
                {
                    result = pr.GetFriendlyUrl();
                }
            }
            return string.IsNullOrEmpty(result) ? ContentReference.StartPage.GetFriendlyUrl() : result;
        }

        public static string GetFriendlyUrl(this PageReference pageReference)
        {
            if (pageReference == null)
            {
                return string.Empty;
            }

            var page = DataFactory.Instance.GetPage(pageReference);

            if (UrlRewriteProvider.IsFurlEnabled)
            {
                var urlBuilder = new UrlBuilder(new Uri(page.LinkURL, UriKind.RelativeOrAbsolute));
                Global.UrlRewriteProvider.ConvertToExternal(urlBuilder, page.PageLink, System.Text.Encoding.UTF8);
                return urlBuilder.ToString();
            }
            else
            {
                return page.LinkURL;
            }
        }



    }
}
