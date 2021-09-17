using System;
using System.Linq.Expressions;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Api.Querying.Queries;
using EPiServer.Find.Cms;
using TRM.Web.Helpers.Find.EpiserverLabsFindToolbox;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers.Find
{
    public static class QueryExtensions
    {
        public static IQueriedSearch<T, QueryStringQuery> AddFuzzySearch<T>(this IQueriedSearch<T, QueryStringQuery> search,
            string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return search;
            }

            Expression<Func<T, string>>[] fuzzySearchFields =
            {
                x => (x as PageData).PageName,
                x => (x as TrmVariationBase).DisplayName,
                x => (x as TrmVariationBase).Description.AsViewedByAnonymous(),
                x => (x as ArticlePage).MainBody.AsViewedByAnonymous(),
            };

            return search.MinimumShouldMatch("75%")
                .FuzzyMatch(fuzzySearchFields)
                .WildcardMatch(fuzzySearchFields);
        }
    }
}