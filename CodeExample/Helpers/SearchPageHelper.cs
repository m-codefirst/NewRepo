using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Find.Cms;
using EPiServer.Framework.Localization;
using Hephaestus.CMS.Models.Pages;
using Mediachase.Commerce.Catalog;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Extensions;
using TRM.Web.Constants;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.ViewModels;
using static TRM.Web.Constants.StringConstants;

namespace TRM.Web.Helpers
{
    public class SearchPageHelper : ISearchPageHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IAmEntryHelper _trmEntryHelper;
        private readonly IAmAssetHelper _amAssetHelper;
        private readonly LocalizationService _localizationService;
        public SearchPageHelper(IContentLoader contentLoader, IAmEntryHelper trmEntryHelper, IAmAssetHelper amAssetHelper, LocalizationService localizationService)
        {
            _contentLoader = contentLoader;
            _trmEntryHelper = trmEntryHelper;
            _amAssetHelper = amAssetHelper;
            _localizationService = localizationService;
        }
        public IEnumerable<ArticleWithBreadcrumbViewModel> GetArticleWithBreadcrumbViewModel(IContentResult<IContentData> content, ITrmLayoutModel layoutModel, bool processBreadcrumb = true)
        {
            foreach (var item in content.Items)
                yield return GetArticle((IContent)item, layoutModel, processBreadcrumb);
        }

        #region GetTeaser
        private ArticleWithBreadcrumbViewModel GetArticle(IContent currentContent, ITrmLayoutModel layoutModel, bool processBreadcrumb = true)
        {
            var content = _contentLoader.Get<ContentData>(currentContent.ContentLink);
            var viewModel = new ArticleWithBreadcrumbViewModel
            {
                PageUrl = currentContent.ContentLink.GetExternalUrl_V2(),
                BreadcrumbItems = processBreadcrumb ? GenerateBreadcrumbViewModel(currentContent, layoutModel) : new List<BreadcrumbItem>()
            };

            if (content == null || !(content is IControlTeaser iControlTeaser)) return viewModel;

            if (content is PageData || content is BlockData)
            { OnContentFound(currentContent, iControlTeaser, content, viewModel); }
            else if (!(content is CatalogContentBase)) {  } 
            else
            { ContentNotFound(currentContent, iControlTeaser, content, viewModel); }

            return viewModel;
        }
        private void OnContentFound(IContent currentContent, IControlTeaser iControlTeaser, ContentData content, ArticleWithBreadcrumbViewModel viewModel)
        {
            viewModel.TeaserTitle = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserTitle) ?
                                        iControlTeaser.TeaserBlock.TeaserTitle :
                                            (content is IControlPageHeader iControlPageHeading && !string.IsNullOrWhiteSpace(iControlPageHeading.Heading) ?
                                                iControlPageHeading.Heading : currentContent.Name);
            viewModel.TeaserDescription = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserDescription) ?
                                            iControlTeaser.TeaserBlock.TeaserDescription : string.Empty;
            viewModel.TeaserImageUrl = iControlTeaser.TeaserBlock.TeaserImage != null ? iControlTeaser.TeaserBlock.TeaserImage.GetExternalUrl_V2() : _amAssetHelper.GetTrmNoPictureImageUrl();
        }
        private void ContentNotFound(IContent currentContent, IControlTeaser iControlTeaser, ContentData content, ArticleWithBreadcrumbViewModel viewModel)
        {
            var displayName = content is EntryContentBase entryContentBase ?
                entryContentBase.DisplayName : (content is NodeContent nodeContent ? nodeContent.DisplayName : currentContent.Name);

            viewModel.TeaserTitle = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserTitle) ?
                                        iControlTeaser.TeaserBlock.TeaserTitle : displayName;
            viewModel.TeaserDescription = !string.IsNullOrWhiteSpace(iControlTeaser.TeaserBlock.TeaserDescription) ?
                                                iControlTeaser.TeaserBlock.TeaserDescription : string.Empty;
            viewModel.TeaserImageUrl = iControlTeaser.TeaserBlock.TeaserImage != null ? iControlTeaser.TeaserBlock.TeaserImage.GetExternalUrl_V2() : _amAssetHelper.GetTrmNoPictureImageUrl();
        }
        #endregion GetTeaser

        #region CreateBreadCrumb
        private List<BreadcrumbItem> GenerateBreadcrumbViewModel(IContent iContent, ITrmLayoutModel layoutModel)
        {
            var currentStartPage = layoutModel.StartPageReference;
            var icontrolTrmBreadcrumbDisplay = iContent as IControlTrmBreadcrumbDisplay;
            if ((icontrolTrmBreadcrumbDisplay != null && icontrolTrmBreadcrumbDisplay.HideSiteBreadcrumb) || currentStartPage == null) return null;

            var breadcrumbItems = new List<BreadcrumbItem> { GetBreadcrumbItem(iContent, true) };
            if (iContent is CatalogContentBase)
            {
                var parentContentReference = _trmEntryHelper.GetCategoryContentReference(iContent.ContentLink);
                if (parentContentReference != null)
                {
                    iContent.ContentLink = parentContentReference;
                    var parentContentBase = _contentLoader.Get<CatalogContentBase>(parentContentReference);
                    var parentDto = GetBreadcrumbItem(parentContentBase);
                    breadcrumbItems.Add(parentDto);
                }
                breadcrumbItems.AddRange(_contentLoader.GetAncestors(iContent.ContentLink)
                    .OfType<CatalogContentBase>()
                    .Where(c => c.ContentType != CatalogContentType.Catalog &&
                                c.ContentType != CatalogContentType.Root)
                    .Select(x => GetBreadcrumbItem(x)));
            }
            if (iContent is PageData)
            {
                breadcrumbItems.AddRange(_contentLoader.GetAncestors(iContent.ContentLink)
                    .OfType<PageData>()
                    .Where(c => c.ContentLink != currentStartPage && c.ContentLink.ID != 1 && c.VisibleInMenu)
                    .Select(x => GetBreadcrumbItem(x)));
            }
            var homeBreadCrumbItem = new BreadcrumbItem
            {
                MenuItemDisplayName = _localizationService.GetString(StringResources.BreadCrumbHomeText, TranslationFallback.Home),
                MenuItemExternalUrl = currentStartPage.GetExternalUrl_V2(),
                IsActiveContent = false,
                IsContainer = iContent is IContainerPage,
            };
            breadcrumbItems.Add(homeBreadCrumbItem);
            breadcrumbItems.Reverse();
            return breadcrumbItems;
        }
        private BreadcrumbItem GetBreadcrumbItem(IContent content, bool isActivePage = false)
        {
            return new BreadcrumbItem
            {
                MenuItemDisplayName = GetAutomaticTrmNavigationBlockDisplayName(content),
                MenuItemExternalUrl = content.ContentLink.GetExternalUrl_V2(),
                IsActiveContent = isActivePage,
                IsContainer = content is IContainerPage,
            };
        }
        private string GetAutomaticTrmNavigationBlockDisplayName(IContent iContent)
        {
            var displayName = string.Empty;
            if (iContent is EntryContentBase entryContent)
            {
                displayName = _trmEntryHelper.GetDisplayName(entryContent.ContentLink);
            }
            if (iContent is NodeContent nodeContent)
            {
                displayName = nodeContent.DisplayName;
            }
            if (iContent is PageData pageData)
            {
                displayName = pageData.Name;
            }
            return displayName;
        }
        #endregion CreateBreadCrumb
    }
}