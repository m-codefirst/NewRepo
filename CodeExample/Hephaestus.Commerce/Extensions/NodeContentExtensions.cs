using System;
using System.Text;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;

namespace Hephaestus.Commerce.Extensions
{
    public static class NodeContentExtensions
    {
        public static bool ImplementsInterface(this NodeContent nodeContent, Type interfaceType)
        {
            var type = nodeContent.GetOriginalType();
            return type != null && interfaceType.IsAssignableFrom(type);
        }

        public static string GetExternalUrl(this NodeContent nodeContent)
        {
            const string id = "id";
            if (nodeContent == null)
            {
                return null;
            }

            var url = new UrlBuilder("");
            if (url.QueryCollection[id] != null && int.Parse(url.QueryCollection[id]) != nodeContent.ContentLink.ID)
            {
                nodeContent.ContentLink = new PageReference(int.Parse(url.QueryCollection[id]));
            }
            Global.UrlRewriteProvider.ConvertToExternal(url, nodeContent.ContentLink, Encoding.UTF8);
            return url.Uri.ToString();
        }
    }
}