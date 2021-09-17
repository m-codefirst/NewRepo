using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Find.Cms;
using EPiServer.Find.Commerce;
using EPiServer.Find.Framework.BestBets;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.Find;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers.Find
{
    public interface IBestBetsFindHelper
    {
        BestBetsResults GetBestBets(string searchTerm);
        ContentResult<T> GetResultsWithBestBets<T>(IContentResult<T> findResults, List<IContent> bestBets, int currentPage = 1) where T : IContentData;
        List<IContent> GetBestBetsForType(BestBetsResults bestBets, ProductTypeFilter searchQueryTypeFilter);
        SearchTotalCountsModel GetTotalCounts(SearchSuggesstionResultModel result, BestBetsResults bestBets);
        SearchTotalCountsModel GetTotalCounts(SearchSuggesstionExtendedResultModel result, BestBetsResults bestBets);
    }

    public class BestBetsFindHelper : IBestBetsFindHelper
    {
        private readonly IContentLoader contentLoader;
        private readonly BestBetRepository _bestBetRepository;

        public BestBetsFindHelper(IContentLoader contentLoader, BestBetRepository bestBetRepository)
        {
            this.contentLoader = contentLoader;
            _bestBetRepository = bestBetRepository;
        }

        public BestBetsResults GetBestBets(string searchTerm)
        {
            var bestBetSelectors = this._bestBetRepository.List().Where(x => x.PhraseCriterion.Match(searchTerm)).Select(x => x.BestBetSelector)
                .ToList();

            var commerceBets = bestBetSelectors.Where(x => x is CommerceBestBetSelector).Cast<CommerceBestBetSelector>().Select(x => x.ContentLink);
            var pagesBets = bestBetSelectors.Where(x => x is PageBestBetSelector).Cast<PageBestBetSelector>().Select(x => x.PageLink);
            var mergedContentReferences = commerceBets.Concat(pagesBets).ToList();

            var bestBets = mergedContentReferences.Select(x =>
            {
                contentLoader.TryGet<IContent>(x, out var content);
                return content;
            }).Where(x => x != null).ToList();

            var result = new BestBetsResults
            {
                Shop = bestBets.Where(x => x is EntryContentBase && !(x is PreciousMetalsVariantBase)).Where(MerchandisingFilter).ToList(),
                Invest = bestBets.Where(x => x is PreciousMetalsVariantBase).Where(MerchandisingFilter).ToList(),
                Articles = bestBets.Where(x => x is IAmContentSearchable && ((IAmContentSearchable)x).ShowInArticleResults).ToList(),
                HelpPages = bestBets.Where(x => x is HelpPage).ToList(),
                All = bestBets
            };

            return result;
        }

        private static bool MerchandisingFilter(IContent content)
        {
            if (content is IAmCommerceSearchable == false)
            {
                return true;
            }

            var searchable = (IAmCommerceSearchable)content;

            return !searchable.Restricted && searchable.PublishOntoSite && searchable.Sellable &&
                   !searchable.ShouldBeOmittedFromSearchResults && searchable is VirtualVariantBase == false;
        }

        public ContentResult<T> GetResultsWithBestBets<T>(IContentResult<T> findResults, List<IContent> bestBets, int currentPage = 1) where T : IContentData
        {
            //Applying best bets only in first page
            if (currentPage > 1)
            {
                return new ContentResult<T>(findResults, findResults.SearchResult);
            }

            var betsResults = bestBets.Where(x => x is T).Cast<T>().Concat(findResults);
            var mergedResults = new ContentResult<T>(betsResults, findResults.SearchResult);

            return new ContentResult<T>(mergedResults, mergedResults.SearchResult);
        }

        public List<IContent> GetBestBetsForType(BestBetsResults bestBets, ProductTypeFilter searchQueryTypeFilter)
        {
            switch (searchQueryTypeFilter)
            {
                case ProductTypeFilter.All:
                    return bestBets.Shop.Concat(bestBets.Invest).ToList();
                case ProductTypeFilter.Shop:
                    return bestBets.Shop;
                case ProductTypeFilter.Invest:
                    return bestBets.Invest;
                default:
                    throw new ArgumentOutOfRangeException(nameof(searchQueryTypeFilter), searchQueryTypeFilter, null);
            }
        }

        public SearchTotalCountsModel GetTotalCounts(SearchSuggesstionResultModel result, BestBetsResults bestBets)
        {
            int variants = (result.Variants?.TotalMatching ?? 0) + (bestBets.Shop?.Count ?? 0) + (bestBets.Invest?.Count ?? 0);
            return new SearchTotalCountsModel
            {
                Variants =  variants,
                Shop = variants,
                Invest = variants,
                Articles = (result.Articles?.TotalMatching ?? 0) + (bestBets.Articles?.Count ?? 0),
                HelpPages = (result.HelpPages?.TotalMatching ?? 0) + (bestBets.HelpPages?.Count ?? 0)
            };
        }

        public SearchTotalCountsModel GetTotalCounts(SearchSuggesstionExtendedResultModel result, BestBetsResults bestBets)
        {
            int shop = (result.Shop?.TotalMatching ?? 0) + (bestBets.Shop?.Count ?? 0);
            int invest = (result.Invest?.TotalMatching ?? 0)+ (bestBets.Invest?.Count ?? 0);

            return new SearchTotalCountsModel
            {
                Variants =  shop + invest,
                Shop = shop,
                Invest = invest,
                Articles = (result.Articles?.TotalMatching ?? 0) + (bestBets.Articles?.Count ?? 0),
                HelpPages = (result.HelpPages?.TotalMatching ?? 0) + (bestBets.HelpPages?.Count ?? 0)
            };
        }
    }

    public class BestBetsResults
    {
        public List<IContent> Shop { get; set; }
        public List<IContent> Invest { get; set; }
        public List<IContent> HelpPages { get; set; }
        public List<IContent> Articles { get; set; }
        public List<IContent> All { get; set; }
    }
}