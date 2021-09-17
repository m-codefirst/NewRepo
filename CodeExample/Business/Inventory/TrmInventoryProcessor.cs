using System;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;

namespace TRM.Web.Business.Inventory
{
    /// <summary>
    /// Solution to allow Bullion variant buy premiums to be configured
    /// </summary>
    public class TrmInventoryProcessor : IInventoryProcessor
    {
        private readonly IInventoryProcessor _epiInventoryProcessor;

        public TrmInventoryProcessor(IInventoryProcessor epiInventoryProcessor)
        {
            _epiInventoryProcessor = epiInventoryProcessor;
        }

        public void AdjustInventoryOrRemoveLineItem(IShipment shipment, OrderStatus orderStatus, Action<ILineItem, ValidationIssue> onValidationError)
        {
            _epiInventoryProcessor.AdjustInventoryOrRemoveLineItem(shipment, orderStatus, onValidationError);
        }

        public void UpdateInventoryOrRemoveLineItem(IShipment shipment, Action<ILineItem, ValidationIssue> onValidationError)
        {
            _epiInventoryProcessor.UpdateInventoryOrRemoveLineItem(shipment, onValidationError);
        }
    }
}
