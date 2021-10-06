using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EPiServer.Find;
using EPiServer.Find.Api;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Cms;
using EPiServer.Find.Commerce;
using EPiServer.Find.Helpers.Text;
using Mediachase.Commerce.Inventory;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Services.Reporting
{
    public class EpiFindStockReportService : IStockReportService
    {
        const int BatchSize = 1000;

        private readonly IClient findClient;
        private readonly IWarehouseRepository warehouseRepository;

        public EpiFindStockReportService(IClient findClient, IWarehouseRepository warehouseRepository)
        {
            this.findClient = findClient;
            this.warehouseRepository = warehouseRepository;
        }

        public CustomReportDto<StockReportRow> GetReport(string term, int page, int itemsPerPage, bool @ascending, string sortField)
        {
            var warehouseDictionary = warehouseRepository.List().ToDictionary(x => x.Code, x => x.Name);

            var searchRequest = findClient
                .Search<TrmVariationBase>()
                .FilterOnLanguages(new List<string> { "en-GB" })
                .ExcludeDeleted()
                .StaticallyCacheFor(TimeSpan.Zero)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage);

            if (!term.IsNullOrWhiteSpace())
            {
                searchRequest = searchRequest.Filter(x => x.Code.Match(term));
            }

            searchRequest = SetOrderBy(searchRequest, sortField, ascending);

            var searchResult = searchRequest.GetContentResult();

            var rows = searchResult.Select(@base => @base.ToStockReportRow(warehouseDictionary)).ToList();

            var totalPages = searchResult.TotalMatching / itemsPerPage + 1;

            return new CustomReportDto<StockReportRow>
            {
                Rows = rows,
                Name = "Stock Report",
                TotalMatching = searchResult.TotalMatching,
                CurrentPage = page,
                RowsPerPage = itemsPerPage,
                TotalPages = totalPages,
            };
        }

        private ITypeSearch<TrmVariationBase> SetOrderBy(ITypeSearch<TrmVariationBase> searchRequest, string sortField, bool @ascending)
        {
            switch (sortField)
            {
                //nested fields
                case nameof(StockReportRow.WarehouseCode):
                case nameof(StockReportRow.Location):
                    return OrderBy(searchRequest, x => x.Inventories(), p => p.WarehouseCode, SortMode.Max, ascending);

                case nameof(StockReportRow.InStock):
                    return OrderBy(searchRequest, x => x.Inventories(), p => p.InStockQuantity, SortMode.Max, ascending);

                case nameof(StockReportRow.BackorderAvailability):
                    return OrderBy(searchRequest, x => x.Inventories(), p => p.BackorderAvailabilityDate, SortMode.Max, ascending);

                case nameof(StockReportRow.BackorderQuantity):
                    return OrderBy(searchRequest, x => x.Inventories(), p => p.BackorderQuantity, SortMode.Max, ascending);

                case nameof(StockReportRow.PreorderAvailability):
                    return OrderBy(searchRequest, x => x.Inventories(), p => p.PreorderAvailabilityDate, SortMode.Max, ascending);

                case nameof(StockReportRow.PreorderQuantity):
                    return OrderBy(searchRequest, x => x.Inventories(), p => p.PreorderQuantity, SortMode.Max, ascending);

                case nameof(StockReportRow.ReorderMinQuantity):
                    return OrderBy(searchRequest, x => x.Inventories(), p => p.ReorderMinQuantity, SortMode.Max, ascending);

                //Plain fields
                case nameof(StockReportRow.ProductTitle):
                    return searchRequest
                        .OrderBy(x => x.DisplayName, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(StockReportRow.Brand):
                    return searchRequest
                        .OrderBy(x => x.BrandDisplayName, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(StockReportRow.PublishedOnSite):
                    return searchRequest
                        .OrderBy(x => x.PublishOntoSite, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(StockReportRow.Sellable):
                    return searchRequest
                        .OrderBy(x => x.Sellable, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(StockReportRow.Restricted):
                    return searchRequest
                        .OrderBy(x => x.Restricted, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(StockReportRow.OffSale):
                    return searchRequest
                        .OrderBy(x => x.OffSale, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);

                case nameof(StockReportRow.Sku):
                default:
                    return searchRequest
                        .OrderBy(x => x.Code, SortMissing.Last,
                            @ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending, true);
            }
        }

        private ITypeSearch<TSource> OrderBy<TSource, TEnumerableItem>(
            ITypeSearch<TSource> search,
            Expression<Func<TSource, IEnumerable<TEnumerableItem>>> enumerableFieldSelector,
            Expression<Func<TEnumerableItem, IndexValue>> itemFieldSelector,
            SortMode? mode, bool ascending)
        {
            return search.OrderBy(
                enumerableFieldSelector,
                itemFieldSelector,
                null,
                SortMissing.Last,
                ascending ? EPiServer.Find.Api.SortOrder.Ascending : EPiServer.Find.Api.SortOrder.Descending,
                mode,
                false);
        }

        public CustomReportDto<StockReportRow> GetFullReport()
        {
            var firstBatch = this.GetReport(string.Empty, 1, BatchSize, true, string.Empty);
            var currentPage = 1;

            var reportRows = new List<StockReportRow>();
            reportRows.AddRange(firstBatch.Rows);

            while (firstBatch.TotalPages > currentPage)
            {
                currentPage += 1;
                var nextBatch = GetReport(string.Empty, currentPage, BatchSize, true, string.Empty);
                reportRows.AddRange(nextBatch.Rows);
            }

            return new CustomReportDto<StockReportRow> { Name = $"{DateTime.Now:yyyyMMdd_HHmmss}_StockReport", Rows = reportRows };
        }
    }
}