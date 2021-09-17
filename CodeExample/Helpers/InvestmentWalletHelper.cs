using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Extentions;
using TRM.Web.Models.Pages;
using EPiServer.Web;
using EPiServer;
using Hephaestus.Commerce.Shared.Services;

namespace TRM.Web.Helpers
{
    public class InvestmentWalletHelper : IInvestmentWalletHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly ICurrencyService _currencyService;

        public InvestmentWalletHelper(IContentLoader contentLoader, IAmBullionContactHelper bullionContactHelper, ICurrencyService currencyService)
        {
            _contentLoader = contentLoader;
            _bullionContactHelper = bullionContactHelper;
            _currencyService = currencyService;
        }

        public Money GetEffectiveBalanceByCustomerContact(CustomerContact customerContact)
        {
            var effectiveBalance = customerContact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);

            return new Money(effectiveBalance, GetCurrentCurrency(customerContact));
        }
        public Money GetAvailableToInvestByCustomerContact(CustomerContact customerContact)
        {
            var effectiveBalance = GetPositiveDecimal(customerContact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend));

            return new Money(effectiveBalance, GetCurrentCurrency(customerContact));
        }
        public Money GetAvailableToWithdrawByCustomerContact(CustomerContact customerContact)
        {
            var effectiveBalance = GetPositiveDecimal(customerContact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance));

            return new Money(effectiveBalance, GetCurrentCurrency(customerContact));
        }
        public string GetBullionAddFundUrlByCustomerContact(CustomerContact customerContact)
        {
            return customerContact.GetBullionAccountAddCreditPage();
        }
        public string GetManageFundByCustomerContact(CustomerContact customerContact)
        {
            return customerContact.GetManageFundsPageUrl();
        }
        public bool IsSippContactByCustomerContact(CustomerContact customerContact)
        {
            return _bullionContactHelper.IsSippContact(customerContact);
        }
        public void PopulateInvestmentWalletViewModel(InvestmentWalletViewModel investmentWalletViewModel, CustomerContact customerContact)
        {
            investmentWalletViewModel.EffectiveBalance = GetEffectiveBalanceByCustomerContact(customerContact);
            investmentWalletViewModel.AvailableToInvest = GetAvailableToInvestByCustomerContact(customerContact);
            investmentWalletViewModel.AvailableToWithdraw = GetAvailableToWithdrawByCustomerContact(customerContact);
            investmentWalletViewModel.BullionAddFundUrl = GetBullionAddFundUrlByCustomerContact(customerContact);
            investmentWalletViewModel.BullionWithdrawFundsUrl = GetBullionWithdrawFundUrl();
            investmentWalletViewModel.HideContent = _bullionContactHelper.HasFailedStage1(customerContact) || _bullionContactHelper.HasFailedStage2(customerContact);
            investmentWalletViewModel.IsSippContact = IsSippContactByCustomerContact(customerContact);
        }

        private string GetBullionWithdrawFundUrl()
        {
            var startPage = GetStartPage();
            if (startPage == null || startPage.WithdrawFundsPage == null) return string.Empty;

            return startPage.WithdrawFundsPage.GetExternalUrl_V2();
        }

        private decimal GetPositiveDecimal(decimal value)
        {
            return value >= 0 ? value : decimal.Zero;
        }

        private StartPage GetStartPage()
        {
            return SiteDefinition.Current.StartPage != null && SiteDefinition.Current.StartPage.ID != 0 ? _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage) : null;
        }

        private Currency GetCurrentCurrency(CustomerContact customerContact)
        {
            return customerContact != null
                ? _currencyService.GetCurrentCurrency(customerContact.GetDefaultCurrencyCode())
                : _currencyService.GetCurrentCurrency();
        }
    }
}