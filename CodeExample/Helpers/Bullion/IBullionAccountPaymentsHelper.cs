using System;
using EPiServer.Business.Commerce.Payment.Mastercard.Mastercard;
using Mediachase.Commerce.Customers;
using TRM.Shared.Models.DTOs.Payments;

namespace TRM.Web.Helpers.Bullion
{
    public interface IBullionAccountPaymentsHelper
    {
        decimal UpdateCustomerBalances(CustomerContact customer, decimal amount);
        bool CreateFundTransactionHistory(ManualPaymentDto dto, string exportTransactionId, string clientIpAddress = "");
        string CreateExportTransaction(ManualPaymentDto dto, Tokenization token, string lastFour);
    }
}