using EPiServer;
using EPiServer.AddOns.Helpers;
using EPiServer.Core;
using EPiServer.Web;
using EPiServer.Web.Routing;
using Hephaestus.CMS.Business.Rendering;
using Hephaestus.CMS.ViewModels;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Find;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Find;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Layouts;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels;
using TRM.Web.Services;
using StringExtensions = TRM.Web.Extentions.StringExtensions;

namespace TRM.Web.Helpers
{
    public class TrmCategoryHelper : ITrmCategoryHelper
    {
        private readonly IAmTeaserHelper _teaserHelper;
        private readonly IAmLayoutHelper _layoutHelper;
        private readonly IAmNavigationHelper _navigationHelper;
        private readonly IContentRepository _contentRepository;
        private readonly IContentLoader _contentLoader;
        private readonly IFindService _findService;
        private readonly IFindHelper _findHelper;
        private readonly MiniProfiler _miniProfiler;

        private int _defaultResultsPerPage = 52;
        private int _cacheResultsInMinutes = 60;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly UrlResolver _urlResolver;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IUserService _userService;
        
        private readonly ITrmVariantHelper _trmVariantHelper;
        private readonly INotVisibleCategoriesHelper _notVisibleCategoriesHelper;

        public TrmCategoryHelper(
            IPageViewContextFactory contextFactory, 
            IAmTeaserHelper teaserHelper, 
            IAmLayoutHelper layoutHelper,
            IContentRepository contentRepository, 
            IContentLoader contentLoader, 
            IFindService findService,
            IFindHelper findHelper, 
            IAmNavigationHelper navigationHelper, 
            ISiteDefinitionResolver siteDefinitionResolver, 
            UrlResolver urlResolver, 
            IAmCurrencyHelper currencyHelper, 
            IAmBullionContactHelper bullionContactHelper, 
            IUserService userService,
            ITrmVariantHelper trmVariantHelper, INotVisibleCategoriesHelper notVisibleCategoriesHelper)
        {
            _teaserHelper = teaserHelper;
            _layoutHelper = layoutHelper;
            _navigationHelper = navigationHelper;
            _siteDefinitionResolver = siteDefinitionResolver;
            _urlResolver = urlResolver;
            _currencyHelper = currencyHelper;
            _bullionContactHelper = bullionContactHelper;
            _userService = userService;
            _contentLoader = contentLoader;
            _contentRepository = contentRepository;
            _findService = findService;
            _findHelper = findHelper;
            _trmVariantHelper = trmVariantHelper;
            _notVisibleCategoriesHelper = notVisibleCategoriesHelper;
            _miniProfiler = MiniProfiler.Current;
        }

