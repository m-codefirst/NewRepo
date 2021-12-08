using System;
using AuthorizeNet;

namespace TRM.Shared.Models.DTOs
{
    public enum TransactionType
    {
        Mastercard,
        Pamp
    }

    public enum ThirdPartyTransactionStatus
    {
        Pending,
        Success,
        Error
    }

    public class ThirdPartyTransaction
    {
        public string Id { get; set; }
        public TransactionType TransactionType { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime LastChanged { get; set; }
        public string TransactionPayloadJson { get; set; }
        public ThirdPartyTransactionStatus TransactionStatus { get; set; }
    }
}
