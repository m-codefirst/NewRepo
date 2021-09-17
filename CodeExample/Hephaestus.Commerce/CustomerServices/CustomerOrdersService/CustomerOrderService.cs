using System;
using System.Collections.Generic;
using System.Linq;
using Hephaestus.Commerce.CustomerServices.CustomerOrdersService.Model;
using Mediachase.Commerce.Orders;

namespace Hephaestus.Commerce.CustomerServices.CustomerOrdersService
{
    public class CustomerOrderService : ICustomerOrdersService
    {
        protected readonly IAmOrderContext OrderContext;
        protected readonly IAmCustomerContact CustomerContact;

        public CustomerOrderService(IAmOrderContext orderContext, IAmCustomerContact customerContact)
        {
            OrderContext = orderContext;
            CustomerContact = customerContact;
        }

        public IEnumerable<CustomerOrderResult> AllPurchaseOrdersForCustomer
        {
            get
            {
                var orders = OrderContext.CurrentOrderContext.GetPurchaseOrders(new Guid(CustomerContact.CurrentContact.PrimaryKeyId.ToString())).OrderByDescending(o => o.Created);

                var allOrders = orders.Select(order => new CustomerOrderResult
                 {
                     OrderNo = order.Id,
                     OrderDate = order.Created,
                     OrderStatus = order.Status,
                     TotalAmount = order.Total,
                     BillingCurrency = order.BillingCurrency,
                     OrderTracking = order.TrackingNumber,
                     MarketId = order.MarketId
                 });

                return allOrders;
            }
        }

        public PurchaseOrder GetSpecificOrder(int orderId)
        {
            return OrderContext.CurrentOrderContext.GetPurchaseOrderById(orderId);
        }

        public virtual IEnumerable<CustomerOrderResult> GetSpecificOrders(Func<CustomerOrderResult, bool> expr)
        {
            return AllPurchaseOrdersForCustomer.Where(expr);
        }

        public virtual IEnumerable<CustomerOrderResult> GetAllPurchaseOrdersForCustomerByCount(int count)
        {
            var allOrders = new List<CustomerOrderResult>();
            var orders = OrderContext.CurrentOrderContext.GetPurchaseOrders(new Guid(CustomerContact.CurrentContact.PrimaryKeyId.ToString())).OrderByDescending(o => o.Created);

            if (orders.Any())
            {
                allOrders.AddRange(orders.Select(order => new CustomerOrderResult
                {
                    OrderNo = order.Id,
                    OrderDate = order.Created,
                    OrderStatus = order.Status,
                    TotalAmount = order.Total,
                    BillingCurrency = order.BillingCurrency,
                    OrderTracking = order.TrackingNumber
                }));
            }

            return allOrders.Take(count);
        }

    }
}
