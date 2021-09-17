using System;
using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.CustomerBankAccounts;

namespace TRM.Web.Helpers.Bullion
{
    public interface ICustomerBankAccountHelper
    {
        CustomerBankAccount GetActiveCustomerBankAccountById(string customerId, Guid bankId);
        List<CustomerBankAccount> GetActiveCustomerBankAccounts(string customerId);
        CustomerBankAccount SaveAccount(string customerId, string countryCode, string bankName, string accountHolderName, string nickname, Guid customerBankAccountId, string bankAccountInformation);
        CustomerBankAccount DeleteCustomerBankAccount(Guid existingCustomerBankAccountId, out string message);
        string GetBankAccountInformation(string countryCode, string sortCode, string iBan, string accountNumber, string swiftOrBic);
    }
}