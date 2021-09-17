using System;
using EPiServer.Commerce.Order;
using System.Collections.Generic;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Services;
using Mediachase.Commerce.Customers;

namespace TRM.Web.Helpers
{
    public interface IAmTransactionHistoryHelper
    {
        Dictionary<Enums.TransactionHistoryType, string> GetTransactionFilterOptions();
        decimal GetTransactionHistoricalSpend(string userId, DateTime since);
        IEnumerable<TransactionHistoryItemViewModel> GetTransactionHistories(
            string customerId,
            Enums.TransactionHistoryType filter,
            int currentPageNumber,
            int pageSize,
            out int totalRecords);
      
        bool LogTheAddFundTransaction(decimal amount, decimal newCustomerAvailableBalance,
            string currencyCode, string orderNumber, string exportTransactionId, string clientIpAddress = "");
        bool LogThePurchaseTransaction(IPurchaseOrder purchaseOrder, string clientIpAddress = "");
        bool LogThePurchaseTransaction(IPurchaseOrder purchaseOrder, CustomerContact customerContact, string clientIpAddress = "");
        bool LogTheSellFromVaultTransactionHistory(TrmSellTransaction sellTransaction);
        bool LogTheDeliveryFromVaultTransactionHistory(TrmDeliverTransaction deliverTransaction);
        bool LogTheWithdrawTransactionHistory(string bankAccountId, string bankAccountNickName, decimal amountIncludingFee,
            string withdrawMethodName, decimal withdrawFee, string currencyCode, string exportTransactionId);
        bool ImportTransactionHistoryFromAx(AxImportData.AxTransactionItem transactionHistory, CustomerContactViewModel contact);
        bool UpdateTransactionLineItemStatusFromAx(AxImportData.TransactionStatusUpdateCustomerItem transactionStatusUpdateItem, Guid contactId, out Guid transactionHistoryId);
        bool HasTransactionHistory(string customerId);
        bool ShouldNotUpdateBalance(string customerId);
        bool UpdateTransactionHistoryStatus(IEnumerable<Guid> transactionIds);
        IEnumerable<DormentFundsItem> GetCustomerByDormantFunds(int dormentDays);
    }
}