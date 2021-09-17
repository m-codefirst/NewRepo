using System;
using System.Collections.Generic;
using System.Linq;
using AjaxControlToolkit;
using Castle.Core.Internal;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.Api.Facets;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Api.Querying.Queries;
using EPiServer.Find.Cms;
using EPiServer.Find.Commerce;
using EPiServer.Find.Framework.BestBets;
using EPiServer.Find.Framework.Statistics;
using EPiServer.Find.Helpers;
using EPiServer.Find.Helpers.Text;
using EPiServer.Find.Statistics;
using EPiServer.Web;
using Hephaestus.ContentTypes.Models.Interfaces;
using log4net;
using StackExchange.Profiling;
using TRM.Web.Business.DataAccess;
using TRM.Web.Extentions;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.Find;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.GBCH;

namespace TRM.Web.Helpers.Find
{
    public class FindService : IFindService
    {
        private const int GbchSearchMaxResult = 1000;
        private readonly IClient _findClient;
        private readonly IFindHelper _findHelper;
        private readonly IAmMarketHelper _marketHelper;
        private readonly MiniProfiler _miniProfiler;
        private readonly IGiftingHelper _giftingHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IAmLocalPriceDataHelper _localPriceDataHelper;
        private readonly IBestBetsFindHelper _bestBetsFindHelper;


        private readonly ILog _logger = LogManager.GetLogger(typeof(FindService));


        public FindService(
            IClient findClient,
            IFindHelper findHelper,
            IAmMarketHelper marketHelper,
            IGiftingHelper giftingHelper,
            IContentLoader contentLoader,
            IAmLocalPriceDataHelper localPriceDataHelper, 
            IBestBetsFindHelper bestBetsFindHelper)
        {
            _findClient = findClient;
            _findHelper = findHelper;
            _marketHelper = marketHelper;
            _giftingHelper = giftingHelper;
            _contentLoader = contentLoader;
            _localPriceDataHelper = localPriceDataHelper;
            _bestBetsFindHelper = bestBetsFindHelper;
            _miniProfiler = MiniProfiler.Current;

        }

