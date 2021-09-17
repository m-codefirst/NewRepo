using EPiServer;
using EPiServer.Commerce.Order;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging;
using Hephaestus.Commerce.Shared.Services;
using Hephaestus.ContentTypes.Business.Extensions;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using PricingAndTradingService.Models.APIResponse;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Models.Export;
using TRM.IntegrationServices.Models.Impersonation;
using TRM.Shared.Helpers;
using TRM.Web.Constants;
using TRM.Web.Helpers.Converters.Interfaces;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.EntityFramework.Orders;
using TRM.Web.Models.ViewModels;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Services;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Helpers
{
    public class OrderHelper : IAmOrderHelper
    {
        private readonly ICurrencyService _currencyService;
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly LocalizationService _localizationService;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(OrderHelper));

        private readonly IGetOrdersService _getOrdersService;
        private readonly CustomerContext _customerContext;
        private readonly IExportTransactionService _exportTransactionService;

        private readonly IAmAssetHelper _assetHelper;
        private readonly IAddressConverterByOrder _addressConverterByOrder;

        private readonly IUserService _userService;
        private readonly IImpersonationLogService _impersonationLogService;

        public OrderHelper(
            ICurrencyService currencyService,
            IContentRepository contentRepository,
            ReferenceConverter referenceConverter,
            LocalizationService localizationService,
            IAmInventoryHelper inventoryHelper,
            IOrderRepository orderRepository,
            IGetOrdersService getOrdersService,
            CustomerContext customerContext,
            IExportTransactionService exportTransactionService,
            IAmAssetHelper assetHelper,
            IAddressConverterByOrder addressConverterByOrder,
            IUserService userService,
            IImpersonationLogService impersonationLogService)
        {
            _currencyService = currencyService;

            _contentRepository = contentRepository;
            _referenceConverter = referenceConverter;
            _localizationService = localizationService;
            _inventoryHelper = inventoryHelper;
            _orderRepository = orderRepository;
            _getOrdersService = getOrdersService;
            _customerContext = customerContext;
            _exportTransactionService = exportTransactionService;
            _assetHelper = assetHelper;
            _addressConverterByOrder = addressConverterByOrder;
            _userService = userService;
            _impersonationLogService = impersonationLogService;
        }

        public List<string> GetOrderNumbersFromPurchaseOrder(IPurchaseOrder purchaseOrder, int numberOfOrders = 0)
        {
            //start number of orders @ 0 - deal with everything (recurring & not) inside this - the customer may not have bought any normal items, the customer may not have bought any recurring items
            var items = purchaseOrder.GetAllLineItems();

            var onlyOneItem = items.Count() == 1;
            var lineItemCollection = purchaseOrder.GetAllLineItems().ToList();
            foreach (var item in lineItemCollection)
            {
                //Handle a new "order" split for each continuity line
                if (item.Properties[StringConstants.CustomFields.MandatoryFieldName].Equals(true) || item.Properties[StringConstants.CustomFields.SubscribedFieldName].Equals(true))
                {
                    numberOfOrders += 1;
                }
            }

            //Handle any "normal" items in a "normal" order
            if (lineItemCollection.Any(li =>
                li.Properties[StringConstants.CustomFields.MandatoryFieldName].Equals(false) &&
                li.Properties[StringConstants.CustomFields.SubscribedFieldName].Equals(false)))
            {
                numberOfOrders += 1;
            }

            if (numberOfOrders == 1 || onlyOneItem)
            {
                return new List<string>() { purchaseOrder.OrderNumber };
            }

            var orderNumbers = new List<string>();

            for (var i = 1; i <= numberOfOrders; i++)
            {
                orderNumbers.Add($"{purchaseOrder.OrderNumber}-{i}");
            }

            return orderNumbers;
        }

        public List<string> GetOrderNumbersFromItems(ICollection<OrderLine> items, string epiServerOrderId,
            int numberOfOrders = 1)
        {
            var onlyOneItem = items.Count == 1;

            numberOfOrders += items.Count(item => item.IsRecurring);

            if (numberOfOrders == 1 || onlyOneItem)
            {
                return new List<string>()
                {
                    epiServerOrderId
                };
            }

            var orderNumbers = new List<string>();
            var orderLineCount = 1;
            foreach (OrderLine orderLine in items)
            {
                orderNumbers.Add($"{(string.IsNullOrEmpty(epiServerOrderId) ? orderLine.SalesId : epiServerOrderId)}-{orderLineCount}");

                orderLineCount++;
            }

            return orderNumbers;
        }

        public void SaveSalesOrder(PurchaseOrderViewModel purchaseOrder, string episerverCustomerRef, string clientIpAddress = "")
        {
            try
            {
                using (var db = new OrdersContext(StringConstants.TrmCustomDatabaseName))
                {
                    var shipment = purchaseOrder.Shipments.First();
                    var payment = purchaseOrder.Payments.First();

                    var order = new Order
                    {
                        OrderId = Guid.NewGuid(),
                        OrderNumber = purchaseOrder.OriginalOrderNumber,
                        EpiOrderNumber = purchaseOrder.OriginalOrderNumber,
                        SalesTotal = purchaseOrder.TotalDecimal,
                        SalesStatus = ((int)Enums.eOrderStatus.Open).ToString(),
                        EpiServerCustomerRef = episerverCustomerRef,
                        IsOpenOrder = true,

                        //Billing information
                        BillingName = $"{payment.Address.FirstName} {payment.Address.LastName}",
                        BillingStreet = payment.Address.Line1,
                        BillingCity = payment.Address.City,
                        BillingState = payment.Address.CountryRegion?.Region,
                        BillingPostCode = payment.Address.PostalCode,
                        BillingCountryCode = payment.Address.CountryCode,

                        //Delivery information
                        DeliveryName = $"{shipment.Address.FirstName} {shipment.Address.LastName}",
                        DeliveryStreet = shipment.Address.Line1,
                        DeliveryCity = shipment.Address.City,
                        DeliveryCounty = payment.Address.CountryRegion?.Region,
                        DeliveryCountryCode = shipment.Address.CountryCode,
                        DeliveryPostCode = shipment.Address.PostalCode,

                        CreatedDateTime = DateTime.Now,
                        ClientIpAddress = clientIpAddress,
                    };

                    var recurringStatues = new List<Shared.Constants.Enums.eRecurrenceType>()
                    {
                        Shared.Constants.Enums.eRecurrenceType.Mandatory
                    };

                    var saleLines = shipment.CartItems.Select(cartItem => new OrderLine
                    {
                        OrderLineId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        SalesId = purchaseOrder.OrderNumbers.FirstOrDefault(),
                        ItemId = cartItem.Code,
                        ItemName = cartItem.DisplayName,
                        HasbeenPersonalised = cartItem.HasbeenPersonalised,
                        SalesPrice = cartItem.PlacedPriceDecimal,
                        LineAmount = cartItem.DiscountedPriceDecimal,
                        SalesQty = cartItem.Quantity,
                        SalesStatus = ((int)Enums.eOrderStatus.Open).ToString(),
                        IsRecurring = (recurringStatues.Contains(cartItem.RecurrenceType) || cartItem.Subscribed),
                    }).ToList();

                    order.LineItems = saleLines;

                    var shippingMethod = shipment.ShippingMethods.Find(s => s.Id == shipment.ShippingMethodId);

                    var deliveryCharges = new List<OrderCharge>
                    {
                        new OrderCharge
                        {
                            OrderChargeId = Guid.NewGuid(),
                            OrderId = order.OrderId,
                            SalesId = purchaseOrder.OrderNumbers.FirstOrDefault(),
                            Value = shippingMethod?.Price.Amount ?? decimal.Zero,
                            Txt = shippingMethod?.DisplayName ?? string.Empty
                        }
                    };

                    order.Charges = deliveryCharges;

                    db.Orders.Add(order);

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                _logger.Log(Level.Error,
                    "Unable to write a purchase order to the Custom Database. Check that there are no pending data migrations or schema changes",
                    e);
            }
        }

        public decimal GetPurchaseOrderHistoricalSpend(string userId, DateTime since)
        {
            if (string.IsNullOrEmpty(userId)) return 0m;

            using (var db = new OrdersContext(StringConstants.TrmCustomDatabaseName))
            {
                db.Database.CommandTimeout = 60000;

                var query = (
                    from o in db.Orders
                    where o.EpiServerCustomerRef == userId && o.CreatedDateTime > since
                    select o.SalesTotal).DefaultIfEmpty();

                return (decimal?)query.Sum() ?? 0;
            }
        }

        public AccountOrderViewModel GetPurchaseOrderList(string epiServerCustomerRef, string axCustomerRef, int pageNumber,
            int resultsPerPage, bool showClosedOrders)
        {

            using (var db = new OrdersContext(StringConstants.TrmCustomDatabaseName))
            {
                //ToDo: Need to improve this query
                db.Database.CommandTimeout = 60000;
                var totalOrders = db.Orders.Count(o => o.IsOpenOrder != showClosedOrders && (o.CustomerRef == axCustomerRef || o.EpiServerCustomerRef == epiServerCustomerRef));
                    
                if (totalOrders == 0)
                {
                    return new AccountOrderViewModel
                    {
                        CurrentPage = 0,
                        Orders = new List<OrderListViewModel>(),
                        TotalPages = 0,
                        TotalResults = totalOrders
                    };
                }

                var totalPages = (totalOrders + resultsPerPage - 1) / resultsPerPage;

                if (pageNumber > totalPages)
                {
                    pageNumber = totalPages;
                }

                if (pageNumber <= 0)
                {
                    pageNumber = 1;
                }

                var orders = db.Orders
                    .Where(o => o.IsOpenOrder != showClosedOrders && (o.CustomerRef == axCustomerRef || o.EpiServerCustomerRef == epiServerCustomerRef))
                    .OrderByDescending(a => a.CreatedDateTime)
                    .Skip(resultsPerPage * (pageNumber - 1))
                    .Take(resultsPerPage)
                    .Include(a => a.LineItems)
                    .ToList();

                var orderList = orders.Select(order => new OrderListViewModel
                {
                    OrderId = order.OrderId.ToString(),
                    OrderDate = order.CreatedDateTime,
                    OrderNumber = order.OrderNumber,
                    OrderNumbers = GetOrderNumbersFromItems(order.LineItems, order.OrderNumber),
                    OrderStatus = GetOrderStatusDescription(order.SalesStatus),
                    SalesTotal = order.SalesTotal,
                    OrderDispatchedTo = GetOrderDispatchTo(order),
                    IsOpenOrder = order.IsOpenOrder,
                    OrderItems = GetOrderItems(order.LineItems)
                }).ToList();
                
                return new AccountOrderViewModel
                {
                    CurrentPage = pageNumber,
                    Orders = orderList,
                    TotalPages = totalPages,
                    TotalResults = totalOrders
                };
            }
        }

        public PurchaseOrderViewModel GetPurchaseOrder(string orderId, string customerRef, string epiServerCustomerRef)
        {
            using (var db = new OrdersContext(StringConstants.TrmCustomDatabaseName))
            {
                var order = db.Orders.FirstOrDefault(a => a.OrderNumber == orderId &&
                    ((!string.IsNullOrEmpty(a.EpiServerCustomerRef) && a.EpiServerCustomerRef.ToLower() == epiServerCustomerRef.ToLower()) ||
                     (!string.IsNullOrEmpty(a.CustomerRef) && a.CustomerRef.ToLower() == customerRef.ToLower())));

                if (order == null) return new PurchaseOrderViewModel { OrderFound = false };

                var cartItems = GetCartOrderItems(order.LineItems);
                var shipment = new ShipmentViewModel
                {
                    Address = _addressConverterByOrder.ConvertToDelivery(order),
                    CartItems = (cartItems != null && cartItems.Any()) ? cartItems.ToList() : Enumerable.Empty<CartItemViewModel>().ToList(),
                };

                var delivery = order.Charges.FirstOrDefault();
                decimal deliveryCharge = 0;
                if (delivery != null)
                {
                    deliveryCharge = delivery.Value;
                    shipment.ShippingMethods = new List<ShippingMethodViewModel>
                    {
                        new ShippingMethodViewModel
                        {
                            Price = new Money(deliveryCharge, _currencyService.GetCurrentCurrency()),
                            DisplayName = delivery.Txt
                        }
                    };
                }

                var purchaseOrderViewModel = new PurchaseOrderViewModel
                {
                    OrderNumbers = GetOrderNumbersFromItems(order.LineItems, order.OrderNumber),
                    OriginalOrderNumber = order.EpiOrderNumber,
                    Name = order.OrderNumber,
                    Payments = new List<PaymentSummaryViewModel> { new PaymentSummaryViewModel { Address = _addressConverterByOrder.ConvertToBilling(order) } },
                    Shipments = new List<ShipmentViewModel> { shipment },
                    SubTotal = shipment.CartItems.Sum(a => a.PlacedPriceDecimal * a.Quantity).ToString("C"),
                    TotalDelivery = deliveryCharge.ToString("C"),
                    TotalDiscount = (deliveryCharge + shipment.CartItems.Sum(a => a.PlacedPriceDecimal * a.Quantity) - order.SalesTotal).ToString("C"),
                    Total = order.SalesTotal.ToString("C"),
                    CreatedDate = order.CreatedDateTime,
                    OrderFound = true,
                    Status = GetOrderStatusDescription(order.SalesStatus),
                    Modified = order.ChangeDate.GetValueOrDefault(DateTime.Today),
                    Order = new OrderListViewModel
                    {
                        OrderDate = order.CreatedDateTime,
                        OrderNumber = order.OrderNumber,
                        OrderNumbers = GetOrderNumbersFromItems(order.LineItems, order.OrderNumber),
                        OrderStatus = GetOrderStatusDescription(order.SalesStatus),
                        SalesTotal = order.SalesTotal,
                        OrderDispatchedTo = GetOrderDispatchTo(order),
                        OrderItems = GetOrderItems(order.LineItems)
                    }
                };

                return purchaseOrderViewModel;
            }
        }
        
        public void TransferGuestOrderToNewlyCreatedUser(CustomerContact currentContact, string oldContactId)
        {
            using (var db = new OrdersContext(StringConstants.TrmCustomDatabaseName))
            {
                var order = db.Orders.FirstOrDefault(a => a.EpiServerCustomerRef.ToLower() == oldContactId);
                if (order == null) return;
                order.EpiServerCustomerRef = currentContact.UserId;
                db.SaveChanges();
            }
        }

        public IPurchaseOrder GetLastOrder(Guid contactId)
        {
            return _orderRepository.Load<IPurchaseOrder>(contactId, "Default").OrderByDescending(o => o.Created).FirstOrDefault();
        }

        private string GetOrderDispatchTo(Order order)
        {
            return $"{order.DeliveryName}, {order.DeliveryStreet} {order.DeliveryCity} {order.DeliveryPostCode}";
        }
        private IEnumerable<OrderItem> GetOrderItems(ICollection<OrderLine> items)
        {
            foreach (var item in items)
            {
                var variantLink = _referenceConverter.GetContentLink(item.ItemId);
                TrmVariant variant = null;
                if (variantLink != null && variantLink.ID > 0)
                    variant = _contentRepository.Get<TrmVariant>(variantLink);

                yield return new OrderItem
                {
                    DisplayName = variant?.DisplayName ?? item.ItemName,
                    SubTitle = variant?.SubDisplayName ?? string.Empty,
                    Code = item.ItemId,
                    Quantity = item.SalesQty,
                    HasbeenPersonalised = item.HasbeenPersonalised,
                    PlacedPriceDecimal = item.SalesPrice,
                    SalesStatus = GetOrderStatusDescription(item.SalesStatus),
                    ImageUrl = _assetHelper.GetDefaultAssetUrl(variant?.ContentLink)
                };
            }
        }
        private IEnumerable<CartItemViewModel> GetCartOrderItems(ICollection<OrderLine> items)
        {
            foreach (var item in items)
            {
                var variantLink = _referenceConverter.GetContentLink(item.ItemId);
                TrmVariant variant = null;
                var stockSummary = new StockSummaryDto();
                if (variantLink != null && variantLink.ID > 0)
                {
                    variant = _contentRepository.Get<TrmVariant>(variantLink);
                    stockSummary = _inventoryHelper.GetStockSummary(variantLink);
                }

                yield return new CartItemViewModel
                {
                    DisplayName = variant?.DisplayName ?? item.ItemId,
                    SubTitle = variant?.SubDisplayName ?? string.Empty,
                    Code = item.ItemId,
                    Quantity = item.SalesQty,
                    HasbeenPersonalised = item.HasbeenPersonalised,
                    PlacedPriceDecimal = item.SalesPrice,
                    SalesStatus = GetOrderStatusDescription(item.SalesStatus),
                    StockSummary = stockSummary,
                    BrandDisplayName = variant?.BrandDisplayName ?? string.Empty,
                    CategoryName = variant?.Category ?? string.Empty,
                };
            }
        }
        private string GetOrderStatusDescription(string salesStatus)
        {
            //Status of 0 and 1 both equal a Order Status of Open
            if (salesStatus == 0.ToString())
            {
                salesStatus = ((int)Enums.eOrderStatus.Open).ToString();
            }

            Enums.eOrderStatus orderStatus;

            if (!Enum.TryParse(salesStatus, out orderStatus)) return salesStatus;

            salesStatus = _localizationService.GetStringByCulture(
                string.Format(StringResources.OrderStatus, orderStatus), orderStatus.DescriptionAttr(),
                ContentLanguage.PreferredCulture);

            return salesStatus;
        }
        
        public void UpdatePurchaseOrderWhenPampQuoteSuccess(IPurchaseOrder purchaseOrder, ExecuteResponse finishQuoteResult)
        {
            purchaseOrder.OrderStatus = OrderStatus.InProgress;
            //purchaseOrder.OrderStatus = OrderStatus.Completed;
            purchaseOrder.Properties[StringConstants.CustomFields.BullionQuotationTradeReferenceId] = finishQuoteResult.TradeReferenceId;
            purchaseOrder.Properties[StringConstants.CustomFields.BullionPAMPTradeReferenceId] = finishQuoteResult.TradeReferenceId;
            purchaseOrder.Properties[StringConstants.CustomFields.BullionPAMPStatus] = PricingAndTradingService.Models.Constants.PampExecuteOnQuoteStatus.Success.ToString();
            _orderRepository.Save(purchaseOrder);
        }
        
        public void UpdatePurchaseOrderWhenPampQuoteRejected(IPurchaseOrder purchaseOrder)
        {
            purchaseOrder.OrderStatus = OrderStatus.Cancelled;
            purchaseOrder.Properties[StringConstants.CustomFields.BullionPAMPStatus] = PricingAndTradingService.Models.Constants.PampFinishQuoteStatus.Rejected.ToString();
            _orderRepository.Save(purchaseOrder);
        }

        public void CloseOrder(string orderId)
        {
            UpdateOrderOpenStatus(orderId, false);
        }
        
        public void OpenOrder(string orderId)
        {
            UpdateOrderOpenStatus(orderId, true);
        }

        private void UpdateOrderOpenStatus(string orderId, bool isOpen)
        {
            using (var db = new OrdersContext(StringConstants.TrmCustomDatabaseName))
            {
                var order = db.Orders.Find(Guid.Parse(orderId));
                if (order != null)
                {
                    order.IsOpenOrder = isOpen;
                    db.SaveChanges();
                }
            }
        }

        #region Export Transaction

        public void SavePurchaseOrdersExportTransaction(IPurchaseOrder purchaseOrder, string integrationStatus = "")
        {
            SavePurchaseOrdersExportTransaction(purchaseOrder, null, string.Empty);
        }

        public void SavePurchaseOrdersExportTransaction(IPurchaseOrder purchaseOrder, CustomerContact customerContact, string integrationStatus = "")
        {
            try
            {
                var po = purchaseOrder as PurchaseOrder;
                if (po == null) return;

                var purchaseOrdersExportTransactionPayload = _getOrdersService.BuildPurchaseOrdersExportTransaction(po, customerContact);
                var customerId = customerContact != null ? ((Guid)customerContact.PrimaryKeyId).ToString() : _customerContext.CurrentContactId.ToString();

                ExportTransaction exportTransaction;
                _exportTransactionService.CreateExportTransaction(purchaseOrdersExportTransactionPayload, customerId, IntegrationServices.Constants.ExportTransactionType.PurchaseOrders, out exportTransaction, customerContact);

                if (exportTransaction != null)
                {
                    UserDetails userDetails;
                    if (_userService.IsImpersonating())
                        userDetails = UserDetails.ForImpersonator(CustomerContext.Current, RequestHelper.GetClientIpAddress());
                    else
                        userDetails = UserDetails.ForCustomer(CustomerContext.Current, RequestHelper.GetClientIpAddress());

                    var impersonationLog = ImpersonationLog.ForExportTransaction(
                        userDetails,
                        exportTransaction,
                        po.TrackingNumber);
                    _impersonationLogService.CreateLog(impersonationLog);

                    if (!string.IsNullOrEmpty(integrationStatus))
                    {
                        exportTransaction.IntegrationStatus = integrationStatus;
                        _exportTransactionService.AddOrUpdateExportTransaction(exportTransaction);
                    }

                    purchaseOrder.Properties[StringConstants.CustomFields.ExportTransactionId] = exportTransaction.TransactionId;
                    _orderRepository.Save(purchaseOrder);
                }

            }
            catch (Exception ex)
            {
                _logger.Log(Level.Error,
                    "Unable to write a purchase order to the Export Transaction. Check that there are no pending data migrations or schema changes",
                    ex);
            }
        }

        #endregion
    }
}
