using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using PricingAndTradingService.Models.APIResponse;
using TRM.Web.Models.EntityFramework.Orders;
using TRM.Web.Models.ViewModels;
using TRM.Web.Models.ViewModels.Cart;

namespace TRM.Web.Helpers
{
    public interface IAmOrderHelper
    {
        List<string> GetOrderNumbersFromPurchaseOrder(IPurchaseOrder purchaseOrder, int numberOfOrders = 0);
        List<string> GetOrderNumbersFromItems(ICollection<OrderLine> items, string epiServerOrderId, int numberOfOrders = 1);
        void SaveSalesOrder(PurchaseOrderViewModel purchaseOrder, string episerverCustomerRef, string clientIpAddress = "");
        decimal GetPurchaseOrderHistoricalSpend(string userId, DateTime since);
        AccountOrderViewModel GetPurchaseOrderList(string epiServerCustomerRef, string axCustomerRef, int pageNumber, int resultsPerPage, bool showClosedOrders);
        PurchaseOrderViewModel GetPurchaseOrder(string orderId, string customerRef, string epiServerCustomerRef);
        void TransferGuestOrderToNewlyCreatedUser(CustomerContact currentContact, string oldContactId);
        IPurchaseOrder GetLastOrder(Guid contactId);
        void CloseOrder(string orderId);
        void OpenOrder(string orderId);
        void SavePurchaseOrdersExportTransaction(IPurchaseOrder purchaseOrder, string integrationStatus = "");
        void SavePurchaseOrdersExportTransaction(IPurchaseOrder purchaseOrder, CustomerContact customerContact, string integrationStatus = "");
        void UpdatePurchaseOrderWhenPampQuoteSuccess(IPurchaseOrder purchaseOrder, ExecuteResponse finishQuoteResult);
        void UpdatePurchaseOrderWhenPampQuoteRejected(IPurchaseOrder purchaseOrder);
    }
}