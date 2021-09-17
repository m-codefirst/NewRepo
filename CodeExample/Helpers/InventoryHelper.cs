using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Find.Helpers.Text;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging;
using EPiServer.Web;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.InventoryService;
using System;
using System.Collections.Generic;
using TRM.Shared.Extensions;
using TRM.Shared.Interfaces;
using TRM.Web.Constants;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Models.Pages;
using ILogger = EPiServer.Logging.ILogger;

namespace TRM.Web.Helpers
{
    public class InventoryHelper : IAmInventoryHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IInventoryService _inventoryService;
        private readonly LocalizationService _localizationService;
        private readonly ICurrentMarket _currentMarket;
        private readonly DateTime _safeBeginningOfTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly CustomerContext _customerContext;

        public InventoryHelper(IContentLoader contentLoader,
            IInventoryService inventoryService,
            LocalizationService localizationService,
            ICurrentMarket currentMarket,
            CustomerContext customerContext
            )
        {
            _contentLoader = contentLoader;
            _inventoryService = inventoryService;
            _localizationService = localizationService;
            _currentMarket = currentMarket;
            _customerContext = customerContext;
        }

        public StockSummaryDto GetStockSummary(ContentReference reference)
        {
            return GetStockSummary(reference, null);
        }
        public StockSummaryDto GetStockSummary(ContentReference reference, CustomerContact customerContact)
        {
            var stockSummary = new StockSummaryDto() { ItemReference = reference };

            var content = _contentLoader.Get<IContent>(reference);

            var outSourcedInventory = content as IOutSourceInventory;
            if (outSourcedInventory != null && outSourcedInventory.OutSrcInventoryRef != null && outSourcedInventory.OutSrcInventoryRef.ID > 0)
            {
                var childReference = outSourcedInventory.OutSrcInventoryRef;
                var childContent = _contentLoader.Get<EntryContentBase>(childReference);
                var childOutSourcedInventory = childContent as IOutSourceInventory;
                if (childOutSourcedInventory == null ||
                    childOutSourcedInventory.OutSrcInventoryRef == null ||
                    childOutSourcedInventory.OutSrcInventoryRef.ID <= 0)
                {
                    // if child doesn't point anywhere, use it
                    content = _contentLoader.Get<IContent>(outSourcedInventory.OutSrcInventoryRef);
                }
            }

            var item = content as IControlInventory;

            if (item == null || item.OffSale || !item.Sellable)
            {
                stockSummary.Status = Enums.eStockStatus.NoLongerAvailable;
                return stockSummary;
            }

            stockSummary.IsTracked = item.TrackInventory;

            stockSummary.MinQuantity = ZeroOrMore(item.MinQuantity, (int)Enums.eQuantity.DefaultMin);
            stockSummary.MaxQuantity = ZeroOrMore(item.MaxQuantity, (int)Enums.eQuantity.DefaultMax);
            stockSummary.LimitedEditionPresentation = item.LimitedEditionPresentation;
            stockSummary.SourcedToOrder = item.SourcedToOrder;

            var iControlTrmMarketFiltering = content as IControlTrmMarketFiltering;
            if (iControlTrmMarketFiltering != null)
            {
                if (iControlTrmMarketFiltering.UnavailableMarkets.Contains(customerContact != null ? customerContact.GetCurrentMarket().MarketId.Value : _currentMarket.GetCurrentMarket().MarketId.Value))
                {
                    stockSummary.Status = Enums.eStockStatus.NoLongerAvailable;
                    return stockSummary;
                }
            }

            if (!item.TrackInventory)
            {
                stockSummary.Status = Enums.eStockStatus.InStock;
                return stockSummary;
            }


            var allInventoryRecords = _inventoryService.QueryByEntry(new List<string> { item.Code });

            var requestDate = DateTime.UtcNow;

            foreach (var inventoryRecord in allInventoryRecords)
            {
                var warehouseStock = decimal.Zero;
                var warehouseBackOrder = decimal.Zero;
                var warehousePreOrder = decimal.Zero;

                var warehouseCanPreorder = CanPreOrder(inventoryRecord);
                var warehouseCanBackorder = CanBackOrder(inventoryRecord);

                if (requestDate >= inventoryRecord.PurchaseAvailableUtc)
                {
                    warehouseStock = ZeroOrMore(inventoryRecord.PurchaseAvailableQuantity);

                    if (warehouseCanBackorder)
                    {
                        warehouseBackOrder = ZeroOrMore(inventoryRecord.BackorderAvailableQuantity);
                    }

                }
                else if (warehouseCanPreorder)
                {
                    warehousePreOrder = ZeroOrMore(inventoryRecord.PreorderAvailableQuantity);
                }

                if (warehouseCanPreorder) stockSummary.CanPreorder = true;
                if (warehouseCanBackorder) stockSummary.CanBackorder = true;

                stockSummary.PurchaseAvailableQuantity += warehouseStock;
                stockSummary.BackorderAvailableQuantity += warehouseBackOrder;
                stockSummary.PreorderAvailableQuantity += warehousePreOrder;
            }


            if (stockSummary.PurchaseAvailableQuantity > 0)
            {
                stockSummary.Status = stockSummary.PurchaseAvailableQuantity < 10 ? Enums.eStockStatus.LowStock : Enums.eStockStatus.InStock;

                stockSummary.ShippingMessage = GetInStockShippingMessage(reference);
                return stockSummary;
            }

            if (stockSummary.CanBackorder)
            {
                stockSummary.Status = Enums.eStockStatus.AwaitingStock;
                stockSummary.ShippingMessage = GetAwaitingShippingMessage(reference);
                return stockSummary;
            }

            if (stockSummary.CanPreorder)
            {
                stockSummary.Status = Enums.eStockStatus.PreOrder;
                return stockSummary;
            }

            if (item.LimitedEditionPresentation > 0)
            {
                stockSummary.Status = Enums.eStockStatus.SoldOut;
                return stockSummary;
            }

            stockSummary.Status = Enums.eStockStatus.AwaitingStock;

            return stockSummary;

        }
        public bool CanBackOrder(InventoryRecord inventoryRecord)
        {
            var requestDate = DateTime.UtcNow;
            return inventoryRecord.BackorderAvailableQuantity > decimal.Zero && inventoryRecord.BackorderAvailableUtc <= requestDate;
        }
        public bool CanPreOrder(InventoryRecord inventoryRecord)
        {
            var requestDate = DateTime.UtcNow;
            return inventoryRecord.PreorderAvailableUtc > _safeBeginningOfTime && requestDate >= inventoryRecord.PreorderAvailableUtc && !(inventoryRecord.PurchaseAvailableUtc < requestDate);
        }
        public void SetItemShippingMessages(IOrderGroup orderGroup)
        {
            SetItemShippingMessages(orderGroup, null);
        }
        public void SetItemShippingMessages(IOrderGroup orderGroup, CustomerContact customerContact)
        {
            foreach (var item in orderGroup.GetAllLineItems())
            {
                var stockSummary = GetStockSummary(item.GetEntryContent().ContentLink, customerContact);
                item.Properties[Shared.Constants.StringConstants.CustomFields.ShippingMessageFieldName] = stockSummary.ShippingMessage;
            }
        }

