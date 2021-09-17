using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using EPiServer.Logging.Compatibility;
using TRM.Web.Models.DDS;
using TRM.Web.Models.EntityFramework.CustomerBankAccounts;

namespace TRM.Web.Helpers.Bullion
{
    public class CustomerBankAccountHelper : ICustomerBankAccountHelper
    {
        private readonly  ILog _logger = LogManager.GetLogger(typeof(CustomerBankAccountHelper));
        private readonly IAmCountryHelper _countryHelper;
        public CustomerBankAccountHelper(IAmCountryHelper countryHelper)
        {
            _countryHelper = countryHelper;
        }
        public CustomerBankAccount SaveAccount(string customerId, string countryCode, string bankName,
            string accountHolderName, string nickname, Guid customerBankAccountId, string bankAccountInformation)
        {
            var bankAccount = new CustomerBankAccount
            {
                AccountHolderName = accountHolderName,
                BankName = bankName,
                CountryCode = countryCode,
                CustomerBankAccountId = customerBankAccountId,
                CustomerId = customerId,
                Deleted = false,
                Nickname = nickname,
                AccountInformation = bankAccountInformation,
                Created = DateTime.Now
            };
            return SaveChanges(bankAccount);
        }

        public CustomerBankAccount DeleteCustomerBankAccount(Guid existingCustomerBankAccountId, out string message)
        {
            try
            {
                message = string.Empty;
                using (var db = new CustomerBankAccountsContext())
                {
                    var bankAccount = db.CustomerBankAccounts.FirstOrDefault(x => x.CustomerBankAccountId == existingCustomerBankAccountId);
                    if (bankAccount != null)
                    {
                        bankAccount.Deleted = true;
                        return SaveChanges(bankAccount);
                    }
                }

                return null;
            }
            catch (Exception err)
            {
                _logger.Error(err.Message, err);
                message = err.Message;
                return null;
            }
        }

        private CustomerBankAccount SaveChanges(CustomerBankAccount bankAccount)
        {
            using (var db = new CustomerBankAccountsContext())
            {
                db.CustomerBankAccounts.AddOrUpdate(bankAccount);
                db.SaveChanges();
            }

            return bankAccount;
        }

        public List<CustomerBankAccount> GetActiveCustomerBankAccounts(string customerId)
        {
            if (string.IsNullOrEmpty(customerId)) return new List<CustomerBankAccount>();

            using (var db = new CustomerBankAccountsContext())
            {
                return db.CustomerBankAccounts.Where(x => x.CustomerId == customerId && !x.Deleted).OrderBy(x => x.AccountHolderName).ToList();
            }
        }

        public CustomerBankAccount GetActiveCustomerBankAccountById(string customerId, Guid bankId)
        {
            if (string.IsNullOrEmpty(customerId)) return new CustomerBankAccount();

            using (var db = new CustomerBankAccountsContext())
            {
                return db.CustomerBankAccounts.FirstOrDefault(x => x.CustomerId == customerId && x.CustomerBankAccountId == bankId && !x.Deleted);
            }
        }

        public string GetBankAccountInformation(string countryCode, string sortCode, string iBan, string accountNumber, string swiftOrBic)
        {
            var countryZone = _countryHelper.GetCountry(countryCode)?.Zone ?? CountryZone.UK;
            switch (countryZone)
            {
                case CountryZone.UK:
                    return $"Sort Code {sortCode} <br/>Account ******{accountNumber.Substring(accountNumber.Length - 2, 2)}";
                case CountryZone.EUROPE:
                    return $"{iBan.Substring(0, 8)}*********{iBan.Substring(iBan.Length - 2, 2)}";
                default:
                    return $"Swift Code {swiftOrBic} <br/>Account ******{accountNumber.Substring(accountNumber.Length - 2, 2)}";
            }
        }
    }
}