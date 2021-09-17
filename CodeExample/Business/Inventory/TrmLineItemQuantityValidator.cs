using System;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Internal;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.InventoryService;

namespace TRM.Web.Business.Inventory
{
    /// <summary>
    /// Solution to allow Bullion variant buy premiums to be configured
    /// </summary>
    public class TrmLineItemQuantityValidator : LineItemQuantityValidator
    {
        public TrmLineItemQuantityValidator()
        {
        }

        public new decimal GetAllowedQuantity(ILineItem lineItem, IStockPlacement stock, Action<ILineItem, ValidationIssue> onValidationError)
        {
            return base.GetAllowedQuantity(lineItem, stock, onValidationError);
        }

        public new decimal GetValidatedQuantity(IShipmentInventory shipmentInventory, ILineItem lineItem, InventoryRecord inventoryRecord, Action<ILineItem, ValidationIssue> onValidationError)
        {
            return base.GetValidatedQuantity(shipmentInventory, lineItem, inventoryRecord, onValidationError);
        }
    }
}
