using System.Globalization;
using System.Web.Optimization;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Web;
using TRM.Web.Constants;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    public class BundlesInitializationModule : IInitializableModule
    {
        private const string BundleVirtualPathPrefix = "~/bundles/";

        public void Initialize(InitializationEngine context)
        {
            var siteDefinitionRepository = context.Locate.Advanced.GetInstance<ISiteDefinitionRepository>();
            var siteDefinitions = siteDefinitionRepository.List();
            foreach (var siteDefinition in siteDefinitions)
            {
                var siteName = siteDefinition.Name;
                GetScriptBundles(siteName);
                GetCssBundles(siteName);
            }
        }

        public void Uninitialize(InitializationEngine context) { }

        private void GetScriptBundles(string siteName)
        {
            var jsPath = string.Format(CultureInfo.InvariantCulture, "~/static/{0}/js/", siteName);

            if (siteName.ToLower() != StringConstants.RoyalMint2020SiteName)
            {
                // HeadScripts
                var headerBundle = CreateBundle(BundleType.JS, siteName + "headscripts");
                BundleTable.Bundles.Add(headerBundle);

                // comment
                var articleBundle = CreateBundle(BundleType.JS, siteName + "article");
                articleBundle.IncludeDirectory(jsPath + "article", "*.js", true);
                BundleTable.Bundles.Add(articleBundle);

                //Variant carousel
                var vartiantCarousel = CreateBundle(BundleType.JS, siteName + "vartiantCarousel");
                vartiantCarousel.Include(jsPath + "products/variantCarousel.js");
                BundleTable.Bundles.Add(vartiantCarousel);

                // special events
                var specialEvents = CreateBundle(BundleType.JS, siteName + "specialEvents");
                specialEvents.IncludeDirectory(jsPath + "occasion", "*.js", true);
                BundleTable.Bundles.Add(specialEvents);

                // jquery
                var jqueryBundle = CreateBundle(BundleType.JS, siteName + "jquery");
                jqueryBundle.IncludeDirectory(jsPath + "jquery/3.4.1", "*.js", true);
                BundleTable.Bundles.Add(jqueryBundle);

                // angular
                var angularBundle = CreateBundle(BundleType.JS, siteName + "angular");
                angularBundle.Include(jsPath + "angularjs/1.7.9/angular.min.js");
                angularBundle.Include(jsPath + "angularjs/1.7.9/angular-animate.min.js");
                angularBundle.Include(jsPath + "angularjs/1.7.9/angular-cookies.min.js");
                angularBundle.Include(jsPath + "angularjs/1.7.9/angular-route.min.js");
                angularBundle.Include(jsPath + "angularjs/1.7.9/angular-sanitize.min.js");
                BundleTable.Bundles.Add(angularBundle);

                // angular app 
                var appBundle = CreateBundle(BundleType.JS, siteName + "app");
                appBundle.Include(jsPath + "app/app.js");
                appBundle.Include(jsPath + "app/services.js");
                appBundle.Include(jsPath + "app/directives.js");
                appBundle.Include(jsPath + "app/directives/ngEnter.js");
                appBundle.Include(jsPath + "app/rmCommerceController.js");
                appBundle.Include(jsPath + "app/rmCheckoutController.js");
                appBundle.Include(jsPath + "app/rmSearch.js");
                appBundle.Include(jsPath + "app/gbchSearchCoinModule.js");
                appBundle.Include(jsPath + "app/rmCatalogueController.js");
                appBundle.Include(jsPath + "app/rmRmg.js");
                appBundle.Include(jsPath + "app/rmCookies.js");
                appBundle.Include(jsPath + "app/rmLogin.js");
                appBundle.Include(jsPath + "app/rmAccountStatementController.js");
                appBundle.Include(jsPath + "app/rmRegistrationController.js");
                appBundle.Include(jsPath + "app/rmContactPreferencesController.js");
                appBundle.Include(jsPath + "app/rmErrorHandler.js");
                appBundle.Include(jsPath + "app/rmFooterSignUpController.js");
                appBundle.Include(jsPath + "app/rmSovereignCertificateSignUpController.js");
                appBundle.Include(jsPath + "app/rmPasswordChangeController.js");
                appBundle.IncludeDirectory(jsPath + "app/directives", "*.js");
                appBundle.Include(jsPath + "app/passwordStrength.js");
                appBundle.Include(jsPath + "app/rmTransactionHistoryController.js");
                appBundle.Include(jsPath + "app/rmArticleListingController.js");
                appBundle.Include(jsPath + "app/rmHistoricMetalPriceChartController.js");
                appBundle.Include(jsPath + "app/rmCustomerServiceController.js");
                appBundle.Include(jsPath + "app/rmSIPPSSASAdminController.js");
                appBundle.Include(jsPath + "app/rmChangeLoginDetailsController.js");
                appBundle.Include(jsPath + "app/directives/confirm-password.directive.js");
                BundleTable.Bundles.Add(appBundle);

                // bootstrap
                var bootstrapBundle = CreateBundle(BundleType.JS, siteName + "bootstrap");
                bootstrapBundle.Include(jsPath + "bootstrap/affix.js");
                bootstrapBundle.Include(jsPath + "bootstrap/alert.js");
                bootstrapBundle.Include(jsPath + "bootstrap/button.js");
                bootstrapBundle.Include(jsPath + "bootstrap/collapse.js");
                bootstrapBundle.Include(jsPath + "bootstrap/dropdown.js");
                bootstrapBundle.Include(jsPath + "bootstrap/modal.js");
                bootstrapBundle.Include(jsPath + "bootstrap/tooltip.js");
                bootstrapBundle.Include(jsPath + "bootstrap/popover.js");
                bootstrapBundle.Include(jsPath + "bootstrap/tab.js");
                bootstrapBundle.Include(jsPath + "bootstrap/transition.js");
                BundleTable.Bundles.Add(bootstrapBundle);

                // footscripts
                var footerBundle = CreateBundle(BundleType.JS, siteName + "footscripts");
                footerBundle.IncludeDirectory(jsPath + "plugins", "*.js", true);
                footerBundle.IncludeDirectory(jsPath + "project", "*.js", true);
                footerBundle.Include(jsPath + "bullion/registration.js");
                footerBundle.Include(jsPath + "commons/cookie.js");
                footerBundle.Include(jsPath + "commons/kycDismissAlert.js");
                BundleTable.Bundles.Add(footerBundle);

                // products
                var productsBundle = CreateBundle(BundleType.JS, siteName + "products");
                productsBundle.IncludeDirectory(jsPath + "products", "*.js", true);
                BundleTable.Bundles.Add(productsBundle);
                // currencyFetcher

                var currencyFetcher = CreateBundle(BundleType.JS, siteName + "currencyFetcher");
                currencyFetcher.Include(jsPath + "bullion/registration.js");
                BundleTable.Bundles.Add(currencyFetcher);

                // Bullion manage bank account
                var bankAccount = CreateBundle(BundleType.JS, siteName + "bullionAddBankAccount");
                bankAccount.Include(jsPath + "bullion/managebankaccount.js");
                BundleTable.Bundles.Add(bankAccount);

                // Bullion request withdrawal
                var requestWithdrawal = CreateBundle(BundleType.JS, siteName + "bullionRequestWithdrawal");
                requestWithdrawal.Include(jsPath + "bullion/requestwithdrawal.js");
                BundleTable.Bundles.Add(requestWithdrawal);

                // Bullion KycFurtherDetails
                var bullionKycFurtherDetails = CreateBundle(BundleType.JS, siteName + "bullionKycFurtherDetails");
                bullionKycFurtherDetails.Include(jsPath + "bullion/bullionKycFurtherDetails.js");
                BundleTable.Bundles.Add(bullionKycFurtherDetails);

                // Bullion KycFurtherDetails
                var commonFuntions = CreateBundle(BundleType.JS, siteName + "commonFuntions");
                commonFuntions.Include(jsPath + "commons/dragdropimage.js");
                BundleTable.Bundles.Add(commonFuntions);

                // Bullion Statement
                var bullionStatement = CreateBundle(BundleType.JS, siteName + "bullionStatement");
                bullionStatement.Include(jsPath + "bullion/statement.js");
                BundleTable.Bundles.Add(bullionStatement);

                //Bullion Sell Or Deliver From Vault
                var sellOrDeliverFromVault = CreateBundle(BundleType.JS, siteName + "sellOrDeliverFromVault");
                sellOrDeliverFromVault.Include(jsPath + "bullion/sellOrDeliverFromVault.js");
                BundleTable.Bundles.Add(sellOrDeliverFromVault);

                //customer price alert
                var customerPriceAlert = CreateBundle(BundleType.JS, siteName + "customerPriceAlert");
                customerPriceAlert.Include(jsPath + "bullion/customerPriceAlert.js");
                BundleTable.Bundles.Add(customerPriceAlert);

                //legacy
                var legacyFunctions = CreateBundle(BundleType.JS, siteName + "legacyFunctions");
                legacyFunctions.Include(jsPath + "legacy/picturefill.js");
                BundleTable.Bundles.Add(legacyFunctions);

                //eventEmitter
                var eventEmitter = CreateBundle(BundleType.JS, siteName + "eventEmitter");
                eventEmitter.Include(jsPath + "commons/eventEmitter.js");
                BundleTable.Bundles.Add(eventEmitter);
            }
            else
            {
                // Royal Mint 2020 --------------------------------------------------

                // Common (webpack)
                var commonBundle = CreateBundle(BundleType.JS, siteName + "common");
                commonBundle.Include(string.Format(CultureInfo.InvariantCulture, "~/static/{0}/", siteName) + "common.bundle.js");
                BundleTable.Bundles.Add(commonBundle);

                // React
                var reactBundle = CreateBundle(BundleType.JS, siteName + "react");
                reactBundle.Include(string.Format(CultureInfo.InvariantCulture, "~/static/{0}/", siteName) + "index.bundle.js");
                BundleTable.Bundles.Add(reactBundle);

                // price Charts
                var priceChartsBundle = CreateBundle(BundleType.JS, siteName + "priceCharts");
                priceChartsBundle.Include(string.Format(CultureInfo.InvariantCulture, "~/static/{0}/", siteName) + "priceCharts.bundle.js");
                BundleTable.Bundles.Add(priceChartsBundle);


                // SVG
                var svgBundle = CreateBundle(BundleType.JS, siteName + "svg");
                svgBundle.Include(string.Format(CultureInfo.InvariantCulture, "~/static/{0}/", siteName) + "svgSource.bundle.js");
                BundleTable.Bundles.Add(svgBundle);

                // jQuery 
                var trm2020jQueryBundle = CreateBundle(BundleType.JS, siteName + "jQuery");
                trm2020jQueryBundle.Include(jsPath + "_vendor/jquery.js");
                BundleTable.Bundles.Add(trm2020jQueryBundle);

                // Bootstrap 
                var trm2020BootstrapBundle = CreateBundle(BundleType.JS, siteName + "bootstrap");
                trm2020BootstrapBundle.Include(jsPath + "_vendor/popper.js");
                trm2020BootstrapBundle.Include(jsPath + "_vendor/bootstrap.js");
                BundleTable.Bundles.Add(trm2020BootstrapBundle);

                // vendors
                var trm2020VendorsBundle = CreateBundle(BundleType.JS, siteName + "vendors");
                trm2020VendorsBundle.Include(jsPath + "_vendor/slick.js");
                trm2020VendorsBundle.Include(jsPath + "_vendor/enquire.js");
                trm2020VendorsBundle.Include(jsPath + "_vendor/svgxuse.js");
                trm2020VendorsBundle.Include(jsPath + "_vendor/bootstrap-spinner.js");
                trm2020VendorsBundle.Include(jsPath + "_vendor/timer.js");
                trm2020VendorsBundle.Include(jsPath + "_vendor/moment.js");
                trm2020VendorsBundle.Include(jsPath + "_vendor/chart.js");
                BundleTable.Bundles.Add(trm2020VendorsBundle);

                // vendors (async)
                var trm2020VendorsAsyncBundle = CreateBundle(BundleType.JS, siteName + "vendorsAsync");
                trm2020VendorsAsyncBundle.Include(jsPath + "_vendor/picturefill.js");
                BundleTable.Bundles.Add(trm2020VendorsAsyncBundle);

                // modules
                var trm2020ModulesBundle = CreateBundle(BundleType.JS, siteName + "modules");
                trm2020ModulesBundle.Include(jsPath + "_modules/carousel.js");
                trm2020ModulesBundle.Include(jsPath + "_modules/gallery.js");
                trm2020ModulesBundle.Include(jsPath + "_modules/share.js");
                trm2020ModulesBundle.Include(jsPath + "_modules/countdown.js");
                trm2020ModulesBundle.Include(jsPath + "_modules/video.js");
                BundleTable.Bundles.Add(trm2020ModulesBundle);

                // global
                var trm2020GlobalBundle = CreateBundle(BundleType.JS, siteName + "global");
                trm2020GlobalBundle.Include(jsPath + "_global/global.js");
                trm2020GlobalBundle.Include(jsPath + "_global/navigation.js");
                BundleTable.Bundles.Add(trm2020GlobalBundle);

                // init
                var trm2020InitBundle = CreateBundle(BundleType.JS, siteName + "init");
                trm2020InitBundle.Include(jsPath + "_global/init.js");
                BundleTable.Bundles.Add(trm2020InitBundle);

                // charts
                var trm2020ChartsBundle = CreateBundle(BundleType.JS, siteName + "charts");
                trm2020ChartsBundle.Include(jsPath + "_modules/charts.js");
                BundleTable.Bundles.Add(trm2020ChartsBundle);

                // help page
                var trm2020HelpPageBundle = CreateBundle(BundleType.JS, siteName + "helpPage");
                trm2020HelpPageBundle.Include(jsPath + "_vendor/masonry.js");
                trm2020HelpPageBundle.Include(jsPath + "help/section.js");
                BundleTable.Bundles.Add(trm2020HelpPageBundle);

                // image zoom
                var trm2020ZoomBundle = CreateBundle(BundleType.JS, siteName + "zoom");
                trm2020ZoomBundle.Include(jsPath + "_vendor/jquery.ez-plus.js");
                trm2020ZoomBundle.Include(jsPath + "_modules/zoom.js");
                BundleTable.Bundles.Add(trm2020ZoomBundle);

                // search
                var trm2020SearchBundle = CreateBundle(BundleType.JS, siteName + "filters");
                trm2020SearchBundle.Include(jsPath + "_modules/filters.js");
                BundleTable.Bundles.Add(trm2020SearchBundle);

                // ------------------------------------------------------------------
            }
        }

        private void GetCssBundles(string siteName)
        {
            var cssPath = string.Format(CultureInfo.InvariantCulture, "~/static/{0}/css/", siteName);

            if (siteName.ToLower() != StringConstants.RoyalMint2020SiteName)
            {
                // Css
                var cssBundle = CreateBundle(BundleType.CSS, siteName + "css");
                cssBundle.Include(cssPath + "style.css", new CssRewriteUrlTransform());
                cssBundle.Include(cssPath + "gbch.css", new CssRewriteUrlTransform());
                cssBundle.Include(cssPath + "printzware.css", new CssRewriteUrlTransform());

                BundleTable.Bundles.Add(cssBundle);

                // Css
                var articleCssBundle = CreateBundle(BundleType.CSS, siteName + "articleCss");
                articleCssBundle.Include(cssPath + "article.css", new CssRewriteUrlTransform());
                BundleTable.Bundles.Add(articleCssBundle);

                var specialEventsCssBundle = CreateBundle(BundleType.CSS, siteName + "specialEventsCss");
                specialEventsCssBundle.Include(cssPath + "specialEvent.css", new CssRewriteUrlTransform());
                BundleTable.Bundles.Add(specialEventsCssBundle);

                var bullionCssBundle = CreateBundle(BundleType.CSS, siteName + "bullionCss");
                bullionCssBundle.Include(cssPath + "bullion/style.css", new CssRewriteUrlTransform());
                BundleTable.Bundles.Add(bullionCssBundle);
            }
            else
            {
                // Royal Mint 2020 --------------------------------------------------

                var trm2020CssBundle = CreateBundle(BundleType.CSS, siteName + "globalCss");
                trm2020CssBundle.Include(cssPath + "_global/global.css");
                trm2020CssBundle.Include(cssPath + "_form-data/form-data.css");
                trm2020CssBundle.Include(cssPath + "invest/investment-prices/page.css");
                BundleTable.Bundles.Add(trm2020CssBundle);

                var trm2020AccountCssBundle = CreateBundle(BundleType.CSS, siteName + "accountCss");
                trm2020AccountCssBundle.Include(cssPath + "account/section.css");
                trm2020AccountCssBundle.Include(cssPath + "account/purchases/section.css");
                trm2020AccountCssBundle.Include(cssPath + "account/page.css");
                BundleTable.Bundles.Add(trm2020AccountCssBundle);

                var trm2020TransactionHistoryCssBundle = CreateBundle(BundleType.CSS, siteName + "transactionHistoryCss");
                trm2020TransactionHistoryCssBundle.Include(cssPath + "account/section.css");
                trm2020TransactionHistoryCssBundle.Include(cssPath + "account/transaction-history/page.css");
                BundleTable.Bundles.Add(trm2020TransactionHistoryCssBundle);

                var trm2020VaultedHoldingsCssBundle = CreateBundle(BundleType.CSS, siteName + "vaultedHoldingsCss");
                trm2020VaultedHoldingsCssBundle.Include(cssPath + "account/section.css");
                trm2020VaultedHoldingsCssBundle.Include(cssPath + "account/vaulted-holdings/page.css");
                trm2020VaultedHoldingsCssBundle.Include(cssPath + "account/vaulted-holdings/deliver/page.css");
                BundleTable.Bundles.Add(trm2020VaultedHoldingsCssBundle);

                var trm2020ShopCssBundle = CreateBundle(BundleType.CSS, siteName + "shopCss");
                trm2020ShopCssBundle.Include(cssPath + "shop/editions/range/product/page.css");
                BundleTable.Bundles.Add(trm2020ShopCssBundle);

                var trm2020CheckoutCssBundle = CreateBundle(BundleType.CSS, siteName + "checkoutCss");
                trm2020CheckoutCssBundle.Include(cssPath + "checkout/section.css");
                BundleTable.Bundles.Add(trm2020CheckoutCssBundle);

                var trm2020LittleTreasuresCssBundle = CreateBundle(BundleType.CSS, siteName + "littleTreasuresCss");
                trm2020LittleTreasuresCssBundle.Include(cssPath + "littleTreasures/section.css");
                BundleTable.Bundles.Add(trm2020LittleTreasuresCssBundle);

                var trm2020DigiInvestmentsCssBundle = CreateBundle(BundleType.CSS, siteName + "digiInvestmentsCss");
                trm2020DigiInvestmentsCssBundle.Include(cssPath + "digiInvestments/section.css");
                BundleTable.Bundles.Add(trm2020DigiInvestmentsCssBundle);


                var trm2020BasketCssBundle = CreateBundle(BundleType.CSS, siteName + "basketCss");
                trm2020BasketCssBundle.Include(cssPath + "basket/page.css");
                BundleTable.Bundles.Add(trm2020BasketCssBundle);

                var trm2020SearchCssBundle = CreateBundle(BundleType.CSS, siteName + "searchCss");
                trm2020SearchCssBundle.Include(cssPath + "search/section.css");
                BundleTable.Bundles.Add(trm2020SearchCssBundle);

                var trm2020ErrorPageCssBundle = CreateBundle(BundleType.CSS, siteName + "errorPageCss");
                trm2020ErrorPageCssBundle.Include(cssPath + "error/page.css");
                BundleTable.Bundles.Add(trm2020ErrorPageCssBundle);

                var trm2020HelpPageCssBundle = CreateBundle(BundleType.CSS, siteName + "helpPageCss");
                trm2020HelpPageCssBundle.Include(cssPath + "help/section.css");
                trm2020HelpPageCssBundle.Include(cssPath + "help/page.css");
                BundleTable.Bundles.Add(trm2020HelpPageCssBundle);

                var trm2020InvestSignaturePageCssBundle = CreateBundle(BundleType.CSS, siteName + "investSignaturePageCss");
                trm2020InvestSignaturePageCssBundle.Include(cssPath + "invest/signature/page.css");
                BundleTable.Bundles.Add(trm2020InvestSignaturePageCssBundle);

                var trm2020DeliverPageCssBundle = CreateBundle(BundleType.CSS, siteName + "deliverPageCss");
                trm2020DeliverPageCssBundle.Include(cssPath + "account/vaulted-holdings/deliver/page.css");
                BundleTable.Bundles.Add(trm2020DeliverPageCssBundle);

                // ------------------------------------------------------------------
            }
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Bundle CreateBundle(BundleType bundleType, string bundleName)
        {
            if (bundleType == BundleType.CSS) return new StyleBundle(BundleVirtualPathPrefix + bundleName);
            return new ScriptBundle(BundleVirtualPathPrefix + bundleName);
        }

        public enum BundleType
        {
            // ReSharper disable once InconsistentNaming
            JS,
            // ReSharper disable once InconsistentNaming
            CSS
        }
    }
}