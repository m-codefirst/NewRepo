using Mediachase.Commerce.Customers;

namespace TRM.Shared.Helpers
{
    public interface IAmCreditHelper
    {
        bool HasEnoughCredit(decimal amount);
        bool DeductCredit(decimal amountToDeduct);
        bool DeductCredit(decimal amountToDeduct, CustomerContact customerContact);
        decimal GetAvailableCredit();
        bool HasCreditAccount();
        decimal GetUsedCredit(CustomerContact customer);
        bool HasEnoughBalances(decimal amount, CustomerContact customerContact = null);
        bool DeductBalances(decimal amountToDeduct);
    }
}