using Mediachase.Commerce.Orders;
using System.Collections.Generic;
using TRM.Shared.Constants;

namespace TRM.Shared.Interfaces
{
    public interface IAmEpiServerOrderHelper
    {
        IEnumerable<PurchaseOrder> GetEpiserverOrders(int maxOrders);
        bool SaveEpiServerOrderStatus(string trackingNumber, Enums.eOrderSendStatus orderSendStatus);
        Enums.eOrderSendStatus GetOrderStatus(string trackingNumber);
        List<PurchaseOrder> GetPurchaseOrdersFromTrackingNumber(string trackingNumber, out int totalRecords);
    }
}