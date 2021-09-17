using System;
using System.Collections.Generic;
using Hephaestus.Commerce.CustomerServices.CustomerOrdersService.Model;
using Mediachase.Commerce.Orders;

namespace Hephaestus.Commerce.CustomerServices.CustomerOrdersService
{
    public interface ICustomerOrdersService
    {
        IEnumerable<CustomerOrderResult> AllPurchaseOrdersForCustomer { get; }

        PurchaseOrder GetSpecificOrder(int orderId);

        IEnumerable<CustomerOrderResult> GetSpecificOrders(Func<CustomerOrderResult, bool> expr);

        IEnumerable<CustomerOrderResult> GetAllPurchaseOrdersForCustomerByCount(int count);
    }
}
