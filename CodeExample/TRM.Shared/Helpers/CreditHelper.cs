using Mediachase.Commerce.Customers;
using System;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;

namespace TRM.Shared.Helpers
{
    public class CreditHelper : IAmCreditHelper
    {
        private readonly CustomerContext _customerContext;

        public CreditHelper(CustomerContext customerContext)
        {
            _customerContext = customerContext;
        }

        public bool HasEnoughCredit(decimal amount)
        {
            var available = GetAvailableCredit();

            return amount <= available;
        }
        
        public bool DeductCredit(decimal amountToDeduct)
        {
            return DeductCredit(amountToDeduct, null);
        }
        public bool DeductCredit(decimal amountToDeduct, CustomerContact customerContact)
        {

            var hasEnoughCredit = HasEnoughCredit(amountToDeduct);

            if (hasEnoughCredit == false) return false;
                 
            var customer = customerContact ?? _customerContext.CurrentContact;

            var used = GetUsedCredit(customer);

            customer.Properties[StringConstants.CustomFields.CreditUsedEPiFieldName].Value = used + amountToDeduct;

            customer.SaveChanges();

            return true;

        }

        public bool HasCreditAccount()
        {
            var customer = _customerContext.CurrentContact;
            var limit = customer.GetDecimalProperty(StringConstants.CustomFields.CreditLimitFieldName);
            
            //When credit limit is less than 500 treat as zero RM-185
            if (limit < 500) limit = decimal.Zero;

            return limit > 0;
        }
        
        public decimal GetAvailableCredit()
        {
            var customer = _customerContext.CurrentContact;
            
            var limit = customer.GetDecimalProperty(StringConstants.CustomFields.CreditLimitFieldName);

            //When credit limit is less than 500 treat as zero RM-185
            if(limit < 500) limit = decimal.Zero;

            var used = GetUsedCredit(customer);

            return (limit - used);
        }

        public decimal GetUsedCredit(CustomerContact customer)
        {
            return customer.GetDecimalProperty(StringConstants.CustomFields.CreditUsedEPiFieldName);
        }

        public bool HasEnoughBalances(decimal amount, CustomerContact customerContact = null)
        {
            var currentCustomer = customerContact ?? _customerContext.CurrentContact;

            if (currentCustomer == null) return false;

            var available = currentCustomer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend);

            return amount <= available;
        }

        public bool DeductBalances(decimal amountToDeduct)
        {
            var customer = _customerContext.CurrentContact;

            var effectiveBalance = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);
            var availableToSpend = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend);
            var availableToWithdraw = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToWithdraw);
            var lifetimeValue = customer.GetDecimalProperty(StringConstants.CustomFields.LifetimeValue);

            customer.Properties[StringConstants.CustomFields.BullionCustomerEffectiveBalance].Value = Math.Round(effectiveBalance - amountToDeduct, 2);
            customer.Properties[StringConstants.CustomFields.BullionCustomerAvailableToSpend].Value = Math.Round(availableToSpend - amountToDeduct, 2);
            customer.Properties[StringConstants.CustomFields.BullionCustomerAvailableToWithdraw].Value = Math.Round(availableToWithdraw - amountToDeduct, 2);
            customer.Properties[StringConstants.CustomFields.LifetimeValue].Value = Math.Round(lifetimeValue - amountToDeduct, 2);
            customer.SaveChanges();

            return true;
        }
    }
}