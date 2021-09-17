using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Web;
using TRM.Web.Business.Rendering;

namespace TRM.Web.Business.Initialization
{
    public class TrmViewEngine : RazorViewEngine
    {
        private const string CacheKeyPrefixPartial = "Partial";
        private const string CacheKeyPrefixView = "View";

        public TrmViewEngine()
        {

            ViewLocationFormats = new[]
            {
                TemplateCoordinator.PageFolderForMultiSite + "{1}/{0}.cshtml",
                TemplateCoordinator.PageFolderForMultiSite + "{0}.cshtml",
                TemplateCoordinator.PageFolderForMultiSite + "{1}.cshtml",
                TemplateCoordinator.PageFolder + "{1}/{0}.cshtml",
                TemplateCoordinator.PageFolder + "{0}.cshtml",
                TemplateCoordinator.PageFolder + "{1}.cshtml",
                TemplateCoordinator.BlockFolder + "{1}/{0}.cshtml",
                "~/Views/Layouts/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml",
                TemplateCoordinator.PrototypeFolderForMultiSite + "{0}.cshtml"
            };

            PartialViewLocationFormats = new[]
            {
                TemplateCoordinator.BlockFolderForMultiSite + "{0}.cshtml",
                TemplateCoordinator.MultiSiteBlockPartialsFolder + "{0}.cshtml",
                TemplateCoordinator.MultiSitePagePartialsFolder + "{0}.cshtml",
                TemplateCoordinator.BlockFolder + "{0}.cshtml",
                TemplateCoordinator.PagePartialsFolder + "{0}.cshtml",
                TemplateCoordinator.BlockFolder + "{1}/{0}.cshtml",
                TemplateCoordinator.PagePartialsFolder + "{1}/{0}.cshtml",
                TemplateCoordinator.ImageFileFolder +  "{0}.cshtml",
                TemplateCoordinator.ImageFileFolderForMultiSite +  "{0}.cshtml",
                TemplateCoordinator.MultiSitePaymentMethodsFolder +  "{0}.cshtml",
                TemplateCoordinator.MultiSiteGdprPagePartialFolder + "{0}.cshtml",

                "~/Views/Layouts/{0}.cshtml",
                "~/Views/Shared/{0}.cshtml",
                "~/Views/Shared/DisplayTemplates/{0}.cshtml",
                "~/Views/Shared/PagePartials/{0}.cshtml",
                TemplateCoordinator.PrototypeBlocksFolderForMultiSite + "{0}.cshtml",
                TemplateCoordinator.PrototypeSharedFolderForMultiSite + "{0}.cshtml"
            };
        }

        public override ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            var siteName = SiteDefinition.Current.Name;

            var result = FindPartialView(controllerContext, partialViewName, siteName, useCache);


            return result;
        }


        public override ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            var result = FindCustomView(controllerContext, viewName, masterName, useCache);

