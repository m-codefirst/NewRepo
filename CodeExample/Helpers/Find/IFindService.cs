using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.Find.Cms;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Find;
using TRM.Web.Models.ViewModels.GBCH;

namespace TRM.Web.Helpers.Find
{
    public interface IFindService
    {
        FindResults<IAmCommerceSearchable> SearchCommerce(FindFilters searchQuery, List<QueryStringFilter> queryFilters, List<IAddCommerceSearchFacets> facets);
        GbchCategoryViewModel GetGBCHSubCategories(GBCHCategory currentContent);
        IEnumerable<GbchVariantViewModel> GetRelatedVariantsByLetter(int contentId, string letters = "");
        IEnumerable<T> GetAllContents<T>() where T : IContent;

        void ReIndexContents<T>(IEnumerable<T> contentsToIndex) where T : IContent;
        IEnumerable<TrmVariant> GetNewInItems(ContentReference categoryRef, int numberOfProducts);
        SearchSuggesstionResultModel GetSearchSuggestionResult(string searchTerm, int maxResults = 10, int currentPage = 1, int cacheResultsForMinutes = 0, bool ignoreCommerce = false, bool ignoreDefaultOperator = false);

        SearchSuggesstionExtendedResultModel GetExtendedSuggestionResult(string searchTerm, IEnumerable<IAddCommerceSearchFacets> facets, int maxResults = 3,
            int cacheResultsForMinutes = 0, bool ignoreCommerce = false,
            bool useAndAsDefaultOperator = false, string visitorLocation = "");
    }

    public class SearchSuggesstionResultModel
    {
        public IContentResult<IContentData> Variants { get; set; }
        public IContentResult<IContentData> HelpPages { get; set; }
        public IContentResult<IContentData> Articles { get; set; }
        public SearchTotalCountsModel Counts { get; set; }
    }

    public class SearchSuggesstionExtendedResultModel
    {
        public IContentResult<IContentData> Shop { get; set; }
        public IContentResult<IContentData> Invest { get; set; }
        public IContentResult<IContentData> HelpPages { get; set; }
        public IContentResult<IContentData> Articles { get; set; }
        public SearchTotalCountsModel Counts { get; set; }
    }

    public class SearchTotalCountsModel
    {
        public int Shop { get; set; }
        public int Invest { get; set; }
        public int HelpPages { get; set; }
        public int Articles { get; set; }
        public int Variants { get; set; }
    }
}
