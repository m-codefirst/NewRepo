using EPiServer.Commerce.Internal;
using EPiServer.Commerce.Marketing.Promotions;
using EPiServer.Commerce.Order;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.Scheduler.Internal;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc.Html;
using Hephaestus.CMS.Business.Gtm;
using Hephaestus.CMS.Business.Initialization;
using Hephaestus.CMS.Business.Rendering;
using Hephaestus.CMS.DataAccess;
using Hephaestus.CMS.DataAccess.DDS;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Helpers;
using kyc;
using Mediachase.Commerce;
using Mediachase.Commerce.InventoryService;
using Mediachase.Commerce.Pricing;
using Mediachase.Commerce.Pricing.Database;
using PricingAndTradingService.Services.Implementations;
using PricingAndTradingService.Services.Interfaces;
using StructureMap.Pipeline;
using System.Diagnostics;
using TRM.IntegrationServices.DataAccess;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Services;
using TRM.ProductFeeds.Helpers;
using TRM.Shared.DataAccess;
using TRM.Shared.Helpers;
using TRM.Shared.Interfaces;
using TRM.Shared.Services;
using TRM.Web.Business.Calculators;
using TRM.Web.Business.Cart;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Business.Feefo;
using TRM.Web.Business.GoogleTagManager;
using TRM.Web.Business.Inventory;
using TRM.Web.Business.Pricing;
using TRM.Web.Business.Promotions;
using TRM.Web.Business.Rendering;
using TRM.Web.Constants;
using TRM.Web.Controllers.Services;
using TRM.Web.Controllers.Services.ExportWrappers;
using TRM.Web.Helpers;
using TRM.Web.Helpers.AutoInvest;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Helpers.Converters;
using TRM.Web.Helpers.Converters.Interfaces;
using TRM.Web.Helpers.Find;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Helpers.TransactionHistories;
using TRM.Web.Models.Catalog.DDS;
using TRM.Web.Models.DDS;
using TRM.Web.Models.EntityFramework.CustomerContactContext;
using TRM.Web.Plugins.ReconciliationExtracts.Services;
using TRM.Web.Services;
using TRM.Web.Services.AutoInvest;
using TRM.Web.Services.MerchandiseFeed;
using TRM.Web.Services.HtmlToPdf;
using TRM.Web.Services.Import;
using TRM.Web.Services.Inventory;
using TRM.Web.Services.InvoiceStatements;
using TRM.Web.Services.MetalPriceChartBuilders;
using TRM.Web.Services.Coupons;
using TRM.Web.Services.ProductBadge;
using TRM.Web.Services.Reporting;
using TRM.Web.Services.TestAutomationHelper;
using TRM.Web.Services.VaultHoldings;
using MetapackShippingProvider.Gateway;
using IAmCartHelper = TRM.Web.Helpers.IAmCartHelper;
using TRM.Web.Services.Payment;
using TRM.Web.Helpers.Payment;
using TRM.Web.Services.Metapack;

namespace TRM.Web.Business.Initialization
{
    [ModuleDependency(typeof(InitializationModule))]
    [ModuleDependency(typeof(ServiceContainerInitialization), typeof(GlobalFilterConfiguration))]
    [InitializableModule]
    public class IocConfiguration : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {

        }

