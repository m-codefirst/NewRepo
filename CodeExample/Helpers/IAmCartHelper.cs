using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders.Dto;
using TRM.Shared.Interfaces;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.ViewModels.Cart;

namespace TRM.Web.Helpers
{
    public interface IAmCartHelper
    {
        MixedOrderGroupViewModel GetMixedLargeCart();
        List<ShipmentViewModel> GetInitialCart();
        MixedOrderGroupViewModel GetMixedMiniCart();

        LargeOrderGroupViewModel GetLargeCartViewModel(string cartName);

        LargeOrderGroupViewModel GetLargeCartViewModel(IOrderGroup cart, Dictionary<ILineItem, List<ValidationIssue>> validationIssues);

        CartSummaryViewModel GetCartSummaryViewModel();

        PurchaseOrderViewModel GetPurchaseOrderViewModel(IPurchaseOrder purchaseOrder);
        PurchaseOrderViewModel GetPurchaseOrderViewModel(IPurchaseOrder purchaseOrder, CustomerContact customerContact);

        string GetValidationMessages(Dictionary<ILineItem, List<ValidationIssue>> validationResults);
        bool ValidateCart(ICart cart, out string cartValidationMessages);
        bool ValidateCart(ICart cart, out string cartValidationMessages, CustomerContact customerContact);

        ProcessPaymentForBullionCartResponse ProcessPaymentForBullionCart(IOrderGroup cart, string orderNumberPrefix, CustomerContact customerContact = null);

        IPurchaseOrder ConvertToPurchaseOrder(ICart cart, string orderNumberPrefix);
        IPurchaseOrder ConvertToPurchaseOrder(ICart cart, string orderNumberPrefix, CustomerContact customerContact);

        void RemovePreviousPayments(IOrderGroup order);
        void RemovePreviousPayments(IOrderGroup order, CustomerContact customerContact);

        bool CreatePayment(IOrderGroup cart, PaymentMethodDto.PaymentMethodRow paymentMethod, string orderNumber,
            IOrderAddress billingAddress, BasePaymentDto paymentDto, out string messages);

        bool AddCouponCode(ICart cart, string couponCode, out Enums.ePromotionValidation validation,
            bool checkApplied = true);

        bool RemoveCouponCode(ICart cart, string promotionCode, out Enums.ePromotionValidation validation);

        bool AddCouponCode(string promotionCode);

        bool GetCardToken(PaymentMethodDto.PaymentMethodRow paymentMethod, IAmTokenisablePayment payment,
            ref string message);

        bool IsItemExistingInCart(string code, string cartName);
        int GetCartSummaryItemsCount();
        Money GetLivePrice(ILineItem lineItem, Currency currency);
        bool HasRetryWithPampExportTransaction();
        bool HasRetryWithPampExportTransaction(CustomerContact customerContact);
    }
}