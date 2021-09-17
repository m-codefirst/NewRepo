using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Cms;
using TRM.Web.Constants;
using TRM.Web.Models.Find;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers.Find
{
    public interface IFindHelper
    {
        IList<EntryPartialViewModel> GetEntryViewModel(FindResults<IAmCommerceSearchable> results, EntrySortOrder entrySortOrder = EntrySortOrder.SortBy, Enums.eStockStatus[] excludeStatuses = null);
        IList<EntryPartialViewModel> GetEntryViewModel(FindResults<IAmCommerceSearchable> results, bool showEntryCompare, EntrySortOrder entrySortOrder, Enums.eStockStatus[] excludeStatuses = null);
        IList<TeaserViewModel> GetArticleViewModel(IContentResult<IContentData> results);
        TeaserViewModel GetArticleViewModel(IContent content);
        IList<TeaserViewModel> GetArticleViewModel(FindResults<IContent> results);
        ITypeSearch<IAmCommerceSearchable> AddCommerceFacet(ITypeSearch<IAmCommerceSearchable> search, IAddCommerceSearchFacets searchFacet);
        FindFacet ProcessFacet(IContentResult<IAmCommerceSearchable> result, IContentResult<IAmCommerceSearchable> filterResults, IAddCommerceSearchFacets searchFacet, List<QueryStringFilter> queryFilters);
    }
}