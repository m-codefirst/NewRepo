using TRM.Web.Models.ViewModels.Bullion.CustomerBankAccount;

namespace TRM.Web.Services
{
    public interface IBankAccountValidationService
    {
        bool ValidateUkBankAccount(AddOrEditBankAccountViewModel viewModel, out bool invalidSortCode, out bool invalidAccountNumber);
        string ValidateIBan(AddOrEditBankAccountViewModel viewModel);
        bool ValidateSortCode(string sortCode);
    }
}