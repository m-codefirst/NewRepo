using System;
using EPiServer.Commerce.Order;
using TRM.Shared.Constants;
using TRM.Shared.Interfaces;

namespace TRM.Shared.Models.DTOs.Payments
{
    public class ManualPaymentDto: IAmTokenisablePayment
    {
        public string Mastercard3DSecureId { get; set; }
        public string Mastercard3DsVtSid { get; set; }
        public string Mastercard3DsVavid { get; set; }
        public string Mastercard3DsSid { get; set; }
        public string Mastercard3DsEci { get; set; }
        public string Card3DsHtmlContent { get; set; }
        public int PaymentId { get; set; }
        public string PaymentMethodName { get; set; }
        public string TransactionId { get; set; }
        public string OrderNumber { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string NameOnCard { get; set; }
        public string MastercardSessionId { get; set; }
        public bool IsAmexPayment { get; set; }
        public string MastercardCardNumber { get; set; }
        public string MastercardCardType { get; set; }
        public string MastercardCardExpiry { get; set; }
        public string TokenisedCardNumber { get; set; }
        public MastercardPaymentAddressDto BillingAddress { get; set; }
        public Guid PaymentMethodId { get; set; }
        public bool SaveCard { get; set; }
        public string SelectedCard { get; set; }
        public CreditCardDto CardToUse { get; set; }

        public bool PaymentSuccessful { get; set; }
        public string Message { get; set; }
        public string AuthorizationCode { get; set; }
        public bool PaymentRequires3DsRedirect { get; set; }
        public string ResponseUrl { get; set; }
        public string CaptureStatus { get; set; }
        public string CaptureTotalAuthorizedAmount { get; set; }
        public string CaptureTotalCapturedAmount { get; set; }
        public string CaptureAcquirerMessage { get; set; }
        public string CaptureAuthorizationCode { get; set; }
        public string CaptureTransactionReceipt { get; set; }
        public string MastercardCardReceipt { get; set; }
        public string Mastercard3DsStatus { get; set; }
        public bool PaymentComplete { get; set; }
        public bool CapturePayment { get; set; }
        public OrderReference OrderGroupReference { get; set; }
        public Guid CustomerId { get; set; }
        public string Customer3dsTransactionId { get; set; }
        public Enums.TrmPaymentType PaymentType { get; set; }
    }
}
