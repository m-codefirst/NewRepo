using Hephaestus.CMS.ViewModels;
using Hephaestus.ContentTypes.Business.Extensions;
using System.Linq;
using System.Web.Mvc;
using TRM.Web.Models.Layouts;

namespace TRM.Web.Extentions
{
    public static class StandardPageHelperExtensions
    {
        public static bool IsValidToShowMenu(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            return IsValidAutomaticMenu(model) || IsValidManualMenu(model);
        }

        public static bool IsValidToShowManualMenu(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            return IsValidManualMenu(model);
        }

        public static bool IsValidToShowAutomaticMenu(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            return IsValidAutomaticMenu(model);
        }

        public static string CssMainBodySectionClass(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            string paddingTopClass = IsValidMenu(model) ? "pt-0" : string.Empty;
            return model.CurrentPage.MainBody != null || model.CurrentPage.DisplayPageHeading
                ? $"mod-section-first {paddingTopClass}"
                : string.Empty;
        }

        public static string CssMiddleContentSectionClass(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            string paddingTopClass = IsValidMenu(model) ? "pt-0" : string.Empty;
            return model.CurrentPage.MainBody == null && !model.CurrentPage.DisplayPageHeading 
                ? $"mod-section-first {paddingTopClass}" 
                : string.Empty;
        }

        public static string CssMainContentClass(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            return IsValidMenu(model) ? "col-md-9 py-2" : string.Empty;
        }

        public static string CssCopyColumnClass(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            return model.CurrentPage.IsFullWidthContainer ? "col-12" : "col-md-9";
        }

        public static string CssTextAlignmentClass(this HtmlHelper helper, IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            return EnumExtensions.GetEnumDescriptionAttrWithFallback(model.CurrentPage.TextAlignment, string.Empty);
        }

        private static bool IsValidMenu(IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            return IsValidAutomaticMenu(model) || IsValidManualMenu(model);
        }

        private static bool IsValidManualMenu(IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            var trmLayoutModel = model.Layout as TrmLayoutModel;
            return model.CurrentPage.ShowManualLeftNavigation &&
                trmLayoutModel?.LeftMenu?.ManualLeftMenuDto?.ChildNavigationItems != null &&
                trmLayoutModel.LeftMenu.ManualLeftMenuDto.ChildNavigationItems.Any();
        }

        private static bool IsValidAutomaticMenu(IPageViewModel<Models.Pages.StandardPage, ILayoutModel> model)
        {
            var trmLayoutModel = model.Layout as TrmLayoutModel;
            return model.CurrentPage.ShowAutomaticLeftNavigation &&
                trmLayoutModel?.LeftMenu?.AutomaticLeftMenuDto?.ChildNavigationItems != null &&
                trmLayoutModel.LeftMenu.AutomaticLeftMenuDto.ChildNavigationItems.Any();
        }
    }
}