        public void Uninitialize(InitializationEngine context)
        {

        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            Debug.Assert(context != null, "context != null");
            context.StructureMap().Configure(ce =>
            {
                ce.For<IAmLayoutHelper>().Use<LayoutHelper>();
                ce.For<IPageViewContextFactory>().Use<TrmPageViewContextFactory>();
                ce.For<ContentAreaRenderer>().Use<TrmContentAreaRenderer>();
                ce.For<IAmSpecialEventsHelper>().Use<SpecialEventsHelper>();
                ce.For<IPanelHelper>().Use<PanelHelper>();
                ce.For<IAmTeaserHelper>().Use<TeaserHelper>();
                ce.For<IAmAssetHelper>().Use<AssetHelper>();
                ce.For<IAmEntryHelper>().Use<EntryHelper>();
                ce.For<IAmLogExceptionHelper>().Use<LogExceptionHelper>();
                ce.For(typeof(ITrmRankedPropertyRepository<>))
                    .LifecycleIs(new UniquePerRequestLifecycle())
                    .Use(typeof(TrmRankedPropertyRepository<>));
                ce.For(typeof(IAmRankedPropertyHelper<>))
                    .LifecycleIs(new UniquePerRequestLifecycle())
                    .Use(typeof(RankedPropertyHelper<>));
                ce.For(typeof(IResetTokenRepository))
                    .LifecycleIs(new UniquePerRequestLifecycle())
                    .Use(typeof(ResetTokenRespository));
                ce.For(typeof(IAmResetTokenHelper))
                    .LifecycleIs(new UniquePerRequestLifecycle())
                    .Use(typeof(ResetTokenHelper));
                ce.For(typeof(ISpecialEventsRepository))
                    .LifecycleIs(new UniquePerRequestLifecycle())
                    .Use(typeof(SpecialEventsRepository));

                ce.For<IAmNavigationHelper>().Use<NavigationHelper>();
                ce.For<IAmCartHelper>().Use<CartHelper>();
                ce.For<ITrmCartService>().Use<BullionCartService>();
                ce.For<ICurrentMarket>().LifecycleIs(new UniquePerRequestLifecycle()).Use<TrmCurrentMarket>();
                ce.For<IAmSpecificationHelper>().Use<SpecificationHelper>();
                ce.For<IFindService>().LifecycleIs(new UniquePerRequestLifecycle()).Use<FindService>();
                ce.For<IFindHelper>().LifecycleIs(new UniquePerRequestLifecycle()).Use<FindHelper>();
                ce.For<IAmOrderGroupAuditHelper>().Use<OrderGroupAuditHelper>();
                ce.For<IUserService>().Use<UserService>();
                ce.For<IBaseUserService>().Use<BaseUserService>();
                ce.For<IAddressBookService>().Use<AddressBookService>();
                ce.For<IAmInventoryHelper>().Use<InventoryHelper>();
                ce.For<IAmAPaymentMethodHelper>().Use<PaymentMethodHelper>();
                ce.For<IAmAReviewService>().Use<CachedFeefoReviewService>();
                ce.For<IAmCreditHelper>().Use<CreditHelper>();
                ce.For<IAmPaymentHelper>().Use<PaymentHelper>();
                ce.For<IAmOrderHelper>().Use<OrderHelper>();
                ce.For<IAmLogExceptionHelper>().Use<LogExceptionHelper>();
                ce.For<IAmMarketHelper>().Use<MarketHelper>();
                ce.For<IEmailHelper>().Use<EmailHelper>();
                ce.For<IAmVatHelper>().Use<VatHelper>();
                ce.For<IAmContactAuditHelper>().Use<ContactAuditHelper>();
                ce.For<IAmCountryHelper>().Use<CountryHelper>();
                ce.For<IAmValidationHelper>().Use<ValidationHelper>();
                ce.For<IAmXmlHelper>().Use<XmlHelper>();
                ce.For<IAmCreditCardHelper>().Use<CreditCardHelper>();
                ce.For<IBuildGtmDataLayer>().Use<TrmGtmCmsDataLayerBuilder>();
                ce.For<IBuildCommerceGtmDataLayer>().Use<TrmGtmCommerceDataLayerBuilder>();
                ce.For<IAmGoogleTagManagerHelper>().Use<GoogleTagManagerHelper>();
                //ce.For<IAmCustomerHelper>().Use<CustomerHelper>();
                ce.For<IAmRecentlyViewedHelper>().Use<RecentlyViewedHelper>();
                ce.For<IAmStatementHelper>().Use<StatementHelper>();
                ce.For<IAmArticleHelper>().Use<ArticleHelper>();
                ce.For<IHaveCommentHelper>().Use<ArticleHelper>();
                ce.For<IAmSovereignCertificateHelper>().Use<SovereignCertificateHelper>();
                ce.For<IProductFeedHelper>().Use<ProductFeedHelper>();
                ce.For<IProductExtractHelper>().Use<ProductExtractHelper>();
                ce.For<IProductExportHelper>().Use<ProductExportHelper>();
                ce.For<IAmVariantHelper>().Use<VariantHelper>();
                ce.For<IAmEpiServerOrderHelper>().Use<EpiServerOrderHelper>();
                ce.For<IGetOrdersService>().Use<GetOrdersService>();
                ce.For<ICustomerExportService>().Use<CustomerExportService>();

                ce.For<ICreditPaymentRepository>().Use<CreditPaymentRepository>();
                ce.For<ICreditPaymentExportService>().Use<CreditPaymentExportService>();
                ce.For<ILineItemCalculator>().Use<TrmLineItemCalculator>();

                ce.For<IEmailMessageRepository>().Use<EmailMessageRepository>();
                ce.For<IExportEmailMessages>().Use<EmailMesaageExportService>();
                ce.For<IStorageRateHelper>().Use<StorageRateHelper>();

                ce.For<IInventoryService>().Use<InventoryServiceProvider>().Named(StringConstants.EpiInventoryService);
                ce.For<IInventoryService>().Use<TrmInventoryService>();
                ce.For<ITrmInventoryService>().Use<TrmInventoryService>();
          
                ce.For<IAmShippingMethodHelper>().Use<ShippingMethodHelper>();
                ce.For<IAmShippingManagerHelper>().Use<ShippingManagerHelper>();
                ce.For<IProfileMigrator>().Use<Migrations.ProfileMigrator>();

                ce.For<IGdprRepository>().Use<GdprRepository>();
                ce.For<IGdprService>().Use<GdprService>();
                ce.For<ICommentService>().Use<CommentService>();
                ce.For<IAmRmgHelper>().Use<RmgHelper>();

                ce.For<IEmailBackInStockRepository>().Transient().Use<EmailBackInStockRepository>();

                ce.For<IBackInStockService>().Use<BackInStockService>();

                ce.For<IRepository<CountryPostCodeValidator>>().Use<CountryPostcodeValidatorRepository>();
                ce.For<IRepository<QueueItKnownUserConfiguration>>().Use<MultiSelectRepository<QueueItKnownUserConfiguration>>();
                ce.For<IRepository<Brand>>().Use<MultiSelectRepository<Brand>>();
                ce.For<IRepository<PampMetal>>().Use<PampMetalRepository>();
                ce.For<IRepository<Country>>().Use<CountryRepository>();
                ce.For<IGiftingHelper>().Use<GiftingHelper>();
                ce.For<IPrintzwareHelper>().Use<PrintzwareHelper>();

                ce.For<IAmContactHelper>().Use<ContactHelper>();
                ce.For<IAmBullionContactHelper>().Use<BullionContactHelper>();

                ce.For<IPricingAndTradingService>().Use<PampService>();
                ce.For<IBullionUserService>().Use<BullionUserService>();
                ce.For<IBullionVatStatusesHelper>().Use<BullionVatStatusesHelper>();
                ce.For<IBullionPremiumGroupHelper>().Use<BullionPremiumGroupHelper>();
                
                ce.For<IMetaFieldTypeHelper>().Use<MetaFieldTypeHelper>();
                ce.For<IAmSecurityQuestionHelper>().Use<SecurityQuestionHelper>();

                ce.For<ISiteHelper>().Use<SiteHelper>();
                ce.For<IAmAddressHelper>().Use<AddressHelper>();

                // Bullion custom pricing, inventory
                ce.For<ILogger>().Use(x => LogManager.GetLogger(x.ParentType ?? x.RootType));
                ce.For<LineItemQuantityValidator>().Use<TrmLineItemQuantityValidator>().Singleton();
                ce.For<IAmCurrencyHelper>().Use<CurrencyHelper>().Singleton();

                ce.For<IPriceService>().DecorateAllWith<TrmPriceService>();
                ce.For<IPriceService>().Use<PriceServiceDatabase>();

                ce.For<IInventoryProcessor>().DecorateAllWith<TrmInventoryProcessor>();
                ce.For<IInventoryProcessor>().Use<DefaultInventoryProcessor>();

                ce.For<IPlacedPriceProcessor>().DecorateAllWith<TrmPlacedPriceProcessor>().Singleton();
                ce.For<IPlacedPriceProcessor>().Use<TrmPlacedPriceProcessor>().Singleton();

                ce.For<IAmStoreHelper>().ClearAll();
                ce.For<IAmStoreHelper>().Add<StoreHelper>().Named(nameof(StoreHelper));
                ce.For<IAmStoreHelper>().Use<TrmStoreHelper>().Ctor<IAmStoreHelper>()
                    .Is(ctx => ctx.GetInstance<IAmStoreHelper>(nameof(StoreHelper)));

                ce.For<IKycPersonQuery>().Use<KycPersonQuery>();
                ce.For<IKycHelper>().Use<KycHelper>();

                ce.For<ICustomerBankAccountHelper>().Use<CustomerBankAccountHelper>();
                ce.For<IBullionWithdrawalHelper>().Use<BullionWithdrawalHelper>();
                ce.For<IKycDocumentQuery>().Use<KycDocumentQuery>();

                ce.For<IRepository<RoyalMintBankAccountDetail>>().Use<RoyalMintBankAccountDetailRepository>();

                ce.For<IAmTransactionHistoryHelper>().Use<TransactionHistoryHelper>().Transient();

                ce.For<IBankAccountValidationService>().Use<BankAccountValidationService>();

                ce.For<IBullionEmailHelper>().Use<BullionEmailHelper>();

                ce.For<ITransactionHistoryDetailBuilderHelper>().Use<AccountActivityTransactionHistoryDetailBuidlerHelper>();
                ce.For<ITransactionHistoryDetailBuilderHelper>().Use<WithdrawalTransactionDetailBuilderHelper>();
                ce.For<ITransactionHistoryDetailBuilderHelper>().Use<StorageFeeTransactionHistoryDetailBuilderHelper>();
                ce.For<ITransactionHistoryDetailBuilderHelper>().Use<FundAddedTransactionDetailBuilderHelper>();
                ce.For<ITransactionHistoryDetailBuilderHelper>().Use<PurchaseTransactionDetailBuilderHelper>();
                ce.For<ITransactionHistoryDetailBuilderHelper>().Use<DeliveryFromVaultTransactionHistoryDetailBuilderHelper>();
                ce.For<ITransactionHistoryDetailBuilderHelper>().Use<SellFromVaultTransactionHistoryDetailBuilderHelper>();

                #region Calculators
                ce.For<IOrderGroupCalculator>().Use<OrderGroupCalculator>();

                // Tax Calculator
                ce.For<ITaxCalculator>().DecorateAllWith<TrmDefaultTaxCalculator>().Singleton();
                ce.For<ITaxCalculator>().Use<TrmDefaultTaxCalculator>();

                // Shipping Calculator
                ce.For<IShippingCalculator>().DecorateAllWith<TrmShippingCalculator>().Singleton();
                ce.For<IShippingCalculator>().Use<TrmShippingCalculator>();

                ce.For<ILineItemCalculator>().DecorateAllWith<TrmLineItemCalculator>().Singleton();
                ce.For<ILineItemCalculator>().Use<TrmLineItemCalculator>().Singleton();
                #endregion

                // Metal price chart data builder
                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartLiveDataBuilder>();
                ce.For<IMetalPriceChartDataBuilder>().Use<MetalPriceChartTodayDataBuilder>();
                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartWeekDataBuilder>();

                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartMonthDataBuilder>();
                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartThreeMonthsDataBuilder>();
                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartSixMonthsDataBuilder>();

                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartYearDataBuilder>();
                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartThreeYearsDataBuilder>();
                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartFiveYearsDataBuilder>();
                ce.For<IMetalPriceChartDataBuilder>().Use<MetaPriceChartAllTimeDataBuilder>();

                ce.For<IAmLocalPriceDataHelper>().Use<LocalPriceDataHelper>();

                ce.For<IBullionStatementService>().Use<BullionStatementService>();

                ce.For<ITrmHtmlToPdf>().Use<RestPackHtmlToPdf>();

                ce.For<IAmPersonalisedDataHelper>().Use<PersonalisedDataHelper>();

                ce.For<IXmlImportService>().Use<XmlImportService>();

                ce.For<IAmAnOrderExportServiceWrapper>().Use<OrderExportServiceWrapper>();
                
                ce.For<IEmailMessageRepository>().Use<EmailMessageRepository>();

                ce.For<IExportTransactionsRepository>().Use<ExportTransactionsRepository>();

                ce.For<IBullionGbiDataImportService>().Use<BullionGbiDataImportService>();
                ce.For<IAxDataImportService>().Use<AxDataImportService>();

                ce.For<SpendAmountGetGiftItemsProcessor>().Use<TrmSpendAmountGetFreeGiftProcessor>();
                ce.For<IDataImportInventoryService>().Use<DataImportInventoryService>();

                ce.For<IOrderNumberGenerator>().Use<TrmOrderNumberGenerator>();

                // Reconciliation extract services
                ce.For<IReconciliationExtractService>().Use<CustomerReconciliationExtractService>();
                ce.For<IReconciliationExtractService>().Use<BuyOrderReconciliationExtractService>();
                ce.For<IReconciliationExtractService>().Use<CardDepositReconciliationExtractService>();
                ce.For<IReconciliationExtractService>().Use<SellTransactionReconciliationExtractService>();
                ce.For<IReconciliationExtractService>().Use<DeliverTransactionsReconciliationExtractService>();
                ce.For<IReconciliationExtractService>().Use<WithdrawalRequestReconciliationExtractService>();

                ce.For<IAccountCreditPaymentHelper>().Use<AccountCreditPaymentHelper>();
                ce.For<IThirdPartyTransactionRepository>().Use<ThirdPartyTransactionRepository>();
                ce.For<IEpiServerCustomerHelper>().Use<EpiServerCustomerHelper>();
                ce.For<ICheckAml>().Use<AmlCheckService>();

                ce.For<ITrmVariantHelper>().Use<TrmVariantHelper>();
                ce.For<IInvestmentWalletHelper>().Use<InvestmentWalletHelper>();
                ce.For<ITrmCategoryHelper>().Use<TrmCategoryHelper>(); 
                ce.For<ISearchPageHelper>().Use<SearchPageHelper>(); 

                ce.For<IAddressConverterByOrder>().Use<AddressConverterByOrder>();
                ce.For<IAnalyticsDigitalData>().Use<AnalyticsDigitalData>();
                ce.For<IAutoInvestUserService>().Use<AutoInvestUserService>();
                ce.For<IAutoInvestmentSerializationHelper>().Use<AutoInvestmentSerializationHelper>();
                ce.For<IAutoInvestProductsService>().Use<AutoInvestProductsService>();
                ce.For<IAutoPurchaseService>().Use<AutoPurchaseService>();
                ce.For<IAutoPurchaseHelper>().Use<AutoPurchaseHelper>();
                ce.For<IAutoPurchaseMailingService>().Use<AutoPurchaseMailingService>();

                ce.For<IJobFailedHandler>().Use<JobFailedEmailHandler>();
                ce.For<IAutoInvestPurchaseService>().Use<AutoInvestPurchaseService>();
                ce.For<IAutoPurchaseUsersProvider>().Use<AutoPurchaseUsersProvider>();
                ce.For<ICustomerContactDbContextFactory>().Use<CustomerContactDbContextFactory>();

                ce.For<ICommonSettingsService>().Use<CommonSettingsService>();
                ce.For<IProcessCartHelper>().Use<ProcessCartHelper>();
                ce.For<IRestrictedCountriesHelper>().Use<RestrictedCountriesHelper>();
                ce.For<IBestBetsFindHelper>().Use<BestBetsFindHelper>();
                ce.For<IGenerateFeed>().Use<FeedGeneration>();
                ce.For<FeedBuilder>().Use<EpiDefaultFeedBuilder>();
                ce.For<INotVisibleCategoriesHelper>().Use<NotVisibleCategoriesHelper>();

                ce.For<IStockInventoryHelper>().Use<StockInventoryHelper>();

                ce.For<ICouponService>().Use<UniqueCouponService>();
                ce.For<IVaultHoldingsService>().Use<VaultHoldingsService>();
                ce.For<IProductBadgeRepository>().Add<ProductBadgeRepository>().Named(nameof(ProductBadgeRepository));
                ce.For<IProductBadgeRepository>().Use<CachedProductBadgeRepository>().Ctor<IProductBadgeRepository>()
                    .Is(ctx => ctx.GetInstance<IProductBadgeRepository>(nameof(ProductBadgeRepository)));
                ce.For<IProductBadgeService>().Use<ProductBadgeService>();
                ce.For<IImpersonationLogService>().Use<ImpersonationLogService>();
                ce.For<IStockReportService>().Use<EpiFindStockReportService>();
                ce.For<IReportSerializer>().Use<ReportSerializer>();
                ce.For<IPublishedContentReportService>().Use<PublishedContentReportService>();
                ce.For<ITestAutomationHelperService>().Use<TestAutomationHelperService>();
                ce.For<IBarclaysPaymentService>().Use<BarclaysPaymentService>();
                ce.For<IBarclaysCardPaymentProviderHelper>().Use<BarclaysCardBarclaysCardPaymentProviderHelper>();
                ce.For<IMetapackShippingService>().Use<MetapackShippingService>();
            });
        }
    }
}