        public IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> CreateViewModelForCategoryLandingPage(
            IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> model, 
            TrmCategory trmCategory, HttpRequestBase request)
        {
            // Generate model for Category Landing page
            var trmLayoutModel = model.Layout as TrmLayoutModel;
            if (trmLayoutModel == null) return model;

            trmLayoutModel.IsInvestmentPage = trmCategory.IsInvestmentPage;
            _navigationHelper.CreateBreadcrumb(trmLayoutModel, trmCategory);

            if (!trmLayoutModel.UseManualMegaMenu || trmLayoutModel.IsPensionContact) { _navigationHelper.CreateMegaMenu(trmLayoutModel); }

            _navigationHelper.CreateMyAccountHoverMenu(trmLayoutModel);
            var icontrolLeftNav = trmCategory as IControlLeftNav;
            if (!trmCategory.IsNewIn)
            {
                if (icontrolLeftNav.ShowMyLeftMenu && icontrolLeftNav.ShowAutomaticLeftNavigation)
                { _navigationHelper.CreateAutomaticLeftMenu(trmLayoutModel, trmCategory); }

                if (icontrolLeftNav.ShowMyLeftMenu &&
                    icontrolLeftNav.ShowManualLeftNavigation &&
                    icontrolLeftNav?.LeftMenuManualContentArea?.FilteredItems != null &&
                    icontrolLeftNav.LeftMenuManualContentArea.FilteredItems.Any())
                { _navigationHelper.CreateManualLeftMenu(trmLayoutModel, icontrolLeftNav); }
            }

            trmLayoutModel.Teaser = _teaserHelper.GetTeaserDto(trmCategory);
            if (trmLayoutModel.EnableCustomerCss)
            { trmLayoutModel.CustomerCssUrl = _layoutHelper.RetrieveGlobalCustomerStylesheet(); }

            model.Layout.CanonicalUrl = trmCategory.CanonicalUrl != null ?
               trmCategory.CanonicalUrl.GetContentReference()?
               .GetFriendlyCanonicalUrl(trmCategory.Language, _urlResolver, _siteDefinitionResolver, request) :
               trmCategory.GetCanonicalUrl(request, _siteDefinitionResolver, _urlResolver);

            _trmVariantHelper.ModifyLayout(trmLayoutModel, trmCategory);

            var startPage = _contentRepository.Get<StartPage>(SiteDefinition.Current.StartPage);
            trmLayoutModel.HideUserAndMiniBasketFromHeader = startPage.SiteWideHideUserAndMiniBasketFromHeader;

            return model;
            // End - Generate model for Category Landing page
        }
        public IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> CreateViewModelForCategoryListingPage(
            IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> model,
            TrmCategory trmCategory,
            CategoryViewModel viewModel, HttpRequestBase request)
        {
            var startPage = _contentRepository.Get<StartPage>(SiteDefinition.Current.StartPage);

            var resultsPerPage = GetCategoryListingResultsPerPage(trmCategory, startPage);

            if (startPage.CacheResultsForMinutes > 0) { _cacheResultsInMinutes = startPage.CacheResultsForMinutes; }

            var variantFacets = new List<IAddCommerceSearchFacets>();
            if (trmCategory.NewInCategoryRoot != null && trmCategory.IsNewIn)
            {
                var numberOfProducts = trmCategory.NumberOfProducts;
                var entriesNewInCategory = new FindResults<IAmCommerceSearchable>();
                var variantsNewInCategoryRoot = _findService.GetNewInItems(trmCategory.NewInCategoryRoot, numberOfProducts);
                entriesNewInCategory.Results.AddRange(variantsNewInCategoryRoot);
                var excludeStatuses = new[] { Enums.eStockStatus.NoLongerAvailable, Enums.eStockStatus.SoldOut };
                model.ViewModel.Entries = _findHelper.GetEntryViewModel(entriesNewInCategory, EntrySortOrder.SortBy, excludeStatuses);
                return model;
            }

            if (trmCategory.DisplayNonSellableView)
            {
                var nonSellableResults = new FindResults<IAmCommerceSearchable>();
                using (_miniProfiler.Step("Get Children"))
                {
                    var nonSellablevariants = _contentLoader.GetChildren<TrmVariant>(trmCategory.ContentLink).ToList();
                    nonSellableResults.Results.AddRange(nonSellablevariants);
                }
                model.ViewModel.Entries = _findHelper.GetEntryViewModel(nonSellableResults).OrderByDescending(r => r.YearOfIssue).ToList();
                return model;
            }

            model.ViewModel.CanSort = !trmCategory.SortByAndRefinements.HideSortOptions;
            model.ViewModel.CanFilter = !trmCategory.SortByAndRefinements.HideRefinements;

            if (trmCategory.SearchFiltersContentArea != null)
                variantFacets =
                    trmCategory.SearchFiltersContentArea.FilteredItems.Select(
                            a => _contentLoader.Get<IAddCommerceSearchFacets>(a.ContentLink)).Where(a => a != null).ToList();
            else if (startPage.DefaultFilters != null)
                variantFacets = startPage.DefaultFilters.FilteredItems.Select(
                        a => _contentLoader.Get<IAddCommerceSearchFacets>(a.ContentLink)).Where(a => a != null).ToList();

            //Commerce search first
            var filters = new FindFilters
            {
                SearchText = viewModel.Filters.Query,
                CatalogId = trmCategory.ContentLink.ID,
                MaxResults = resultsPerPage,
                EntrySortOrder = viewModel.Filters.SelectedEntrySortOrder,
                CacheResultsForMinutes = _cacheResultsInMinutes
            };

            // ReSharper disable once PossibleNullReferenceException
            var uri = new Uri(request.Url.AbsoluteUri);

            viewModel.Filters.CatalogId = trmCategory.ContentLink.ID;
            viewModel.Filters.CurrentPage = 1;
            viewModel.Filters.MaxResults = resultsPerPage;
            viewModel.Filters.HideFilterCounts = startPage.HideFilterCounts;

            var currentPage = trmCategory as IContent;
            viewModel.Filters.PageId = currentPage.ContentLink.ID;

            _notVisibleCategoriesHelper.AddHelperCategoriesStringFacet(variantFacets);

            viewModel.Filters.EntryFacets = variantFacets.Select(a => new FindFacet()
            {
                Name = a.Name,
                Value = a.Term,
                ShowExpanded = a.ShowExpanded,
                FacetType = a.FacetType,
                Description = (a as TrmFacetBlock)?.Description,
                ViewAllLink = (a as TrmFacetBlock)?.ViewAllLink
            }).ToList();

            viewModel.Filters.SelectedEntryFilters = new List<QueryStringFilter>();
            var queryStrings = HttpUtility.ParseQueryString(uri.Query);

            foreach (var key in queryStrings.AllKeys)
            {
                var myVariantFacet =
                    variantFacets.FirstOrDefault(
                        vF => StringExtensions.EncodeValue(vF.Term) == StringExtensions.EncodeValue(key));
                if (key == "q" || myVariantFacet == null) { continue; }
                var values = queryStrings.GetValues(key);
                if (values == null) continue;
                foreach (var val in values)
                {
                    var filterToAdd = new QueryStringFilter
                    {
                        Key = myVariantFacet.Term,
                        EncodedValue = StringExtensions.EncodeValue(val)
                    };
                    viewModel.Filters.SelectedEntryFilters.Add(filterToAdd);
                }
            }

            var variantResults = _findService.SearchCommerce(filters, viewModel.Filters.SelectedEntryFilters, variantFacets);

            _notVisibleCategoriesHelper.CleanupAllCategoriesIdsFacet(viewModel);
            var categoriesToExcludeFromFilters = _notVisibleCategoriesHelper.GetCategoriesNotVisibleInMenu(variantResults);

            foreach (var facet in variantResults.Facets)
            {
                if (facet == null) continue;
                var entryFacet = viewModel.Filters.EntryFacets.SingleOrDefault(a => a.EncodedValue == facet.EncodedValue);
                if (entryFacet != null)
                {
                    if (facet.Value == "Category")
                    {
                        var entryFacetTerms = facet.Terms
                            .Where(x => x.EncodedTerm != StringExtensions.EncodeValue(trmCategory.DisplayName))
                            .Where(x=>  categoriesToExcludeFromFilters.Contains(x.EncodedTerm) == false)
                            .ToList();

                        entryFacet.Terms = entryFacetTerms;
                    }
                    else
                    {
                        entryFacet.Terms = facet.Terms;
                    }
                }
            }
            var variants = _findHelper.GetEntryViewModel(variantResults, filters.EntrySortOrder).ToList();

            model.ViewModel.Filters.ShowMoreFiltersCount = startPage.ShowMoreFiltersCount > 0 ? startPage.ShowMoreFiltersCount : 5;
            model.ViewModel.Entries = variants;

            int totalItems = GetTotalItemsForLoadMore(trmCategory, resultsPerPage, variantResults);

            model.ViewModel.TotalEntryCount = totalItems;

            model.ViewModel.SearchPageHeader = new SearchPageHeader(model.ViewModel.Filters)
            {
                TotalEntryPages = 0,
                TotalArticlePages = 0
            };

            if (totalItems <= 0) return model;

            var numberOfEntryPages = Convert.ToDecimal(totalItems) / variantResults.ItemsPerPage;
            model.ViewModel.SearchPageHeader.TotalEntryPages = Convert.ToInt32(Math.Ceiling(numberOfEntryPages));
            return model;
        }

        private static int GetTotalItemsForLoadMore(TrmCategory trmCategory, int resultsPerPage, FindResults<IAmCommerceSearchable> variantResults)
        {
            //Send only first page so load more is hidden
            if (trmCategory.DisableLoadMoreBtn)
            {
                return Math.Min(resultsPerPage, variantResults.TotalItems);
            }
            
            return variantResults.TotalItems;
        }

        private int GetCategoryListingResultsPerPage(TrmCategory trmCategory, StartPage startPage)
        {
            var fallbackValue = startPage?.ListingResultsPerPage > 0 ? startPage.ListingResultsPerPage : _defaultResultsPerPage;

            return trmCategory.MaxNoOfProducts > 0 ? trmCategory.MaxNoOfProducts : fallbackValue;
        }
    }
}