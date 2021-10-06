using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.Cms;
using Microsoft.Ajax.Utilities;

namespace TRM.Web.Services.Reporting
{
    public class PublishedContentReportService : IPublishedContentReportService
    {
        const int BatchSize = 1000;
        private readonly IClient findClient;

        public PublishedContentReportService(IClient findClient)
        {
            this.findClient = findClient;
        }

        public CustomReportDto<PublishedContentReportRow> GetReport(DateTime? @from, DateTime? to, int page, int itemsPerPage,
            bool @ascending, string sortField, string author, string language)
        {
            var searchRequest = findClient
                .Search<IContent>()
                .ExcludeDeleted()
                .StaticallyCacheFor(TimeSpan.Zero)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage);

            searchRequest = searchRequest.Filter(x => ((IVersionable)x).Status.Match(VersionStatus.Published));

            if (!language.IsNullOrWhiteSpace())
            {
                searchRequest = searchRequest.FilterOnLanguages(new List<string> {language});
            }

            if (from != null)
            {
                searchRequest = searchRequest.Filter(x => ((IChangeTrackable)x).Saved.GreaterThan(from.Value));
            }

            if (to != null)
            {
                searchRequest = searchRequest.Filter(x => ((IChangeTrackable)x).Saved.LessThan(to.Value));
            }

            if (!author.IsNullOrWhiteSpace())
            {
                searchRequest = searchRequest.Filter(x => ((IChangeTrackable)x).ChangedBy.MatchCaseInsensitive(author));
            }

            searchRequest = SetOrderBy(searchRequest, sortField, ascending);

            var searchResult = searchRequest.GetContentResult();

            var rows = searchResult.Select(@base => @base.ToPublishedContentReportRow()).ToList();

            var totalPages = searchResult.TotalMatching / itemsPerPage + 1;

            return new CustomReportDto<PublishedContentReportRow>
            {
                Rows = rows,
                Name = "Published Content Report",
                TotalMatching = searchResult.TotalMatching,
                CurrentPage = page,
                RowsPerPage = itemsPerPage,
                TotalPages = totalPages,
            };
        }

        private ITypeSearch<IContent> SetOrderBy(ITypeSearch<IContent> searchRequest, string sortField, bool @ascending)
        {
            switch (sortField)
            {
                case nameof(PublishedContentReportRow.LastPublished):
                    return searchRequest
                        .OrderBy(x => (x as IChangeTrackable).Changed, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);
               
                case nameof(PublishedContentReportRow.StartPublish):
                    return searchRequest
                        .OrderBy(x => (x as IVersionable).StartPublish, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(PublishedContentReportRow.ChangedBy):
                    return searchRequest
                        .OrderBy(x => (x as IChangeTrackable).ChangedBy, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(PublishedContentReportRow.Language):
                    return searchRequest
                        .OrderBy(x => ((ILocalizable)x).Language.Name, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(PublishedContentReportRow.ContentType):
                    return searchRequest
                        .OrderBy(x => x.ContentTypeName(), SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(PublishedContentReportRow.ContentLink):
                    return searchRequest
                        .OrderBy(x => x.ContentLink, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(PublishedContentReportRow.Name):
                default:
                    return searchRequest
                        .OrderBy(x => x.Name, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

            }
        }

        public CustomReportDto<PublishedContentReportRow> GetFullReport(DateTime? @from, DateTime? to, string author, string language)
        {
            var firstBatch = this.GetReport(@from, to, 1, BatchSize, true, string.Empty, author, language);
            var currentPage = 1;

            var reportRows = new List<PublishedContentReportRow>();
            reportRows.AddRange(firstBatch.Rows);

            while (firstBatch.TotalPages > currentPage)
            {
                currentPage += 1;
                var nextBatch = GetReport(@from, to, currentPage, BatchSize, true, string.Empty, author, language);
                reportRows.AddRange(nextBatch.Rows);
            }

            return new CustomReportDto<PublishedContentReportRow>
            { Name = $"{DateTime.Now:yyyyMMdd_HHmmss}_PublishedContentReport", Rows = reportRows };
        }
    }
}