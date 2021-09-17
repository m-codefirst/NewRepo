﻿using System.Linq;
using System.Text.RegularExpressions;
using EPiServer.Find;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Api.Querying.Queries;
using EPiServer.Find.Tracing;

namespace TRM.Web.Helpers.Find.EpiserverLabsFindToolbox
{

    public static class QueryHelpers
    {

        // Return all terms and phrases in query
        public static string[] GetQueryPhrases(string query)
        {

            // Replace any occurence of ¤, with a whitespace, which is used as term seperator for built-in .UsingSynonyms() as this will otherwise make MatchPhrase, MatchPhrasePrefix not match
            string cleanedQuery = UnescapeElasticSearchQuery(query).Replace("¤", " ");

            // Replace double space, tabs with single whitespace and trim space on side
            cleanedQuery = Regex.Replace(cleanedQuery, @"\s+", " ").Trim();

            // Match single terms and quoted terms, allow hyphens and ´'` in terms, allow space between quotes and word.
            return Regex.Matches(cleanedQuery, @"([\w-]+)|([""][\s\w-´'`]+[""])").Cast<Match>().Select(c => c.Value.Trim()).Except(new string[] { "AND", "OR" }).Take(50).ToArray();
        }

        public static string UnescapeElasticSearchQuery(string s)
        {
            return s.Replace("\\", "");
        }

        public static string EscapeElasticSearchQuery(string s)
        {
            return s.Replace("-", "\\-");
        }

        public static bool TryGetMinShouldMatchQueryStringQuery(IQuery query, out MinShouldMatchQueryStringQuery currentMinShouldMatchQueryStringQuery)
        {
            currentMinShouldMatchQueryStringQuery = query as MinShouldMatchQueryStringQuery;
            if (currentMinShouldMatchQueryStringQuery == null)
            {
                return false;
            }

            return true;
        }

        public static bool TryGetBoolQuery(IQuery query, out BoolQuery currentBoolQuery)
        {
            currentBoolQuery = query as BoolQuery;
            if (currentBoolQuery == null)
            {
                return false;
            }

            return true;

        }

        public static bool TryGetQueryStringQuery<TSource>(IQuery query, IQueriedSearch<TSource> search, out MultiFieldQueryStringQuery currentQueryStringQuery)
        {
            currentQueryStringQuery = query as MultiFieldQueryStringQuery;
            if (currentQueryStringQuery == null)
            {
                // Synonyms are only supported for QueryStringQuery
                EPiServer.Find.Tracing.Trace.Instance.Add(new TraceEvent(search, "The use of synonyms are only supported för QueryStringQueries, i.e. with the use of the .For() -extensions. The query will be executed without the use of synonyms.") { IsError = false });
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get query, default operator, analyzer and fields from current queryStringQuery (produced by For())
        /// </summary>
        /// <param name="currentQueryStringQuery"></param>
        /// <returns></returns>
        public static string GetQueryString(MultiFieldQueryStringQuery currentQueryStringQuery)
        {
            return (currentQueryStringQuery.Query ?? string.Empty).ToString();
        }

        public static string GetRawQueryString(MinShouldMatchQueryStringQuery currentQueryStringQuery)
        {
            return (currentQueryStringQuery.RawQuery ?? string.Empty).ToString();
        }
        public static bool IsStringQuoted(string text)
        {
            return (text.StartsWith("\"") && text.EndsWith("\""));
        }
    }

}