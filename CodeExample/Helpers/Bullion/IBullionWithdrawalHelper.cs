using System;
using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.ViewModels.Bullion.CustomerBankAccount;
using TRM.Web.Models.ViewModels.Bullion.RequestStandardWithdrawal;

namespace TRM.Web.Helpers.Bullion
{
    public interface IBullionWithdrawalHelper
    {
        WithdrawalFundsDto WithdrawFunds(CustomerContact customer, Guid contactId, decimal amountToWithdraw, BankAccountViewModel bankAccount, string orderNumberPrefix, RequestWithdrawalPaymentType paymentType, out decimal customerAvailableToWithdraw);
        BullionBankWithdrawalFeeBlock GetWithdrawalFee(CustomerContact customer, BankAccountViewModel bankAccount, bool quickWithdrawal, out string message);
        List<RequestWithdrawalPaymentType> GetListRequestWithdrawalPaymentTypes();
        RequestWithdrawalPaymentType GetWithdrawalPaymentType(string paymentName);
        decimal GetWithdrawalVatRate(BankAccountViewModel bankAccount);
        decimal GetWithdrawalVat(BankAccountViewModel bankAccount, BullionBankWithdrawalFeeBlock withdrawalFee);
    }
}