        public FindResults<IAmCommerceSearchable> SearchCommerce(FindFilters searchQuery, List<QueryStringFilter> queryFilters, List<IAddCommerceSearchFacets> facets)
        {
            try
            {
                var entrySearch = _findClient.Search<IAmCommerceSearchable>();

                var useAndAsDefaultOperator = false;
                var startPageContentLink = SiteDefinition.Current.StartPage;
                if (startPageContentLink != null && startPageContentLink.ID > 0)
                {
                    var startPage = _contentLoader.Get<StartPage>(startPageContentLink);

                    if (startPage.IgnoreCommerceInSearch)
                    {
                        return new FindResults<IAmCommerceSearchable>();
                    }

                    useAndAsDefaultOperator = startPage.UseAndAsDefaultOperator;
                }

                if (useAndAsDefaultOperator)
                {
                    entrySearch = entrySearch.For(searchQuery.SearchText)
                        .WithAndAsDefaultOperator()
                        .InField(x => (x as TrmVariant).DisplayName)
                        .InField(x => (x as TrmVariant).MetaKeywords)
                        .AndInField(x => (x as TrmVariant).Code)
                        .UsingSynonyms()
                        .ExcludeDeleted()
                        .FilterOnReadAccess()
                        .PublishedInCurrentLanguage(DateTime.Today.AddDays(1))
                        .FilterOnSite(SiteDefinition.Current.Id)
                        .ApplyBestBets();
                }
                else
                {
                    entrySearch = entrySearch.For(searchQuery.SearchText)
                        .InField(x => (x as TrmVariant).DisplayName)
                        .InField(x => (x as TrmVariant).MetaKeywords)
                        .AndInField(x => (x as TrmVariant).Code)
                        .UsingSynonyms()
                        .ExcludeDeleted()
                        .FilterOnReadAccess()
                        .PublishedInCurrentLanguage(DateTime.Today.AddDays(1))
                        .FilterOnSite(SiteDefinition.Current.Id)
                        .ApplyBestBets();
                }

                if (!string.IsNullOrEmpty(searchQuery.SearchText.Trim()))
                {
                    entrySearch = entrySearch.Track()
                        .StatisticsTrack();
                }

                var bestBets = _bestBetsFindHelper.GetBestBets(searchQuery.SearchText.Trim());
                entrySearch = BestBetsFilter(bestBets, entrySearch);

                var merchandisingFilter = MerchandisingFilter();
                entrySearch = entrySearch.Filter(merchandisingFilter).FilterOnCurrentSite();
                if (searchQuery.TypeFilter == ProductTypeFilter.Shop)
                {
                    entrySearch = entrySearch.Filter(a => a.MatchTypeHierarchy(typeof(EntryContentBase)))
                        .Filter(a => !a.MatchTypeHierarchy(typeof(PreciousMetalsVariantBase)));
                }
                if (searchQuery.TypeFilter == ProductTypeFilter.Invest)
                {
                    entrySearch = entrySearch.Filter(a => a.MatchTypeHierarchy(typeof(PreciousMetalsVariantBase)));
                }

                if (searchQuery.CatalogId > 0)
                {
                    var categoryFilter = _findClient.BuildFilter<IAmCommerceSearchable>();

                    categoryFilter = categoryFilter.And(a => a.AllCategoryIds.Match(searchQuery.CatalogId));

                    entrySearch = entrySearch.Filter(categoryFilter);
                }

                //Apply facets to the  search
                foreach (var facet in facets.Where(f => f.FacetType == FacetType.Term))
                {
                    if (string.IsNullOrEmpty(facet.Term)) continue;

                    entrySearch = _findHelper.AddCommerceFacet(entrySearch, facet);

                }

                foreach (var rangefacet in facets.Where(f => f.FacetType == FacetType.Range))
                {
                    if (string.IsNullOrEmpty(rangefacet.Term)) continue;
                    entrySearch = _findHelper.AddCommerceFacet(entrySearch, rangefacet);

                }

                if (queryFilters != null)
                {
                    var groups = queryFilters.GroupBy(f => f.Key);
                    foreach (var group in groups)
                    {
                        var variantFilterBuilder = _findClient.BuildFilter<IAmCommerceSearchable>();
                        foreach (var filter in group)
                        {
                            var facet = facets.FirstOrDefault(f => f.Term == filter.Key);
                            if (facet != null)
                                variantFilterBuilder = facet.FacetType == FacetType.Term ? filter.AddTermFilter(variantFilterBuilder) : filter.AddRangeFilter(variantFilterBuilder);
                        }
                        entrySearch = entrySearch.Filter(variantFilterBuilder);
                    }
                }

                var priceRef = _localPriceDataHelper.GetPriceReference();
                switch (searchQuery.EntrySortOrder)
                {
                    case EntrySortOrder.AZ:
                        entrySearch = entrySearch.OrderBy(a => a.DisplayNameForSort, SortMissing.Last);
                        break;
                    case EntrySortOrder.ZA:
                        entrySearch = entrySearch.OrderByDescending(a => a.DisplayNameForSort, SortMissing.Last);
                        break;
                    case EntrySortOrder.HL:
                        entrySearch = entrySearch.OrderByDescending(v => v.ProductPrices[priceRef], SortMissing.Last);
                        break;
                    case EntrySortOrder.LH:
                        entrySearch = entrySearch.OrderBy(v => v.ProductPrices[priceRef], SortMissing.Last);
                        break;
                    case EntrySortOrder.PublishDate:
                        entrySearch = entrySearch.OrderByDescending(v => v.ReleaseDate, SortMissing.Last);
                        break;
                    case EntrySortOrder.SortBy:
                        //No applied sorting - Items are automatically sorted by relevance
                        entrySearch = entrySearch
                            .OrderBy(o => (o as IStockStatus).StockStatusForOrder, SortMissing.Last); //Title

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                //Execute the query
                var baseSearch = entrySearch;
                var search = _findClient.MultiSearch<IAmCommerceSearchable>()
                    .Search(
                        a =>
                            baseSearch.Take(1)
                            .StaticallyCacheFor(TimeSpan.FromMinutes(searchQuery.CacheResultsForMinutes))
                    )
                    .Search(
                        a =>
                            entrySearch.Skip((searchQuery.CurrentPage - 1) * searchQuery.MaxResults)
                            .Take(searchQuery.MaxResults)
                            .StaticallyCacheFor(TimeSpan.FromMinutes(searchQuery.CacheResultsForMinutes))
                    );

                List<IContentResult<IAmCommerceSearchable>> searchResults;
                using (_miniProfiler.Step("Commerce Search"))
                {
                    searchResults = search.StaticallyCacheFor(TimeSpan.FromMinutes(searchQuery.CacheResultsForMinutes))
                        .GetContentResult((int)TimeSpan.FromMinutes(searchQuery.CacheResultsForMinutes).TotalSeconds).ToList();

                }

                var baseResults = searchResults[0];
                var bestBetsForCurrentType = _bestBetsFindHelper.GetBestBetsForType(bestBets, searchQuery.TypeFilter);
                var filterResults =  _bestBetsFindHelper.GetResultsWithBestBets(searchResults[1], bestBetsForCurrentType);

                var facetsToProcessForCounts =
                    facets.Where(facet => !string.IsNullOrEmpty(facet.Term))
                        .ToList();

                var resultFacets = facetsToProcessForCounts.Select(a => _findHelper.ProcessFacet(baseResults, filterResults, a, queryFilters));
                var searchResultFacets = resultFacets.Where(a => a != null).ToList();
                var results = new FindResults<IAmCommerceSearchable>
                {
                    Results = filterResults.ToList(),
                    CurrentPage = searchQuery.CurrentPage,
                    ItemsPerPage = searchQuery.MaxResults,
                    TotalItems = filterResults.TotalMatching + bestBetsForCurrentType.Count,
                    Facets = searchResultFacets,
                    BaseTotalItems = baseResults.TotalMatching + bestBetsForCurrentType.Count
                };

                return results;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Failed contacting Find Service for query {0}", searchQuery.SearchText), e);

                return new FindResults<IAmCommerceSearchable>();
            }
        }

        private FilterBuilder<IAmCommerceSearchable> MerchandisingFilter()
        {
            //Filter out withdrawn and publishOntoSite items
            var merchandisingFilter = _findClient.BuildFilter<IAmCommerceSearchable>();
            merchandisingFilter = merchandisingFilter.And(a => a.Restricted.Match(false));
            merchandisingFilter = merchandisingFilter.And(a => a.PublishOntoSite.Match(true));
            merchandisingFilter = merchandisingFilter.And(a => a.Sellable.Match(true));
            merchandisingFilter = merchandisingFilter.And(a => a.ShouldBeOmittedFromSearchResults.Match(false));
            merchandisingFilter = merchandisingFilter.And(a => !a.MatchTypeHierarchy(typeof(VirtualVariantBase)));
            return merchandisingFilter;
        }
        private List<int> GetCategoryIds(ContentReference contentReference)
        {
            var results = new List<int>();
            var cats = _contentLoader.GetChildren<TrmCategory>(contentReference).Select(x => x.ContentLink);
            foreach (var cat in cats)
            {
                results.Add(cat.ID);
                results.AddRange(GetCategoryIds(cat));
            }
            return results;
        }
        public IEnumerable<TrmVariant> GetNewInItems(ContentReference categoryRef, int numberOfProducts)
        {
            if (categoryRef == null || categoryRef == ContentReference.EmptyReference) return new List<TrmVariant>();
            try
            {
                var categories = GetCategoryIds(categoryRef);
                categories.Add(categoryRef.ID);
                categories = categories.Distinct().ToList();
                //Build the search & filter objects
                var entrySearch = _findClient.Search<TrmVariant>();
                entrySearch = entrySearch
                    .ExcludeDeleted()
                    .FilterOnReadAccess()
                    .PublishedInCurrentLanguage()
                    .FilterOnSite(SiteDefinition.Current.Id);

                // Node And custom category
                entrySearch = entrySearch
                    .Filter(x => x.AllCategoryIds.In(categories));
                var categorySearch = entrySearch.OrderByDescending(p => p.ReleaseDate).Take(numberOfProducts).GetContentResult().Select(p => p);
                return categorySearch;
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Failed contacting Find Service for query GetNewInItems {0}", categoryRef), e);
                return new List<TrmVariant>();
            }
        }
        public GbchCategoryViewModel GetGBCHSubCategories(GBCHCategory currentContent)
        {
            if (currentContent == null) return new GbchCategoryViewModel();
            try
            {
                //Build the search & filter objects
                var entrySearch = _findClient.Search<GBCHCategory>();

                //Build search for the provided text
                //Filter results for visitors
                //Apply best bets
                entrySearch = entrySearch
                    .ExcludeDeleted()
                    .FilterOnReadAccess()
                    .PublishedInCurrentLanguage()
                    .FilterOnSite(SiteDefinition.Current.Id)
                    .ApplyBestBets()
                    .Track()
                    .StatisticsTrack();

                // Node And custom category
                entrySearch = entrySearch
                    .Filter(x => x.ContentLink.ID.Match(currentContent.ContentLink.ID)
                                 | x.Ancestors().Match(currentContent.ContentLink.ToReferenceWithoutVersion()
                                     .ToString()));

                var categorySearch = entrySearch.OrderByDescending(p => p.IsDefaultCategory).ThenBy(p => p.Created).Select(p => new GbchCategoryViewModel
                {
                    Id = p.ContentLink.ID,
                    ParentId = p.ParentLink.ID,
                    DisplayName = p.DisplayName,
                    Description = p.GBCHCategoryDescription,
                    DefaultImageUrl = p.DefaultImageUrl()
                });

                var searchResult = categorySearch.GetResult();

                var categories = searchResult.Hits.Select(sh => sh.Document).ToList();
                var variants = GetVariantsOfGbchCategory(currentContent).ToList();

                foreach (var gbchCategoryViewModel in categories)
                {
                    gbchCategoryViewModel.SubCategories.AddRange(categories.Where(x => x.ParentId == gbchCategoryViewModel.Id));
                    gbchCategoryViewModel.Variants.AddRange(variants.Where(v => v.ParentId == gbchCategoryViewModel.Id));
                }

                return categories.FindLast(c => c.Id == currentContent.ContentLink.ID);
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Failed contacting Find Service for query GetGBCHSubCategories {0}", currentContent.ContentLink.ID), e);

                return new GbchCategoryViewModel();
            }
        }

        public IEnumerable<GbchVariantViewModel> GetRelatedVariantsByLetter(int contentId, string letters = "")
        {
            try
            {
                var query = letters.ToLetterStandard();

                //Build the search & filter objects
                var entrySearch = BuildBasicSearchForVariant();

                if (!string.IsNullOrEmpty(query))
                {
                    entrySearch = entrySearch.Filter(x => x.Letter.In(query.Select(c => c.ToString()), true));
                }

                entrySearch = entrySearch.Filter(x => x.ParentLink.ID.Match(contentId));

                var variantSearch = entrySearch.Select(variant => new GbchVariantViewModel
                {
                    Id = variant.ContentLink.ID,
                    Code = variant.Code,
                    Letter = variant.Letter,
                    DefaultImageUrl = variant.GetDefaultAssetUrl(),
                    DisplayName = variant.Name,
                    Description = variant.DescriptionForProductFeed,
                    Price = variant.DisplayPrice(),
                    ParentId = variant.ParentLink.ID,
                    RelatedEntries = variant.GetAssociatedReferences(),
                });

                var searchResult = variantSearch.Take(GbchSearchMaxResult).GetResult();

                var relatedEntryIds = searchResult.Hits.SelectMany(sh => sh.Document.RelatedEntries).Distinct().ToList();

                if (relatedEntryIds.Any())
                {
                    var relatedSearch = BuildBasicSearchForVariant();
                    relatedSearch = relatedSearch.Filter(x => x.ContentLink.ID.In(relatedEntryIds));

                    var relatedVariantSearch = relatedSearch.Select(variant => new GbchVariantViewModel
                    {
                        Id = variant.ContentLink.ID,
                        Code = variant.Code,
                        Letter = variant.Letter,
                        DefaultImageUrl = variant.GetDefaultAssetUrl(),
                        DisplayName = variant.Name,
                        Description = variant.DescriptionForProductFeed,
                        Price = variant.DisplayPrice(),
                        ParentId = variant.ParentLink.ID,
                        RelatedEntries = variant.GetAssociatedReferences(),
                        MaxQuantity = variant.MaxQuantity
                    });

                    var relatedVariants = relatedVariantSearch.Take(GbchSearchMaxResult).GetResult();

                    return relatedVariants.Hits.Select(sh => sh.Document);
                }

                return new List<GbchVariantViewModel>();
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Failed contacting Find Service for query GetRelatedVariantsByLetter {0}", contentId), e);

                return new List<GbchVariantViewModel>();
            }
        }

        private IEnumerable<GbchVariantViewModel> GetVariantsOfGbchCategory(GBCHCategory currentContent)
        {
            if (currentContent == null) return new List<GbchVariantViewModel>();
            try
            {
                //Build the search & filter objects
                var entrySearch = BuildBasicSearchForVariant();

                // Node And custom category
                entrySearch = entrySearch.Filter(x => x.Ancestors().Match(currentContent.ContentLink.ToReferenceWithoutVersion().ToString()));

                var variantSearch = entrySearch.Select(variant => new GbchVariantViewModel
                {
                    Id = variant.ContentLink.ID,
                    Code = variant.Code,
                    Letter = variant.Letter,
                    DefaultImageUrl = variant.GetDefaultAssetUrl() + "?width=70",
                    DisplayName = variant.Name,
                    Description = variant.DescriptionForProductFeed,
                    Price = variant.DisplayPrice(),
                    ParentId = variant.ParentLink.ID,
                    RelatedEntries = variant.GetAssociatedReferences()
                });

                var searchResult = variantSearch.Take(GbchSearchMaxResult).GetResult();

                return searchResult.Hits.Select(sh => sh.Document);
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Failed contacting Find Service for query GetVariantsOfGBCHCategory {0}", currentContent.ContentLink.ID), e);

                return new List<GbchVariantViewModel>();
            }
        }

        public IEnumerable<T> GetAllContents<T>() where T : IContent
        {
            var maximumItemsSearchedByFind = 1000;
            var currentPage = 1;
            bool reachAllItems = false;
            var result = new List<T>();
            while (!reachAllItems)
            {
                var search = _findClient.Search<T>()
                .ExcludeDeleted()
                .FilterOnSite(SiteDefinition.Current.Id)
                .ApplyBestBets()
                .Skip((currentPage - 1) * maximumItemsSearchedByFind)
                .Take(maximumItemsSearchedByFind)
                .GetContentResult();
                var totalItems = search.TotalMatching;
                reachAllItems = currentPage * maximumItemsSearchedByFind >= totalItems;
                result.AddRange(search);
            }

            return result;
        }

        private ITypeSearch<TrmVariant> BuildBasicSearchForVariant()
        {
            var merchandisingFilter = _findClient.BuildFilter<TrmVariant>();

            //Filter out withdrawn and publishOntoSite and non sellable items
            merchandisingFilter = merchandisingFilter.And(a => a.Restricted.Match(false));
            merchandisingFilter = merchandisingFilter.And(a => a.PublishOntoSite.Match(true));
            merchandisingFilter = merchandisingFilter.And(a => a.Sellable.Match(true));

            return _findClient.Search<TrmVariant>()
            .ExcludeDeleted()
            .FilterOnReadAccess()
            .PublishedInCurrentLanguage()
            .FilterOnSite(SiteDefinition.Current.Id)
            .ApplyBestBets()
            .Track()
            .StatisticsTrack()
            .Filter(merchandisingFilter);
        }

        public SearchSuggesstionResultModel GetSearchSuggestionResult(string searchTerm, int maxResults = 10, int currentPage = 1, int cacheResultsForMinutes = 0, bool ignoreCommerce = false, bool ignoreDefaultOperator = false)
        {
            var bestBets = _bestBetsFindHelper.GetBestBets(searchTerm);

            // multisearch in the order: Variants--> Helpers --> CmsContents
            var multiSearch = BuildMultiSearch(searchTerm, maxResults, currentPage, cacheResultsForMinutes, ignoreCommerce, ignoreDefaultOperator, false, bestBets);
            var totalSearches = multiSearch.Searches.Count();

            if (totalSearches < 2) throw new Exception("Search Suggestion has to perform on at least two types of content, Helpers and CmsContents.");

            List<IContentResult<IContentData>> searchResult;
            try
            {
                searchResult = multiSearch.GetContentResult((int)TimeSpan.FromMinutes(cacheResultsForMinutes).TotalSeconds).ToList();
            }
            catch (Exception)
            {
                multiSearch = BuildMultiSearch(searchTerm, maxResults, currentPage, cacheResultsForMinutes, ignoreCommerce, ignoreDefaultOperator, true, bestBets);
                totalSearches = multiSearch.Searches.Count();

                if (totalSearches < 2) throw new Exception("Search Suggestion has to perform on at least two types of content, Helpers and CmsContents.");
                searchResult = multiSearch.GetContentResult((int)TimeSpan.FromMinutes(cacheResultsForMinutes).TotalSeconds).ToList();
            }
            var result = new SearchSuggesstionResultModel();
            if (!ignoreCommerce)
            {
                result.Variants = _bestBetsFindHelper.GetResultsWithBestBets(searchResult?[0], bestBets.Shop, currentPage);
            }

            result.Articles = _bestBetsFindHelper.GetResultsWithBestBets(searchResult?[totalSearches - 1], bestBets.Articles, currentPage);
            result.HelpPages = _bestBetsFindHelper.GetResultsWithBestBets(searchResult?[totalSearches - 2], bestBets.HelpPages, currentPage);

            result.Counts = _bestBetsFindHelper.GetTotalCounts(result, bestBets);

            return result;
        }

        private IMultiSearch<IContentData> BuildMultiSearch(string searchTerm, int maxResults, int currentPage, int cacheResultsForMinutes,
            bool ignoreCommerce, bool ignoreDefaultOperator = false, bool ignoreStockStatus = false, BestBetsResults bestBets = null)
        {
            var search = _findClient.Search<IContentData>();
            var useAndAsDefaultOperator = false;
            var startPageContentLink = SiteDefinition.Current.StartPage;
            if (startPageContentLink != null && startPageContentLink.ID > 0)
            {
                var startPage = _contentLoader.Get<StartPage>(startPageContentLink);
                useAndAsDefaultOperator = startPage.UseAndAsDefaultOperator;
            }

            if (!ignoreDefaultOperator && useAndAsDefaultOperator)
            {
                search = search.For(searchTerm)
                    .WithAndAsDefaultOperator()
                    .InField(x => (x as TrmVariant).DisplayName)
                    .InField(x => (x as TrmVariant).MetaKeywords)
                    .AndInField(x => (x as TrmVariant).Code)
                    .UsingSynonyms()
                    .ExcludeDeleted()
                    .FilterOnReadAccess()
                    .PublishedInCurrentLanguage(DateTime.UtcNow)
                    .FilterOnSite(SiteDefinition.Current.Id)
                    .StaticallyCacheFor(TimeSpan.FromMinutes(cacheResultsForMinutes))
                    .ApplyBestBets();
            }
            else
            {
                search = search.For(searchTerm)
                    .InField(x => (x as TrmVariant).DisplayName)
                    .InField(x => (x as TrmVariant).MetaKeywords)
                    .AndInField(x => (x as TrmVariant).Code)
                    .UsingSynonyms()
                    .ExcludeDeleted()
                    .FilterOnReadAccess()
                    .PublishedInCurrentLanguage(DateTime.UtcNow)
                    .FilterOnSite(SiteDefinition.Current.Id)
                    .StaticallyCacheFor(TimeSpan.FromMinutes(cacheResultsForMinutes))
                    .ApplyBestBets();
            }

            if (ignoreCommerce && startPageContentLink != null)
            {
                search = search.Filter(x =>
                    ((IContent)x).Ancestors().Match(startPageContentLink
                        .ToReferenceWithoutVersion().ToString()));
            }

            BestBetsFilter(bestBets, search);
            
            var entryResults = search.Take(maxResults).FilterOnCurrentSite();

            var searchResultsFilter = _findClient.BuildFilter<IAmContentSearchable>();
            searchResultsFilter = searchResultsFilter.And(s => s.ShowInArticleResults.Match(true));

            if (currentPage > 1)
            {
                entryResults = entryResults.Skip((currentPage - 1) * maxResults);
            }
            ITypeSearch<IContentData> commerceEntryFilter = null;
            if (!ignoreCommerce)
            {
                var merchandisingFilter = MerchandisingFilter();
                commerceEntryFilter = entryResults.Filter(a => a.MatchTypeHierarchy(typeof(EntryContentBase))).Filter(merchandisingFilter);
            }
            var help = entryResults.Filter(a => a.MatchType(typeof(HelpPage)));
            var articles = entryResults.Filter(a => a.MatchTypeHierarchy(typeof(IAmContentSearchable))).Filter(searchResultsFilter);

            if (ignoreCommerce)
            {
                articles = articles.Filter(a => !a.MatchTypeHierarchy(typeof(TrmCategoryBase)));
            }

            help = OrderByRating(help, true, ignoreStockStatus);
            articles = OrderByRating(articles, true, ignoreStockStatus);

            return commerceEntryFilter != null
                ? _findClient.MultiSearch<IContentData>().Search(a => commerceEntryFilter).Search(a => help).Search(a => articles)
                : _findClient.MultiSearch<IContentData>().Search(a => help).Search(a => articles);
        }

        public SearchSuggesstionExtendedResultModel GetExtendedSuggestionResult(string searchTerm, IEnumerable<IAddCommerceSearchFacets> facets, int maxResults = 3,
            int cacheResultsForMinutes = 0, bool ignoreCommerce = false,
            bool useAndAsDefaultOperator = false, string visitorLocation = "")
        {
            var bestBets = _bestBetsFindHelper.GetBestBets(searchTerm);

            var multiSearch = BuildExtendedMultiSearch(searchTerm, maxResults, cacheResultsForMinutes, ignoreCommerce, useAndAsDefaultOperator, facets, false, visitorLocation, bestBets: bestBets);
            var totalSearches = multiSearch.Searches.Count();

            if (totalSearches < 2) throw new Exception("Search Suggestion has to perform on at least two types of content, Helpers and CmsContents.");

            List<IContentResult<IContentData>> searchResult;
            try
            {
                searchResult = multiSearch.GetContentResult((int)TimeSpan.FromMinutes(cacheResultsForMinutes).TotalSeconds).ToList();
            }
            catch (Exception)
            {
                multiSearch = BuildExtendedMultiSearch(searchTerm, maxResults, cacheResultsForMinutes, ignoreCommerce, useAndAsDefaultOperator, facets, true, visitorLocation);
                totalSearches = multiSearch.Searches.Count();

                if (totalSearches < 2) throw new Exception("Search Suggestion has to perform on at least two types of content, Helpers and CmsContents.");
                searchResult = multiSearch.GetContentResult((int)TimeSpan.FromMinutes(cacheResultsForMinutes).TotalSeconds).ToList();
            }

            var result = new SearchSuggesstionExtendedResultModel();
            if (totalSearches == 4)
            {
                result.Shop = _bestBetsFindHelper.GetResultsWithBestBets(searchResult?[0], bestBets.Shop);
                result.Invest = _bestBetsFindHelper.GetResultsWithBestBets(searchResult?[1], bestBets.Invest);
            }
            result.HelpPages = _bestBetsFindHelper.GetResultsWithBestBets(searchResult?[totalSearches - 2], bestBets.HelpPages);
            result.Articles =  _bestBetsFindHelper.GetResultsWithBestBets(searchResult?[totalSearches - 1], bestBets.Articles);

            result.Counts = _bestBetsFindHelper.GetTotalCounts(result, bestBets);
            return result;
        }

        private IMultiSearch<IContentData> BuildExtendedMultiSearch(string searchTerm, int count, int cacheResultsForMinutes,
            bool ignoreCommerce, bool useAndAsDefaultOperator, IEnumerable<IAddCommerceSearchFacets> facets, bool ignoreStockStatus = false, string visitorLocation = "", BestBetsResults bestBets = null)
        {
            var query = _findClient.Search<IContentData>().For(searchTerm);

            if (useAndAsDefaultOperator)
            {
                query = query.WithAndAsDefaultOperator();
            }

            var search = query
                .InField(x => (x as TrmVariant).DisplayName)
                .InField(x => (x as TrmVariant).MetaKeywords)
                .AndInField(x => (x as TrmVariant).Code)
                .UsingSynonyms()
                .ExcludeDeleted()
                .FilterOnReadAccess()
                .PublishedInCurrentLanguage(DateTime.UtcNow)
                .FilterOnSite(SiteDefinition.Current.Id)
                .StaticallyCacheFor(TimeSpan.FromMinutes(cacheResultsForMinutes))
                .ApplyBestBets()
                .FilterOnCurrentSite();

            search = BestBetsFilter(bestBets, search);

            if (!visitorLocation.IsNullOrWhiteSpace())
            {
                search = search.Filter(x => !((TrmVariationBase) x).RestrictedCountriesArray.MatchCaseInsensitive(visitorLocation));
            }

            var articlesFilter = _findClient.BuildFilter<IAmContentSearchable>()
                                        .And(s => s.ShowInArticleResults.Match(true));

            var help = search.Filter(a => a.MatchType(typeof(HelpPage)));
            var articles = search.Filter(a => a.MatchTypeHierarchy(typeof(IAmContentSearchable))).Filter(articlesFilter);


            if (ignoreCommerce)
            {
                articles = articles.Filter(a => !a.MatchTypeHierarchy(typeof(TrmCategoryBase)));
                articles = OrderByRating(articles, true);

                return _findClient.MultiSearch<IContentData>()
                    .Search(a => help)
                    .Search(a => articles);
            }

            var merchandisingFilter = MerchandisingFilter();

            var shop = search.Filter(a => a.MatchTypeHierarchy(typeof(EntryContentBase)))
                                .Filter(a => !a.MatchTypeHierarchy(typeof(PreciousMetalsVariantBase)))
                                .Filter(merchandisingFilter);

            var invest = search.Filter(a => a.MatchTypeHierarchy(typeof(PreciousMetalsVariantBase)))
                                .Filter(merchandisingFilter);


            help = help.Take(bestBets?.HelpPages.Count <= count ? count - bestBets.Shop.Count : 0);
            articles = articles.Take(bestBets?.Articles.Count <= count ? count - bestBets.Shop.Count : 0);
            shop = shop.Take(bestBets?.Shop.Count <= count ? count - bestBets.Shop.Count : 0);
            invest = invest.Take(bestBets?.Invest.Count <= count ? count - bestBets.Shop.Count : 0);
            
            shop = OrderByRating(shop, true);
            invest = OrderByRating(invest, true);

            foreach (var facet in facets.Where(f => f.FacetType == FacetType.Term))
            {
                if (string.IsNullOrEmpty(facet.Term)) continue;

                shop = AddCommerceFacet(shop, facet);
                invest = AddCommerceFacet(invest, facet);
            }

            foreach (var rangefacet in facets.Where(f => f.FacetType == FacetType.Range))
            {
                if (string.IsNullOrEmpty(rangefacet.Term)) continue;

                shop = AddCommerceFacet(shop, rangefacet);
                invest = AddCommerceFacet(invest, rangefacet);
            }

            return _findClient.MultiSearch<IContentData>()
                .Search(a => shop)
                .Search(a => invest)
                .Search(a => help)
                .Search(a => articles);
        }

        private ITypeSearch<T> BestBetsFilter<T>(BestBetsResults bestBets, ITypeSearch<T> search) where T : IContentData
        {
            if (bestBets?.All == null || !bestBets.All.Any())
            {
                return search;
            }

            var bestBetsFilter = _findClient.BuildFilter<IContentData>();
            foreach (var bestBet in bestBets.All)
            {
                bestBetsFilter = bestBetsFilter.And(x => !((IContent) x).ContentLink.Match(bestBet.ContentLink));
            }

            search = search.Filter(bestBetsFilter);
            return search;
        }

        private ITypeSearch<IContentData> OrderByRating(ITypeSearch<IContentData> search, bool isOrderByDescending, bool ignoreStockStatus = false)
        {
            var priceRef = _localPriceDataHelper.GetPriceReference();
            if (ignoreStockStatus)
            {
                return !isOrderByDescending ?
                   search
                       .ThenBy(o => (o as IRating).Rating, SortMissing.Last)
                       .ThenBy(o => (o as IAmCommerceSearchable).Created, SortMissing.Last)
                       .ThenByDescending(o => (o as IAmCommerceSearchable).ProductPrices[priceRef], SortMissing.Last)
                       .ThenByDescending(o => (o as IAmCommerceSearchable).DisplayNameForSort, SortMissing.Last) :
                   search
                       .ThenByDescending(o => (o as IRating).Rating, SortMissing.Last) //Rating
                       .ThenByDescending(o => (o as IAmCommerceSearchable).Created, SortMissing.Last) //Created
                       .ThenBy(o => (o as IAmCommerceSearchable).ProductPrices[priceRef], SortMissing.Last) //Price
                       .ThenBy(o => (o as IAmCommerceSearchable).DisplayNameForSort, SortMissing.Last); //Title
            }
            else
            {
                return !isOrderByDescending ?
                   search
                       .OrderBy(o => (o as IStockStatus).StockStatusForOrder, SortMissing.Last)
                       .ThenBy(o => (o as IRating).Rating, SortMissing.Last)
                       .ThenBy(o => (o as IAmCommerceSearchable).Created, SortMissing.Last)
                       .ThenByDescending(o => (o as IAmCommerceSearchable).ProductPrices[priceRef], SortMissing.Last)
                       .ThenByDescending(o => (o as IAmCommerceSearchable).DisplayNameForSort, SortMissing.Last) :
                   search
                       .OrderBy(o => (o as IStockStatus).StockStatusForOrder, SortMissing.Last) //Stock Status
                       .ThenByDescending(o => (o as IRating).Rating, SortMissing.Last) //Rating
                       .ThenByDescending(o => (o as IAmCommerceSearchable).Created, SortMissing.Last) //Created
                       .ThenBy(o => (o as IAmCommerceSearchable).ProductPrices[priceRef], SortMissing.Last) //Price
                       .ThenBy(o => (o as IAmCommerceSearchable).DisplayNameForSort, SortMissing.Last); //Title
            }
        }

        public void ReIndexContents<T>(IEnumerable<T> contentsToIndex) where T : IContent
        {
            if (contentsToIndex.IsNullOrEmpty()) return;

            _findClient.Index(contentsToIndex);
        }

        private ITypeSearch<IContentData> AddCommerceFacet(ITypeSearch<IContentData> search, IAddCommerceSearchFacets searchFacet)
        {
            if (searchFacet.FacetType == FacetType.Term)
            {
                return new Search<TrmVariant, IQuery>(search, context =>
                {
                    // ReSharper disable once UseStringInterpolation
                    var facetRequest = new TermsFacetRequest(searchFacet.Name)
                    {
                        Field = string.Format("{0}$${1}", searchFacet.Term, "string"),
                        Size = 100
                    };
                    context.RequestBody.Facets.Add(facetRequest);
                });
            }

            if (searchFacet.FacetType != FacetType.Range) return search;

            var ranges = new List<NumericRange>
            {
                new NumericRange {To = searchFacet.StartPoint - 1 }
            };
            int rangeStart;

            for (rangeStart = searchFacet.StartPoint; rangeStart + searchFacet.Increment < searchFacet.EndPoint; rangeStart = rangeStart + searchFacet.Increment)
            {
                //If we've added more than 20 then the UI can't nicely handle this
                //Also if an editor creates a range block with a tiny increment and an end point in the millions it'll bring the site down
                //This will ensure that the start and end are covered but we're preventing the site crashing from trying to render hundreds/thousands of options
                if (ranges.Count > 20)
                {
                    break;
                }

                ranges.Add(new NumericRange { From = rangeStart, To = rangeStart + searchFacet.Increment });
            }

            if (rangeStart < searchFacet.EndPoint)
            {
                ranges.Add(new NumericRange { From = rangeStart, To = searchFacet.EndPoint });
            }


            ranges.Add(new NumericRange { From = searchFacet.EndPoint });

            Action<NumericRangeFacetRequest> action = x => x.Ranges.AddRange(ranges);

            var searchTerm = searchFacet.Term == "ProductPrices" ? _localPriceDataHelper.GetPriceReference() + "$$number" : string.Format("{0}$${1}", searchFacet.Term, "number");

            return new Search<TrmVariant, IQuery>(search, context =>
            {
                var rangeFacetRequest = new NumericRangeFacetRequest(searchFacet.Name)
                {
                    Field = searchTerm
                };
                action(rangeFacetRequest);
                context.RequestBody.Facets.Add(rangeFacetRequest);
            });

        }
    }
}