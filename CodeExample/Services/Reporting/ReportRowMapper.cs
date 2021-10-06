using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Find.Cms;
using EPiServer.Find.Commerce;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Services.Reporting
{
    public static class ReportRowMapper
    {
        public static PublishedContentReportRow ToPublishedContentReportRow(this IContent content)
        {
            var trackable = (content as IChangeTrackable);
            var localizable = (content as ILocalizable);
            var versionable = (content as IVersionable);

            var row = new PublishedContentReportRow
            {
                Name = content.Name,
                LastPublished = trackable?.Saved,
                StartPublish = versionable?.StartPublish,
                ChangedBy = trackable?.ChangedBy ?? "-",
                Language = localizable?.Language.Name ?? "-",
                ContentType = content.ContentTypeName(),
                ContentLink = content.ContentLink
            };

            return row;
        }

        public static StockReportRow ToStockReportRow(this TrmVariationBase variation, Dictionary<string, string> warehouseDictionary)
        {
            var firstInventory = variation.Inventories().FirstOrDefault();
            var warehouseCode = firstInventory?.WarehouseCode ?? string.Empty;

            var row = new StockReportRow
            {
                ContentLink = variation.ContentLink,
                Sku = variation.Code,
                ProductTitle = variation.DisplayName,
                Brand = variation.BrandDisplayName,
                WarehouseCode = warehouseCode,
                Location =  warehouseDictionary.ContainsKey(warehouseCode) ? warehouseDictionary[warehouseCode] : string.Empty,
                InStock = (double)(firstInventory?.InStockQuantity ?? 0),
                BackorderAvailability = firstInventory?.BackorderAvailabilityDate,
                BackorderQuantity = (double)(firstInventory?.BackorderQuantity ?? 0),
                PreorderAvailability = firstInventory?.PreorderAvailabilityDate,
                PreorderQuantity = (double)(firstInventory?.PreorderQuantity ?? 0),
                ReorderMinQuantity = (double) (firstInventory?.ReorderMinQuantity ?? 0),
                PublishedOnSite = variation.PublishOntoSite,
                Sellable = variation.Sellable,
                Restricted = variation.Restricted,
                OffSale =  variation.OffSale
            };

            return row;
        }
    }
}