            return result;
        }

        private string GetPartialViewFileName(string viewLocationUrl)
        {
            var url = viewLocationUrl;
            if (url.Contains('/'))
            {
                url = viewLocationUrl.Split('/').Last();
            }

            if (url.Contains("."))
            {
                url = url.Split('.').First();
            }

            return url;
        }

        private static string CheckForPlaceHolderAndReplaceWithSiteName(string path)
        {
            return path.Replace(TemplateCoordinator.BespokePathPlaceHolder, SiteDefinition.Current.Name);
        }

        private static string GetControllerArea(ControllerContext controllerContext)
        {
            var area = string.Empty;

            if (controllerContext.RouteData.DataTokens.ContainsKey("area"))
                area = controllerContext.RouteData.DataTokens["area"].ToString();

            return area;
        }

        private static string GetController(ControllerContext controllerContext)
        {
            return controllerContext.RouteData.Values["controller"].ToString();
        }

        private ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, string siteName, bool useCache)
        {
            var controller = GetController(controllerContext);
            var area = GetControllerArea(controllerContext);
            var keyPath = Path.Combine(CacheKeyPrefixPartial, area, controller, partialViewName, SiteDefinition.Current.Name ?? "");

            if (useCache)
            {
                var cacheLocation = ViewLocationCache.GetViewLocation(controllerContext.HttpContext, keyPath);
                if (!string.IsNullOrWhiteSpace(cacheLocation))
                {
                    return new ViewEngineResult(CreatePartialView(controllerContext, cacheLocation), this);
                }
            }

            var attempts = new List<string>();

            var partialViewFormats = string.IsNullOrEmpty(area) ? PartialViewLocationFormats : AreaPartialViewLocationFormats;

            foreach (var rootPath in partialViewFormats)
            {
                string currentPath;

                if (rootPath.Contains(TemplateCoordinator.BespokePathPlaceHolder))
                {
                    var replacedRootPath = CheckForPlaceHolderAndReplaceWithSiteName(rootPath);
                    var fileName = GetPartialViewFileName(partialViewName);

                    currentPath = string.IsNullOrEmpty(area)
                                   ? string.Format(replacedRootPath, fileName)
                                   : string.Format(rootPath, controller, controller, area);
                }
                else
                {
                    currentPath = string.IsNullOrEmpty(area)
                                    ? string.Format(rootPath, partialViewName, controller, siteName)
                                    : string.Format(rootPath, partialViewName, controller, area);
                }

                if (FileExists(controllerContext, currentPath))
                {
                    ViewLocationCache.InsertViewLocation(controllerContext.HttpContext, keyPath, currentPath);

                    return new ViewEngineResult(CreatePartialView(controllerContext, currentPath), this);
                }

                attempts.Add(currentPath);
            }

            return new ViewEngineResult(attempts.Distinct().ToList());
        }

        private ViewEngineResult FindCustomView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            var controller = GetController(controllerContext);
            var area = GetControllerArea(controllerContext);

            var keyPath = Path.Combine(CacheKeyPrefixView, area, controller, viewName, SiteDefinition.Current.Name ?? "");

            var validView = OnValidViewOrNull(controllerContext, keyPath, viewName, masterName);
            if (validView != null) return validView;

            if (useCache)
            {
                var cacheLocation = ViewLocationCache.GetViewLocation(controllerContext.HttpContext, keyPath);
                if (!string.IsNullOrWhiteSpace(cacheLocation))
                {
                    return new ViewEngineResult(CreateView(controllerContext, cacheLocation, masterName), this);
                }
            }

            var attempts = new List<string>();
            var locationFormats = string.IsNullOrEmpty(area) ? ViewLocationFormats : AreaViewLocationFormats;

            foreach (var rootPath in locationFormats)
            {
                var customPath = rootPath;
                if (customPath.Contains(TemplateCoordinator.BespokePathPlaceHolder))
                {
                    customPath = CheckForPlaceHolderAndReplaceWithSiteName(rootPath);
                }
                var currentPath = string.IsNullOrEmpty(area)
                                         ? string.Format(customPath, viewName, controller)
                                         : string.Format(customPath, viewName, controller, area);

                if (FileExists(controllerContext, currentPath))
                {
                    ViewLocationCache.InsertViewLocation(controllerContext.HttpContext, keyPath, currentPath);
                    return new ViewEngineResult(CreateView(controllerContext, currentPath, masterName), this);
                }
                attempts.Add(currentPath);
            }

            return new ViewEngineResult(attempts.Distinct().ToList());
        }

        private ViewEngineResult OnValidViewOrNull(ControllerContext controllerContext, string keyPath, string viewName, string masterName)
        {
            if (!(viewName.Contains("~/Views/" + SiteDefinition.Current.Name + "/Pages") && FileExists(controllerContext, viewName))) return null;

            ViewLocationCache.InsertViewLocation(controllerContext.HttpContext, keyPath, viewName);
            return new ViewEngineResult(CreateView(controllerContext, viewName, masterName), this);
        }

    }
}