using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using TRM.Shared.Interfaces;
using static TRM.Shared.Constants.Enums;
using static TRM.Shared.Constants.StringConstants;

namespace TRM.Shared.Helpers
{
    public class EpiServerOrderHelper : IAmEpiServerOrderHelper
    {
        public IEnumerable<PurchaseOrder> GetEpiserverOrders(int maxOrders)
        {
            var parameters = new OrderSearchParameters
            {
                SqlWhereClause = "OrderGroup.Name = 'Default' and OrderGroupId IN (SELECT ObjectId FROM OrderGroup_PurchaseOrder po LEFT JOIN cls_Contact c ON OrderGroup.CustomerId = c.ContactId WHERE po.SendStatus < 3 AND (c.ObsAccountNumber <> NULL or c.ObsAccountNumber <>\'\'))"
            };
            var classes = new StringCollection { OrderContext.Current.PurchaseOrderMetaClass.Name };

            var searchOptions = new OrderSearchOptions { Classes = classes, RecordsToRetrieve = maxOrders };
            var orders = OrderContext.Current.Search<PurchaseOrder>(parameters, searchOptions);

            return orders;
        }

        public eOrderSendStatus GetOrderStatus(string trackingNumber)
        {
            // Get Purchase Order
            var orderSendStatus = eOrderSendStatus.Invalid;
            var purchaseOrder = OrderContext.Current.GetPurchaseOrder(trackingNumber);
            if (purchaseOrder != null)
            {
                if (Enum.TryParse(purchaseOrder[CustomFields.SendStatus].ToString(), out orderSendStatus))
                {
                    return orderSendStatus;
                }
            }
            return orderSendStatus;
        }

        public bool SaveEpiServerOrderStatus(string trackingNumber, eOrderSendStatus orderSendStatus)
        {
            var results = GetPurchaseOrdersFromTrackingNumber(trackingNumber, out var matchingRecords);
            if (matchingRecords < 1) return false;

            var firstRecord = results.FirstOrDefault();
            if (firstRecord == null) return false;

            firstRecord[CustomFields.SendStatus] = (int)orderSendStatus;
            firstRecord.AcceptChanges();
            if (matchingRecords <= 1) return true;

            foreach (var duplicateOrders in results.Skip(1))
            {
                duplicateOrders[CustomFields.SendStatus] = 99;
                duplicateOrders.AcceptChanges();
            }
            return true;
        }

        public List<PurchaseOrder> GetPurchaseOrdersFromTrackingNumber(string trackingNumber, out int totalRecords)
        {
            var orderSearchOptions = new OrderSearchOptions {Namespace = "Mediachase.Commerce.Orders"};
            orderSearchOptions.Classes.Add("PurchaseOrder");
            orderSearchOptions.RecordsToRetrieve = int.MaxValue;

            var parameters = new OrderSearchParameters
            {
                SqlMetaWhereClause = $"META.TrackingNumber = '{trackingNumber}'"
            };
            var orderSearch = new OrderSearch(parameters, orderSearchOptions);
            return OrderContext.Current.Search<PurchaseOrder>(orderSearch, out totalRecords).ToList();
        }
    }
}
