using EPiServer.Commerce.Order;
using EPiServer.Core;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.InventoryService;
using TRM.Web.Models.DTOs;
using ILogger = EPiServer.Logging.ILogger;

namespace TRM.Web.Helpers
{
    public interface IAmInventoryHelper
    {
        StockSummaryDto GetStockSummary(ContentReference reference);
        StockSummaryDto GetStockSummary(ContentReference reference, CustomerContact customerContact);
        void SetItemShippingMessages(IOrderGroup orderGroup);
        void SetItemShippingMessages(IOrderGroup orderGroup, CustomerContact customerContact);
        bool CanBackOrder(InventoryRecord inventoryRecord);
        bool CanPreOrder(InventoryRecord inventoryRecord);
        void LogStockQuantity(IOrderGroup orderGroup, ILogger logger, string afterBeforeMessage, CustomerContact customerContact = null);
    }
}