        public void LogStockQuantity(IOrderGroup orderGroup, ILogger logger, string afterBeforeMessage, CustomerContact customerContact = null)
        {
            var currentContactId = customerContact == null ? _customerContext.CurrentContactId : (Guid)customerContact.PrimaryKeyId;
            foreach (var item in orderGroup.GetAllLineItems())
            {
                var stockSummary = GetStockSummary(item.GetEntryContent().ContentLink, customerContact);
                logger.Error($"{afterBeforeMessage} - Stock Level: {stockSummary.TotalAvailable}, Product Code: {item.Code}, User Id: {currentContactId}");
            }
        }

        private decimal ZeroOrMore(decimal? numberToCheck, decimal defaultValue = decimal.Zero)
        {
            return numberToCheck != null ? numberToCheck > decimal.Zero ? numberToCheck.Value : defaultValue : defaultValue;
        }
        private string GetInStockShippingMessage(ContentReference contentReference)
        {
            var entry = _contentLoader.Get<EntryContentBase>(contentReference);

            var variant = entry as TrmVariant;

            if (variant != null && variant.InStockLeadTimeMessageOverride.IsNotNullOrEmpty())
            {
                return variant.InStockLeadTimeMessageOverride;
            }

            var variantWithMessage = variant as IHaveCustomShippingMessage;
            if (variantWithMessage != null && !string.IsNullOrWhiteSpace(variantWithMessage.InStockCustomShippingMessage))
            {
                return variantWithMessage.InStockCustomShippingMessage;
            }

            return _localizationService.GetStringByCulture(StringResources.AtpShippingInStock, StringConstants.TranslationFallback.InStock, ContentLanguage.PreferredCulture); ;
        }
        private string GetAwaitingShippingMessage(ContentReference contentReference)
        {
            var entry = _contentLoader.Get<EntryContentBase>(contentReference);

            var iControlAtpMessageOverride = entry as IControlAtpMessageOverride;
            if (!string.IsNullOrWhiteSpace(iControlAtpMessageOverride?.AtpMessageOverride))
            {
                return iControlAtpMessageOverride.AtpMessageOverride;
            }

            var iControlAtp = entry as IControlAtp;
            if (iControlAtp == null) return string.Empty;

            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;
            if (startPage == null || iControlAtp.AtpDate == DateTime.MinValue) return string.Empty;

            var deliveryTimespan = iControlAtp.AtpDate - DateTime.Now;

            if (string.IsNullOrEmpty(startPage.DeliveryTimespanThresholds)) return GetStoredDate(iControlAtp.AtpDate);
            var deliveryTimespans = startPage.DeliveryTimespanThresholds.Split(',');
            foreach (var timespan in deliveryTimespans)
            {
                int timespanAsInt;
                if (!int.TryParse(timespan, out timespanAsInt)) continue;
                if (deliveryTimespan.Days < timespanAsInt)
                {
                    return
                        _localizationService.GetStringByCulture(
                            string.Format(StringResources.AtpShippingTimespan, timespan),
                            string.Format(StringConstants.TranslationFallback.AtpShippingTimespan, timespan), ContentLanguage.PreferredCulture);
                }
            }

            return GetStoredDate(iControlAtp.AtpDate);
        }
        private string GetStoredDate(DateTime atpDate)
        {
            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;
            if (startPage == null) return string.Empty;

            var earlyThreshold = startPage.StoredDateEarlyThreshold;
            var lateThreshold = startPage.StoredDateLateThreshold;
            var monthName =
                _localizationService.GetStringByCulture(string.Format(StringResources.AtpShippingMonth, atpDate.Month),
                    atpDate.ToString("MMMM"), ContentLanguage.PreferredCulture);

            if (atpDate.Day < earlyThreshold)
            {
                return _localizationService.GetStringByCulture(StringResources.AtpShippingEarly, "Shipping early ", ContentLanguage.PreferredCulture) + monthName;
            }
            if (atpDate.Day > lateThreshold)
            {
                return _localizationService.GetStringByCulture(StringResources.AtpShippingLate, "Shipping late ", ContentLanguage.PreferredCulture) + monthName;
            }

            return _localizationService.GetStringByCulture(StringResources.AtpShippingMid, "Shipping mid ", ContentLanguage.PreferredCulture) + monthName;
        }
    }
}