using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Internal;
using EPiServer.Web.Routing;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using TRM.Shared.Constants;
using static TRM.Shared.Constants.Enums;

namespace TRM.Shared.Extensions
{
    public static class PageDataExtensions
    {
        public static string ContentUrlExtension(this UrlHelper urlHelper, Url url)
        {
            if (url == null || url.IsEmpty()) { return string.Empty; }

            if (url.IsAbsoluteUri) { return url.ToString(); }

            var currentUrl = HttpContext.Current.Request.Url;
            var responseUri = new Uri(currentUrl, UrlEncoder.Encode(UrlResolver.Current.GetUrl(url.ToString())));
            if (responseUri.Host != currentUrl.Host)
            {
                responseUri = new Uri($"{currentUrl.Scheme}://{currentUrl.Authority}{responseUri.AbsolutePath}{responseUri.Fragment}", UriKind.Absolute);

                if (currentUrl != null && currentUrl.ToString().Contains("/old/"))
                { responseUri = new Uri($"{currentUrl.Scheme}://{currentUrl.Authority}{responseUri.AbsolutePath.Replace("/old/", "/")}{responseUri.Fragment}", UriKind.Absolute); }

                return $"{responseUri.AbsolutePath}{responseUri.Fragment}";
            }

            var stringUrl = UrlEncoder.Encode(UrlResolver.Current.GetUrl(url.ToString()) ?? url.ToString());
            if (currentUrl != null && currentUrl.ToString().Contains("/old/"))
            {
                return stringUrl.Replace("/old/", "/");
            }

            return stringUrl;
        }
        public static string ContentUrlExtension(this Url url)
        {
            if (url == null || url.IsEmpty()) { return string.Empty; }

            if (url.IsAbsoluteUri) { return url.ToString(); }

            var currentUrl = HttpContext.Current.Request.Url;
            var responseUri = $"{currentUrl.Scheme}://{currentUrl.Authority}/{url}";
            return responseUri;
        }
        public static string GetExternalUrl_V2(this ContentReference contentLink)
        {
            return ContentUrlResolver(contentLink);
        }
        public static string ContentUrlExtension(this UrlHelper urlHelper, ContentReference contentLink)
        {
            return ContentUrlResolver(contentLink);
        }

        private static string ContentUrlResolver(ContentReference contentLink)
        {
            var instance = ServiceLocator.Current.GetInstance<IUrlResolver>();
            if (!ContentReference.IsNullOrEmpty(contentLink))
            {
                string content = instance.GetUrl(contentLink);
                if (!string.IsNullOrEmpty(content))
                {
                    var currentUrl = HttpContext.Current != null ? HttpContext.Current.Request.Url :
                        new Uri(SiteDefinition
                        .Current
                        .Hosts
                        .FirstOrDefault(x => x.Type == HostDefinitionType.Primary)
                        .Url
                        .ToString());
                    var responseUri = new Uri(currentUrl, UrlEncoder.Encode(content));
                    if (content != null && responseUri.Host != currentUrl.Host)
                    {
                        if (contentLink.ProviderName == "CatalogContent")
                        {
                            responseUri = new Uri($"{currentUrl.Scheme}://{currentUrl.Authority}{responseUri.AbsolutePath}{responseUri.Fragment}", UriKind.Absolute);
                        }
                    }

                    if (content.Contains("/old/"))
                    { responseUri = new Uri($"{currentUrl.Scheme}://{currentUrl.Authority}{responseUri.AbsolutePath.Replace("/old/", "/")}{responseUri.Fragment}", UriKind.Absolute); }

                    return $"{responseUri.AbsolutePath}{responseUri.Fragment}";
                }
            }
            return string.Empty;
        }

        public static XhtmlString ParseXhtmlString(this HtmlHelper htmlHelper, XhtmlString xhtmlstring)
        {
            if (!(xhtmlstring == null || xhtmlstring.IsEmpty))
            {
                xhtmlstring = xhtmlstring.CreateWritableClone();
                for (var i = 0; i < xhtmlstring.Fragments.Count; i++)
                {
                    var sFragment = xhtmlstring.Fragments[i];
                    if (sFragment is UrlFragment)
                    {
                        var url = UrlResolver.Current.GetUrl(sFragment.InternalFormat);
                        var linkType = GetLinkTypeForLink(sFragment, url);
                        if (linkType == LinkItemType.Internal)
                        {
                            UrlBuilder urlBuilder = new UrlBuilder(sFragment.InternalFormat);
                            Global.UrlRewriteProvider.ConvertToExternal(urlBuilder, null, Encoding.UTF8);
                            var path = urlBuilder.Path;
                            var currentUrl = HttpContext.Current.Request.Url;
                            var absoluteUrl = new Uri(currentUrl, path);

                            var responseUri = new Uri($"{currentUrl.Scheme}://{currentUrl.Authority}{absoluteUrl.AbsolutePath}{absoluteUrl.Fragment}", UriKind.Absolute);

                            if (url.Contains("/old/"))
                            { responseUri = new Uri($"{currentUrl.Scheme}://{currentUrl.Authority}{responseUri.AbsolutePath.Replace("/old/", "/")}{responseUri.Fragment}", UriKind.Absolute); }

                            var outFragment = new UrlFragment($"{responseUri.AbsolutePath}{responseUri.Fragment}");
                            xhtmlstring.Fragments[i] = outFragment;
                        }
                    }
                }
            }

            return xhtmlstring;
        }
        private static LinkItemType GetLinkTypeForLink(IStringFragment stringFragment, string url)
        {
            var linkType = LinkItemType.External;
            if (stringFragment?.ReferencedPermanentLinkIds?.Count > 0)
            {
                linkType = LinkItemType.Internal;
                if (RoyalMintObjectTypes.MediaTypes.Any(extension => url.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase)))
                {
                    linkType = LinkItemType.Download;
                }
            }

            return linkType;
        }

        public static XhtmlString CleanXhtmlString(this HtmlHelper htmlHelper, XhtmlString xhtmlstring)
        {
            return CleanXhtmlStringCommon(xhtmlstring);
        }
        public static XhtmlString CleanXhtmlString(this XhtmlString xhtmlstring)
        {
            return CleanXhtmlStringCommon(xhtmlstring);
        }

        private static XhtmlString CleanXhtmlStringCommon(this XhtmlString xhtmlstring)
        {
            if (!(xhtmlstring == null || xhtmlstring.IsEmpty))
            {
                xhtmlstring = ParseXhtmlString(null, xhtmlstring);
                xhtmlstring = xhtmlstring.CreateWritableClone();
                for (var i = 0; i < xhtmlstring.Fragments.Count; i++)
                {
                    var stringWriter = new StringWriter();
                    var sFragment = xhtmlstring.Fragments[i];
                    if (!(sFragment is UrlFragment))
                    {
                        var interText = Regex.Replace(sFragment.InternalFormat, @"<(.|\n)*?>", string.Empty); // Removed HTML tags

                        HttpUtility.HtmlDecode(interText, stringWriter); // Decode the encoded string

                        var outFragment = new StaticFragment(stringWriter.ToString());
                        xhtmlstring.Fragments[i] = outFragment;
                    }
                }
            }

            return xhtmlstring;
        }
    }
}
