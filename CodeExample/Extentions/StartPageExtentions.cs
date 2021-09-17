using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Hephaestus.CMS.Extensions;
using TRM.Shared.Extensions;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Pages;

namespace TRM.Web.Extentions
{
    public static class StartPageExtentions
    {
        public static StartPage GetAppropriateStartPageForSiteSpecificProperties(this object trmObject)
        {
            return GetAppropriateStartPageForSiteSpecificProperties();
        }

        public static IMetalPriceSettings GetMetalPriceSettingsPageForSiteSpecificProperties(this object trmObject)
        {
            var startPage = GetAppropriateStartPageForSiteSpecificProperties();

            if (startPage?.MetalPriceSettingsPage != null)
            {
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
                return contentLoader.Get<MetalPriceSettingsPage>(startPage.MetalPriceSettingsPage);
            }
            else
                return startPage;
        }

        public static StartPage GetAppropriateStartPageForSiteSpecificProperties()
        {
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var startPage = SiteDefinition.Current.StartPage != null && SiteDefinition.Current.StartPage.ID != 0 ? contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage) : null;
            if (startPage != null) return startPage;

            var siteDefinitionRepository = ServiceLocator.Current.GetInstance<ISiteDefinitionRepository>();
            var firstSiteDefinition = siteDefinitionRepository.List().FirstOrDefault();
            if (firstSiteDefinition == null) return null;

            var startPageReference = firstSiteDefinition.StartPage;
            return startPageReference == null ? null : contentLoader.Get<StartPage>(startPageReference);
        }

        public static string GetLoginPageUrl(this object content, string fallback = "")
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var loginPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).LoginPage :
                startPage?.LoginPage;
            if (startPage == null || loginPage == null) return fallback;

            return loginPage.GetExternalUrl_V2();
        }

        public static string GetMyAccountPageUrl(this object content, string fallback = "")
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var myAccountPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).MyAccountPage :
                startPage?.MyAccountPage;

            if (myAccountPage == null) return fallback;

            return myAccountPage.GetExternalUrl_V2();
        }

        public static string GetViewOrdersPageUrl(this object content, string fallback = "")
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var viewOrdersPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ViewOrdersPage :
                startPage?.ViewOrdersPage;

            if (viewOrdersPage == null) return fallback;

            return viewOrdersPage.GetExternalUrl_V2();
        }

        public static string GetInvestmentsPageUrl(this object content, string fallback = "")
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var investmentsPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).InvestmentsPage :
                startPage?.InvestmentsPage;

            if (startPage == null || investmentsPage == null) return fallback;

            return investmentsPage.GetExternalUrl_V2();
        }

        public static string GetPurchasesPageUrl(this object content, string fallback = "")
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var purchasesPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).PurchasesPage :
                startPage?.PurchasesPage;

            if (startPage == null || purchasesPage == null) return fallback;

            return purchasesPage.GetExternalUrl_V2();
        }

        public static string GetManageFundsPageUrl(this object content, string fallback = "")
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var manageFundsPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ManageFundsPage :
                startPage?.ManageFundsPage;

            if (startPage == null || manageFundsPage == null) return fallback;

            return manageFundsPage.GetExternalUrl_V2();
        }

        public static string GetVaultedInvestementsUrl(this object content, string fallback = "")
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var vaultedInvestementsPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).VaultedInvestementsPage :
                startPage?.VaultedInvestementsPage;

            if (startPage == null || vaultedInvestementsPage == null) return fallback;

            return vaultedInvestementsPage.GetExternalUrl_V2();
        }

        public static string GetBullionOnlyCheckoutUrl(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.BullionOnlyCheckoutPage == null) return string.Empty;

            return startPage.BullionOnlyCheckoutPage.GetExternalUrl_V2();
        }

        public static string GetRegistrationPage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.BullionOnlyCheckoutPage == null) return string.Empty;
            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var registrationPage = (startPage?.UserAccountSettingsPage != null) ?
                contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).RegistrationPage :
                startPage?.RegistrationPage;
            return registrationPage.GetExternalUrl_V2();
        }

        public static string GetConsumerOrderNumberPrefix(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.CheckoutPage == null) return string.Empty;

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var checkoutPageContent = contentLoader.Get<PageData>(startPage.CheckoutPage) as CheckoutPage;

            return checkoutPageContent != null ? checkoutPageContent.OrderNumberPrefix : string.Empty;
        }

        public static string GetQuickCheckoutPageUrl(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.QuickCheckoutPage == null) return string.Empty;

            return startPage.QuickCheckoutPage.GetExternalUrl_V2();
        }

        public static string GetBasketUrl(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.BasketPage == null) return string.Empty;

            return startPage.BasketPage.GetExternalUrl_V2();
        }

        public static string GetPriceAlertPageUrl(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var priceAlertPage = (startPage?.MetalPriceSettingsPage != null) ?
                            contentLoader.Get<MetalPriceSettingsPage>(startPage.MetalPriceSettingsPage).PriceAlertPage :
                            startPage?.PriceAlertPage;

            if (priceAlertPage == null) return string.Empty;

            return priceAlertPage.GetExternalUrl_V2();
        }

        public static string GetPriceChartPageUrl(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var priceChartPage = (startPage?.MetalPriceSettingsPage != null) ?
                            contentLoader.Get<MetalPriceSettingsPage>(startPage.MetalPriceSettingsPage).PriceChartPage :
                            startPage?.PriceChartPage;

            if (priceChartPage == null) return string.Empty;

            return priceChartPage.GetExternalUrl_V2();
        }

        public static string GetBullionRegistrationPage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.BullionRegistrationPage == null) return string.Empty;

            return startPage.BullionRegistrationPage.GetExternalUrl_V2();
        }

        public static string GetBullionExistingAccountLandingPage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.BullionExistingAccountLandingPage == null) return string.Empty;

            return startPage.BullionExistingAccountLandingPage.GetExternalUrl_V2();
        }

        public static string GetBullionAccountAddCreditPage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.BullionAccountPaymentPage == null) return string.Empty;

            return startPage.BullionAccountPaymentPage.GetExternalUrl_V2();
        }

        public static string GetBullionFurtherDetailsPage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.KycFurtherDetailsPage == null) return string.Empty;

            return startPage.KycFurtherDetailsPage.GetExternalUrl_V2();
        }

        public static string GetStartPage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            return startPage != null ? startPage.ContentLink.GetExternalUrl_V2() : string.Empty;
        }
	
        public static string GetBullionSignatureLandingPage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.BullionSignatureLandingPage == null) return string.Empty;

            return startPage.BullionSignatureLandingPage.GetExternalUrl_V2();
        }

        public static string GetCustomerServicePage(this object content)
        {
            var startPage = content.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null) return string.Empty;

            return startPage.CustomerServicePage == null ? startPage.ContentLink.GetExternalUrl_V2() : startPage.CustomerServicePage.GetExternalUrl_V2();
        }

    }
}