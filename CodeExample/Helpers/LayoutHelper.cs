using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web;
using Hephaestus.CMS.Extensions;
using Hephaestus.ContentTypes.Models.Interfaces;
using TRM.Shared.Extensions;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers
{
    public class LayoutHelper : IAmLayoutHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IContentVersionRepository _contentVersionRepository;

        public LayoutHelper(IContentLoader contentLoader, IContentVersionRepository contentVersionRepository)
        {
            _contentLoader = contentLoader;
            _contentVersionRepository = contentVersionRepository;
        }

        public ContentArea GetAnnouncementBannerContentArea(IContent iContent)
        {
            if (!(iContent is IControlAnnouncementBlocks)) return null;

            var contentControlAnnouncementBanners = iContent as IControlAnnouncementBlocks;
            if (contentControlAnnouncementBanners.HideAnnouncement) return null;

            var contentAreaToUse = GetAnnouncementBannerContentAreaFromLink(iContent.ContentLink);
            if (contentAreaToUse != null) return contentAreaToUse;

            return GetAnnouncementBannerContentAreaFromLink(iContent.ParentLink, true) ?? GetAnnouncementBannerContentAreaFromLink(SiteDefinition.Current.StartPage, true);
        }

        public ContentArea GetAnnouncementBannerContentAreaFromLink(ContentReference contentLink, bool fromParent = false)
        {
            if (contentLink == null) return null;

            var contentData = _contentLoader.Get<ContentData>(contentLink);
            var contentControlAnnouncementBanners = contentData as IControlAnnouncementBlocks;
            if (contentControlAnnouncementBanners == null) return null;

            if (contentControlAnnouncementBanners.AnnouncementContentArea != null &&
                !contentControlAnnouncementBanners.AnnouncementContentArea.IsEmpty)
            {
                if (fromParent)
                {
                    var newContentArea = new ContentArea();
                    foreach (var item in contentControlAnnouncementBanners.AnnouncementContentArea.Items)
                    {
                        var cloneItem = item.CreateWritableClone();
                        cloneItem.AllowedRoles = new string[] { };
                        newContentArea.Items.Add(cloneItem);
                    }
                    return newContentArea;
                }
                return contentControlAnnouncementBanners.AnnouncementContentArea;
            }
            else
            {
                return GetAnnouncementBannerContentAreaFromLink(((IContent)contentData).ParentLink, true);
            }
        }

        public string RetrieveGlobalCustomerStylesheet()
        {
            var homeFolder = GetSpecificFolder(_contentLoader, Constants.StringConstants.HomeFolderName, SiteDefinition.Current.SiteAssetsRoot);
            if (homeFolder == null)
            {
                return string.Empty;
            }
            
            var url = GetSpecificCssAssetFileUrl(_contentLoader, homeFolder, Constants.StringConstants.CssCustomerFileName);

            var startPageLatestSavedDateTime = _contentVersionRepository.List(ContentReference.StartPage)
                .Where(x => x.Status == VersionStatus.Published)
                .OrderByDescending(x => x.Saved).FirstOrDefault()?.Saved;

            long cacheBuster = 0;
            if (startPageLatestSavedDateTime.HasValue)
            {
                cacheBuster = (long)(startPageLatestSavedDateTime.Value - DateTime.MinValue).TotalMilliseconds;

            }

            return $"{url}?cb={cacheBuster}";
        }

        private static IContent GetSpecificFolder(IContentLoader contentRepository, string folderName,
            ContentReference parentReference)
        {
            return contentRepository.GetChildren<ContentFolder>(parentReference)
                .FirstOrDefault(x => string.Equals(x.Name, folderName, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetSpecificCssAssetFileUrl(IContentLoader contentRepository, IContent folder,
            string fileName)
        {
            var customerCss = contentRepository.GetChildren<IContent>(folder.ContentLink)
                .FirstOrDefault(x => string.Compare(x.Name, fileName, StringComparison.OrdinalIgnoreCase) == 0);
            return customerCss != null
                ? new UrlBuilder(new ContentReference(customerCss.ContentLink.ID).GetExternalUrl_V2()).Path
                : string.Empty;
        }
        public string RenderRazorViewToString(ControllerContext controllerContext, String viewName, Object model)
        {
            controllerContext.Controller.ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controllerContext, viewName);
                var viewContext = new ViewContext(controllerContext, viewResult.View,
                    controllerContext.Controller.ViewData, controllerContext.Controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(controllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}