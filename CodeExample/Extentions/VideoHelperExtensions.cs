using EPiServer.Core;
using Hephaestus.CMS.Models.Blocks;
using System.Web.Mvc;
using TRM.Web.Models.Blocks;
using Extensions = TRM.Shared.Extensions.PageDataExtensions;

namespace TRM.Web.Extentions
{
    public static class VideoHelperExtensions
    {
        public static string GetYouTubeUrl(this UrlHelper urlhelper, string baseUrl, string siteUrl)
        {
            return $"{baseUrl}{siteUrl}";
        }

        public static bool IsContainValidImage(this HtmlHelper helper, VideoBlock model)
        {
            return (model.VideoBlockCoverImage != null && !ContentReference.IsNullOrEmpty(model.VideoBlockCoverImage.Image) ||
                !ContentReference.IsNullOrEmpty(model.Image));
        }

        public static string GetImageUrl(this UrlHelper urlhelper, ImageBlock imageBlock)
        {
            return (imageBlock != null && !ContentReference.IsNullOrEmpty(imageBlock.Image)) ?
                    Extensions.ContentUrlExtension(urlhelper, imageBlock.Image) : string.Empty;
        }

        public static string GetImageUrl(this UrlHelper urlhelper, ContentReference image)
        {
            return (!ContentReference.IsNullOrEmpty(image)) ?
                    Extensions.ContentUrlExtension(urlhelper, image) : string.Empty;
        }
    }
}