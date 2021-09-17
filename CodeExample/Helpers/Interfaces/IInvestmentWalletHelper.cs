using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.ViewModels.Bullion;

namespace TRM.Web.Helpers.Interfaces
{
    public interface IInvestmentWalletHelper
    {
        Money GetEffectiveBalanceByCustomerContact(CustomerContact customerContact);
        Money GetAvailableToInvestByCustomerContact(CustomerContact customerContact);
        Money GetAvailableToWithdrawByCustomerContact(CustomerContact customerContact);
        string GetBullionAddFundUrlByCustomerContact(CustomerContact customerContact);
        string GetManageFundByCustomerContact(CustomerContact customerContact);
        bool IsSippContactByCustomerContact(CustomerContact customerContact);
        void PopulateInvestmentWalletViewModel(InvestmentWalletViewModel investmentWalletViewModel, CustomerContact customerContact);
    }
}
