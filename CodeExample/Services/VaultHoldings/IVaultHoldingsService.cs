using Mediachase.Commerce.Customers;

namespace TRM.Web.Services.VaultHoldings
{
    public interface IVaultHoldingsService
    {
        VaultHoldingsDto GetVaultHoldings(CustomerContact contact);
    }
}