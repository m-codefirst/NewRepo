using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Shared.Models.DTOs;

namespace TRM.Web.Helpers
{
    public interface IAmBullionContactHelper
    {
        bool IsConsumerAccount(CustomerContact contact);
        bool IsConsumerAccountOnly(CustomerContact contact);
        bool IsBullionAccountOnly(CustomerContact contact);
        bool IsMixedAccount(CustomerContact contact);
        bool IsCustomerServiceAccount(CustomerContact contact);
        string GetBullionCustomerType(CustomerContact contact);
        string GetBullionPremiumGroup(CustomerContact contact);
        bool IsSippContact(CustomerContact contact);
        CustomerAddress GetBullionAddress(CustomerContact contact);
        bool UpdateCustomerKycStatus(CustomerContact contact, KycQueryResultDto kycQueryResultDto);
        Money GetMoneyAvailableToInvest(CustomerContact contact);
        bool HasFailedStage1(CustomerContact contact);
        bool HasFailedStage2(CustomerContact contact);
        bool IsKycStatusApproved(CustomerContact customerContact);
        bool KycRefered(CustomerContact contact);
        bool KycRejected(CustomerContact contact);
        string GetUsername(CustomerContact contact);
        string GetFullname(CustomerContact customer);
        string GetBeneficiaryReference(CustomerContact contact);
        decimal GetEffectiveBalance(CustomerContact contact);
        void UpdateBalances(CustomerContact contact, decimal effectiveBalanceAmount);
        void UpdateBalances(CustomerContact contact, decimal effectiveBalanceAmount, decimal availableToSpendAmount, decimal availableToWithdrawAmount);
        void UpdateLifeTimeBalance(CustomerContact contact, decimal lifetimeValueBalanceAmount);
        bool IsPensionProvider(CustomerContact contact);
        string GetDefaultCurrencyCode(CustomerContact currentCustomer);
        bool IsBullionAccount(CustomerContact contact);
        bool IsBullionCustomerType(string customerCustomerCustomerType);
    }
}