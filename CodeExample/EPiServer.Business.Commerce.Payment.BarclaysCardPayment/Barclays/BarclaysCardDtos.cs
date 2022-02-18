using System;
using Newtonsoft.Json;
using TRM.Shared.Models.DTOs.Payments;

namespace EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays
{
    // ReSharper disable once InconsistentNaming
    public class threeDSResult
    {
        [JsonProperty(PropertyName = "3DSecure")]
        public ThreeDSecure threeDSecure { get; set; }
        [JsonProperty(PropertyName = "3DSecureId")]
        public string threeDSecureId { get; set; }
        public string merchant { get; set; }
        public Response response { get; set; }
        public session session { get; set; }
    }

    public class PaymentTransactionResponse
    {
        [JsonProperty(PropertyName = "order")]
        public Order OrderObject { get; set; }
        [JsonProperty(PropertyName = "response")]
        public Response ResponseObject { get; set; }
        [JsonProperty(PropertyName = "result")]
        public string Result { get; set; }
        [JsonProperty(PropertyName = "merchant")]
        public string Merchant { get; set; }
        [JsonProperty(PropertyName = "transaction")]
        public Transaction TransactionObject { get; set; }
        [JsonProperty(PropertyName = "sourceOfFunds")]
        public SourceOfFunds SourceOfFundsObject { get; set; }
        [JsonProperty(PropertyName = "timeOfRecord")]
        public string TimeOfRecord { get; set; }
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }
    }

    public class PaymentTransaction
    {
        [JsonProperty(PropertyName = "session")]
        public session SessionObject { get; set; }
        [JsonProperty(PropertyName = "billing")]
        public Billing BillingObject { get; set; }
        [JsonProperty(PropertyName = "apiOperation")]
        public string ApiOperation { get; set; }
        [JsonProperty(PropertyName = "customer")]
        public Customer Customer { get; set; }
        [JsonProperty(PropertyName = "order")]
        public Order Order { get; set; }
        [JsonProperty(PropertyName = "sourceOfFunds")]
        public SourceOfFunds SourceOfFundsObject { get; set; }
        [JsonProperty(PropertyName = "3DSecureId")]
        public string threeDSecureId { get; set; }
    }

    public class Check3DsEnrollment
    {
        public session session { get; set; }
        public Order order { get; set; }
        [JsonProperty(PropertyName = "3DSecure")]
        public ThreeDSecure threeDSecure { get; set; }

        public string apiOperation { get; set; }

        [JsonProperty(PropertyName = "sourceOfFunds")]
        public SourceOfFunds SourceOfFunds { get; set; }
    }

   public class CardSecurityCode
    {
        [JsonProperty(PropertyName = "acquirerCode")]
        public string AcquirerCode { get; set; }
        [JsonProperty(PropertyName = "gatewayCode")]
        public string GatewayCode { get; set; }
    }
    
    public class CardholderVerification
    {
        [JsonProperty(PropertyName = "avs")]
        public CardSecurityCode Avs { get; set; }
    }

    public class Risk
    {
        [JsonProperty(PropertyName = "response")]
        public CardSecurityCode Response { get; set; }
        [JsonProperty(PropertyName = "review")]
        public RiskReview Review { get; set; }
        [JsonProperty(PropertyName = "rule")]
        public Rule[] Rule { get; set; }
    }

    public class RiskReview
    {
        [JsonProperty(PropertyName = "decision")]
        public string Decision { get; set; }
    }

    
    public class Rule
    { 
        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "recommendation")]
        public string Recommendation { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
    
    public class Process3DsResult
    {
        [JsonProperty(PropertyName = "3DSecure")]
        public ThreeDSecure threeDSecure { get; set; }
        public string apiOperation { get; set; }
    }

    public class Order
    {
        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; set; }
        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }
        [JsonProperty(PropertyName = "totalCapturedAmount")]
        public string TotalCapturedAmount { get; set; }
        [JsonProperty(PropertyName = "totalAuthorizedAmount")]
        public string TotalAuthorizedAmount { get; set; }
        [JsonProperty(PropertyName = "totalRefundedAmount")]
        public string TotalRefundedAmount { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "result")]
        public string Result { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "response")]
        public Response ResponseObject { get; set; }
    }

    public class Response
    {
        [JsonProperty(PropertyName = "acquirerCode")]
        public string AcquirerCode { get; set; }
        [JsonProperty(PropertyName = "acquirerMessage")]
        public string AcquirerMessage { get; set; }
        [JsonProperty(PropertyName = "cardSecurityCode")]
        public CardSecurityCode CardSecurityCode { get; set; }
        [JsonProperty(PropertyName = "cardholderVerification")]
        public CardholderVerification CardholderVerification { get; set; }
        [JsonProperty(PropertyName = "gatewayCode")]
        public string GatewayCode { get; set; }
        [JsonProperty(PropertyName = "3DSecure")]
        public ThreeDSecure threeDSecure { get; set; }
    }

    public class session
    {
        public string id { get; set; }
    }

    public class authenticationRedirect
    {
        public string responseUrl { get; set; }
        public SimpleHtml simple { get; set; }
    }

    public class ThreeDSecure
    {
        public authenticationRedirect authenticationRedirect { get; set; }
        public string gatewayCode { get; set; }
        public string summaryStatus { get; set; }
        public string xid { get; set; }
        [JsonProperty(PropertyName = "3DSecureId")]
        public string threeDSecureId { get; set; }
        public string merchant { get; set; }
        public Response response { get; set; }
        public string authenticationToken { get; set; }
        public string paRes { get; set; }
        public string acsEci { get; set; }
    }

    public class SimpleHtml
    {
        public string htmlBodyContent { get; set; }
    }

    public class Billing
    {
        public Address address { get; set; }
    }

    public class Address : MastercardPaymentAddressDto
    {
    }

    public class Customer
    {
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string mobilePhone { get; set; }
        public string phone { get; set; }
    }

    public class SourceOfFunds
    {
        public string token { get; set; }
        public string type { get; set; }
        public CardProvided provided { get; set; }
    }

    public class Transaction
    {
        public Acquirer acquirer { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string authorizationCode { get; set; }
        public string receipt { get; set; }
        public string source { get; set; }
        public string terminal { get; set; }
    }

    public class Acquirer
    {
        public string id { get; set; }
    }

    public class CardProvided
    {
        public Card card { get; set; }
    }

    public class Card
    {
        public string brand { get; set; }
        public Expiry expiry { get; set; }
        public string fundingMethod { get; set; }
        public string issuer { get; set; }
        public string number { get; set; }
        public string scheme { get; set; }
        public string nameOnCard { get; set; }
    }

    public class Expiry
    {
        public string month { get; set; }
        public string year { get; set; }
    }

    public class ErrorResponse
    {
        public Error error { get; set; }
        public string result { get; set; }
    }

    public class Error
    {
        public string cause { get; set; }
        public string explanation { get; set; }
        public string field { get; set; }
        public string supportCode { get; set; }
        public string validationType { get; set; }
    }

    public class Tokenization
    {
        public session session { get; set; }
        public SourceOfFundsForTokenization sourceOfFunds { get; set; }
        public string result { get; set; }
        public string status { get; set; }
        public string token { get; set; }
        public string verificationStrategy { get; set; }
        public string repositoryId { get; set; }
        public Usage usage { get; set; }
    }

    public class Usage
    {
        public DateTime lastUpdated { get; set; }
        public DateTime lastUsed { get; set; }
        public string lastUpdatedBy { get; set; }
    }

    public class SourceOfFundsForTokenization
    {
        public string token { get; set; }
        public string type { get; set; }
        public CardProvidedForTokenization provided { get; set; }
    }
    public class CardProvidedForTokenization
    {
        public CardForTokenization card { get; set; }
    }
    public class CardForTokenization
    {
        public string brand { get; set; }
        public string expiry { get; set; }
        public string fundingMethod { get; set; }
        public string issuer { get; set; }
        public string number { get; set; }
        public string scheme { get; set; }
        public string nameOnCard { get; set; }
    }
}
