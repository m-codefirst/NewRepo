using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.Extensions;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using PricingAndTradingService.Models;
using TrmWallet;
using TRM.IntegrationServices.Constants;
using TRM.IntegrationServices.Interfaces;
using TRM.Shared.Extensions;
using TRM.Web.Business.Pricing;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Models.ViewModels.Bullion.Portfolio;
using StringConstants = TRM.Web.Constants.StringConstants;
using System.Web;
using TRM.Web.Models.ViewModels;
using TRM.Web.Models.ViewModels.MetalPrice;
using TRM.Web.Services.SellFromVault;

namespace TRM.Web.Services.Portfolio
{
    public class MetalWithVariantType
    {
        public PricingAndTradingService.Models.Constants.MetalType MetalType { get; set; }
        public Enums.BullionVariantType BullionType { get; set; }
    }

    [ServiceConfiguration(typeof(IBullionPortfolioService), Lifecycle = ServiceInstanceScope.Transient)]
    public class BullionPortfolioService : IBullionPortfolioService
    {
        private const string MetalSellPricesHttpContextItemKey = nameof(MetalSellPricesHttpContextItemKey);
        private readonly ITrmWalletRepository _portfolioRepository;
        private readonly IExportTransactionsRepository _exportTransactionsRepository;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly IPremiumCalculator<IAmPremiumVariant> _premiumCalculator;
        private readonly CustomerContext _customerContext;
        private readonly IBullionVariantHelper _variantHelper;
        private readonly ILineItemCalculator _lineItemCalculator;
        private readonly IBullionSellFromVaultService _bullionSellFromVaultService;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IMetalPriceService _metalPriceService;

        public BullionPortfolioService(
            ITrmWalletRepository portfolioRepository,
            IAmCurrencyHelper currencyHelper,
            IPremiumCalculator<IAmPremiumVariant> premiumCalculator,
            CustomerContext customerContext,
            IBullionVariantHelper variantHelper,
            ILineItemCalculator lineItemCalculator,
            IExportTransactionsRepository exportTransactionsRepository,
            IBullionSellFromVaultService bullionSellFromVaultService,
            IAmBullionContactHelper bullionContactHelper,
            IMetalPriceService metalPriceService)
        {
            _portfolioRepository = portfolioRepository;
            _currencyHelper = currencyHelper;
            _premiumCalculator = premiumCalculator;
            _customerContext = customerContext;
            _variantHelper = variantHelper;
            _lineItemCalculator = lineItemCalculator;
            _exportTransactionsRepository = exportTransactionsRepository;
            _bullionSellFromVaultService = bullionSellFromVaultService;
            _bullionContactHelper = bullionContactHelper;
            _metalPriceService = metalPriceService;
        }

        public PortfolioViewModel CreatePortfolioViewModel()
        {
            var vaultContentViewModel = CreateVaultContentViewModel();
            vaultContentViewModel.LegacyHeading = true;
            
            return new PortfolioViewModel
            {
                VaultContentViewModel = vaultContentViewModel
            };
        }

        public VaultContentViewModel CreateVaultContentViewModel()
        {
            var currency = _currencyHelper.GetDefaultCurrencyCode();

            var metalSellPrices = GetMetalSellPrices(currency);
            HttpContext.Current.Items[MetalSellPricesHttpContextItemKey] = metalSellPrices;

            var portfolioVariantItems = GetPortfolioVariantItems(metalSellPrices, currency);
            var currentContact = _customerContext.CurrentContact;
            var isBullionAccount = _bullionContactHelper.IsBullionAccount(currentContact);

            return new VaultContentViewModel
            {
                SellBackUrl = GetSellFromVaultUrl(),
                DeliverFromVaultUrl = GetDeliverFromVaultUrl(),
                CategoriesFilter = GetCategoriesFilter(),
                MetalTypesFilter = GetMetalTypesFilter(),
                PortfolioVariantItems = portfolioVariantItems.ToList(),
                ShowNeedConfirmKyc = _variantHelper.ShowNeedConfirmKyc(isBullionAccount),
                LegacyHeading = false
            };
        }

        public VaultedInvestmentBlockViewModel CreateVaultedInvestmentBlockViewModel()
        {
            var currency = _currencyHelper.GetDefaultCurrencyCode();
            var metalSellPrices = HttpContext.Current.Items[MetalSellPricesHttpContextItemKey] as Dictionary<string, decimal> ??
                                  GetMetalSellPrices(currency);

            var portfolioVariantItems = GetPortfolioVariantItems(metalSellPrices, currency);
            var priceGroupDic = CalculatingPriceForVariantGroup(portfolioVariantItems);

            var totalHoldingValue = GetTotalHoldingValueWithUnit(priceGroupDic, currency);

            var pampMetalPrices = _metalPriceService.GetPampMetalPrices().ToList();

            return new VaultedInvestmentBlockViewModel
            {
                TotalHoldings = totalHoldingValue.ToString(),
                NoHoldings = totalHoldingValue.Amount.Equals(decimal.Zero),
                GoldMetal = GetPortfolioMetaInfo(portfolioVariantItems, priceGroupDic, PricingAndTradingService.Models.Constants.MetalType.Gold, currency, pampMetalPrices),
                SilverMetal = GetPortfolioMetaInfo(portfolioVariantItems, priceGroupDic, PricingAndTradingService.Models.Constants.MetalType.Silver, currency, pampMetalPrices),
                PlatinumMetal = GetPortfolioMetaInfo(portfolioVariantItems, priceGroupDic, PricingAndTradingService.Models.Constants.MetalType.Platinum, currency, pampMetalPrices),
                IsSippContact = _bullionContactHelper.IsSippContact(CustomerContext.Current.CurrentContact),
                MetalAndProductTypeHoldingFormatted = CalculateMetalAndProductTypeHoldingFormatted(priceGroupDic, currency)
            };
        }

        public PortfolioMetalModel GetPortfolioMetalModel(IEnumerable<PortfolioVariantItemModel> portfolioVariants,
            PricingAndTradingService.Models.Constants.MetalType metalType)
        {
            var priceGroupDic = CalculatingPriceForVariantGroup(portfolioVariants);
            var pampMetalPrices = _metalPriceService.GetPampMetalPrices().ToList();
            var currency = _currencyHelper.GetDefaultCurrencyCode();

            return GetPortfolioMetaInfo(portfolioVariants, priceGroupDic, metalType, currency, pampMetalPrices);
        }

        public MetalChartBlockViewModel CreateMetalChartBlockViewModel()
        {
            var currency = _currencyHelper.GetDefaultCurrencyCode();
            var metalSellPrices = HttpContext.Current.Items[MetalSellPricesHttpContextItemKey] as Dictionary<string, decimal> ??
                                  GetMetalSellPrices(currency);

            var portfolioVariantItems = GetPortfolioVariantItems(metalSellPrices, currency);
            var priceGroupDic = CalculatingPriceForVariantGroup(portfolioVariantItems);

            var totalHoldingValue = GetTotalHoldingValueWithUnit(priceGroupDic, currency);

            return new MetalChartBlockViewModel
            {
                MetalHoldingPercentage = CalculateMetalHoldingPercentage(priceGroupDic, totalHoldingValue.Amount),
                MetalHoldingFormatted = CalculateMetalHoldingFormatted(priceGroupDic, currency),
                ProductTypeHoldingPercentage = CalculateProductTypeHoldingPercentage(priceGroupDic, totalHoldingValue.Amount),
                ProductTypeHoldingFormatted = CalculateProductTypeHoldingFormatted(priceGroupDic, currency),
                MetalAndProductTypeHoldingPercentage = CalculateMetalAndProductTypeHoldingPercentage(priceGroupDic, totalHoldingValue.Amount),
                MetalAndProductTypeHoldingFormatted = CalculateMetalAndProductTypeHoldingFormatted(priceGroupDic, currency)
            };
        }
        public bool CreatePortfolioContentsWhenPurchase(IPurchaseOrder purchaseOrder)
        {
            return CreatePortfolioContentsWhenPurchase(purchaseOrder, null);
        }
        public bool CreatePortfolioContentsWhenPurchase(IPurchaseOrder purchaseOrder, CustomerContact customerContact)
        {
            var portfolioContents = new List<IWalletItem>();
            var allLineItems = purchaseOrder.GetAllVaultedItems();

            if (allLineItems == null) return false;

            var lineItems = allLineItems.ToList();
            portfolioContents.AddRange(lineItems.Select(lineItem => new PortfolioContent
            {
                Id = Guid.NewGuid(),
                VariantCode = lineItem.Code,
                QuantityInVault = lineItem.Quantity,
                WeightInVault = (lineItem.Code.GetVariantByCode() as IAmPremiumVariant).ConvertQuantityToWeight(lineItem.Quantity),
                PurchaseDate = purchaseOrder.Created,
                OriginalUnitPrice = lineItem.GetDiscountedPrice(purchaseOrder.Currency, _lineItemCalculator) / lineItem.Quantity,
                Status = StringConstants.PortfolioContentStatus.Pending,
                OriginalPurchaseOrderNumber = purchaseOrder.OrderNumber
            }));

            var currentContactId = customerContact != null ? (Guid)customerContact.PrimaryKeyId : _customerContext.CurrentContactId;

            return _portfolioRepository.CreatePortfolioContentsWhenPurchase(currentContactId, portfolioContents);
        }

        public bool HasOutstandingBuyOrSellOrDeliverTransaction(Guid customerId)
        {
            var transactionTypes = new[] {
                (int)ExportTransactionType.SellFromVault,
                (int)ExportTransactionType.DeliverFromVault,
                (int)ExportTransactionType.PurchaseOrders
            };

            var integratedStatuses = new[]
            {
                IntegrationServices.Constants.StringConstants.AxIntegrationStatus.NotSentToICore,
                IntegrationServices.Constants.StringConstants.AxIntegrationStatus.FailedInAX,
                IntegrationServices.Constants.StringConstants.AxIntegrationStatus.SavedInICoreNode
            };

            var contactIds = new[]
            {
                customerId.ToString()
            };
            //Get all transactions which has status is not sent to AX
            return _exportTransactionsRepository.AnyOutstandingTransactions(transactionTypes, integratedStatuses, contactIds);
        }

        public bool ImportPortfolioFromAx(AxImportData.BullionHoldingsCustomer customerWithStoredItems, Guid customerId)
        {
            var portfolioHeader = new PortfolioHeader
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                LastUpdatedFromAX = customerWithStoredItems.Generated.ToSqlDatetime()
            };

            var portfolioContents = new List<PortfolioContent>();
            foreach (var item in customerWithStoredItems.Items)
            {
                var premiumVariant = item.VariantCode.GetVariantByCode() as IAmPremiumVariant;
                if (premiumVariant == null) continue;

                var qtyItem = premiumVariant.ParseAxQuantityToEpi(item.Qty.ToDecimalExactCulture());

                portfolioContents.Add(new PortfolioContent
                {
                    Id = Guid.NewGuid(),
                    VariantCode = item.VariantCode,
                    QuantityInVault = qtyItem,
                    WeightInVault = premiumVariant.ConvertQuantityToWeight(qtyItem),
                    PurchaseDate = item.PurchaseDate.ToSqlDatetime(),
                    OriginalUnitPrice = premiumVariant.ConvertAxWeightToExWeight(item.OriginalUnitPrice.ToDecimalExactCulture()),
                    Status = item.Status,
                    EpiTransId = item.EpiTransId,
                    OriginalPurchaseOrderNumber = item.EpiTransId.GetOrderNumberFromEpiTransId()
                });
            }

            return _portfolioRepository.ImportPortfolioFromAX(portfolioHeader, portfolioContents);
        }

        private Dictionary<string, decimal> GetMetalSellPrices(Currency currency)
        {
            var indicativeMetalPrices = _premiumCalculator.GetIndicativePrices(currency);
            return indicativeMetalPrices != null ? indicativeMetalPrices.GroupBy(x => x.PampMetal.Code)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.MetalPrice.BuyPrice)) : new Dictionary<string, decimal>();
        }

        public IEnumerable<PortfolioVariantItemModel> GetPortfolioVariantItems()
        {
            var currency = _currencyHelper.GetDefaultCurrencyCode();
            var metalSellPrices = HttpContext.Current.Items[MetalSellPricesHttpContextItemKey] as Dictionary<string, decimal> ??
                                  GetMetalSellPrices(currency);

            return this.GetPortfolioVariantItems(metalSellPrices, currency);
        }

        private IEnumerable<PortfolioVariantItemModel> GetPortfolioVariantItems(Dictionary<string, decimal> metalSellPrices, Currency currency)
        {
            var portfolioVariantItemModels = new List<PortfolioVariantItemModel>();

            var customer = _customerContext.CurrentContact;
            if (customer?.PrimaryKeyId == null) return portfolioVariantItemModels;

            var portfolioContents = _portfolioRepository.GetCustomerWalletItems(customer.PrimaryKeyId.Value, true);
            if (portfolioContents == null || !portfolioContents.Any()) return portfolioVariantItemModels;

            var bullionUnableToSellerDeliverFromVault = customer.GetBooleanProperty(Shared.Constants.StringConstants.CustomFields.BullionUnableToSellorDeliverFromVault);

            foreach (var groupContent in portfolioContents.GroupBy(x => new
            {
                x.VariantCode,
                x.Status
            }))
            {
                var bullionVariant = groupContent.Key.VariantCode.GetVariantByCode() as PreciousMetalsVariantBase;
                if (bullionVariant == null) continue;

                var bullionType = bullionVariant.GetBullionVariantType();

                var currentSellPriceValueWithoutPremium =
                    metalSellPrices.TryGet(bullionVariant.MetalType.GetDescriptionAttribute()) *
                    groupContent.Sum(x => x.WeightInVault);

                var quantityBreak = bullionType.Equals(Enums.BullionVariantType.Signature)
                    ? groupContent.Sum(x => x.WeightInVault)
                    : groupContent.Sum(x => x.QuantityInVault);

                var sellPremium = _bullionSellFromVaultService.GetSellPremium(bullionVariant,
                    currentSellPriceValueWithoutPremium, quantityBreak);

                portfolioVariantItemModels.Add(new PortfolioVariantItemModel
                {
                    Currency = currency,
                    VariantCode = bullionVariant.Code,
                    ImageUrl = bullionVariant.GetDefaultAssetUrl(),
                    Title = bullionVariant.Name,
                    DisplayName = bullionVariant.DisplayName,
                    SubDisplayName = bullionVariant.SubDisplayName,
                    BullionType = bullionType,
                    MetalType = bullionVariant.MetalType,
                    IsLittleTreasure = (bullionVariant as SignatureVariant)?.IsLtProduct ?? false,
                    QuantityInVault = groupContent.Sum(x => x.QuantityInVault),
                    TotalWeightInVault = Math.Round(groupContent.Sum(x => x.WeightInVault), 3),

                    TotalActualPriceValue = groupContent.Sum(x => x.ActualPriceValue),
                    CurrentPriceValue = currentSellPriceValueWithoutPremium - sellPremium,

                    CanDeliver = !bullionUnableToSellerDeliverFromVault && CanShowDeliverToMe(bullionVariant, groupContent),
                    CanSellBack = !bullionUnableToSellerDeliverFromVault && groupContent.Any(x => x.Status.Equals(StringConstants.PortfolioContentStatus.Settled)),

                    EntryUrl = bullionVariant.ContentLink.GetExternalUrl_V2()
                });
            }

            return portfolioVariantItemModels
                .Where(x => (x.BullionType.Equals(Enums.BullionVariantType.Signature) && x.TotalWeightInVault > 0) ||
                          (!x.BullionType.Equals(Enums.BullionVariantType.Signature) && x.QuantityInVault > 0))
                .OrderBy(x => x.BullionType).ThenBy(x => x.MetalType).ThenBy(x => x.VariantCode);
        }

        private bool CanShowDeliverToMe(PreciousMetalsVariantBase bullionVariant, IGrouping<dynamic, IWalletItem> groupPortfolioContents)
        {
            if (bullionVariant is SignatureVariant) return false;

            var physicalVariant = bullionVariant as PhysicalVariantBase;
            if (physicalVariant == null) return false;

            var currentContact = _customerContext.CurrentContact;
            if (currentContact == null || _bullionContactHelper.IsSippContact(currentContact)) return false;

            return groupPortfolioContents.Any(x => x.Status.Equals(StringConstants.PortfolioContentStatus.Settled)) &&
                   physicalVariant.CanDeliver;
        }

        private Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> CalculatingPriceForVariantGroup(
            IEnumerable<PortfolioVariantItemModel> portfolioItems)
        {
            return portfolioItems.GroupBy(x => new { x.BullionType, x.MetalType }).ToDictionary(x =>
                new MetalWithVariantType
                {
                    BullionType = x.Key.BullionType,
                    MetalType = x.Key.MetalType
                }, CalculatingPriceByMetalType);
        }

        private IEnumerable<PortfolioMetalModel> CalculatingPriceByMetalType(
            IEnumerable<PortfolioVariantItemModel> portfolioItems)
        {
            return portfolioItems.GroupBy(x => x.MetalType).Select(x => new PortfolioMetalModel
            {
                Metal = new MetalQuantity
                {
                    Metal = x.Key,
                    QuantityInOz = x.Sum(y => y.TotalWeightInVault)
                },
                HoldingMoneyValue = x.Sum(y => Math.Round(y.CurrentPriceValue, 2))
            });
        }

        private Money GetTotalHoldingValueWithUnit(Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic, Currency currency)
        {
            //Calculate price here: get from PAMP
            return new Money(priceGroupDic.Sum(x => x.Value.Sum(y => y.HoldingMoneyValue)), currency);
        }

        private PortfolioMetalModel GetPortfolioMetaInfo(IEnumerable<PortfolioVariantItemModel> portfolioItems,
            Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic,
            PricingAndTradingService.Models.Constants.MetalType metalType, Currency currency,
            IEnumerable<PampMetalPriceItemViewModel> pampMetalPrices)
        {
            var quantityInOz = portfolioItems.Where(x => x.MetalType == metalType).Sum(x => x.TotalWeightInVault);
            var metalPrice = priceGroupDic.Where(x => x.Key.MetalType == metalType).Sum(x => x.Value.Sum(y => y.HoldingMoneyValue));
            var pampMetalPrice = pampMetalPrices.FirstOrDefault(x => x.Metal.Name.Equals(metalType.ToString()));

            var portfolioMetalModel = new PortfolioMetalModel
            {
                Metal = new MetalQuantity
                {
                    Metal = metalType,
                    Currency = currency,
                    QuantityInOz = quantityInOz
                },
                HoldingMoneyValue = metalPrice,
                HoldingMoneyWithUnit = (new Money(metalPrice, currency)).ToCurrencyString()
            };

            if (pampMetalPrice != null)
            {
                portfolioMetalModel.PriceChange = pampMetalPrice.SellPriceChange;
            }

            return portfolioMetalModel;
        }

        private Dictionary<string, decimal> CalculateMetalHoldingPercentage(Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic, decimal totalHoldingValue)
        {
            if (totalHoldingValue == 0)
                return priceGroupDic.GroupBy(x => x.Key.MetalType).ToDictionary(x => x.Key.ToString(), x => Decimal.Zero);

            return priceGroupDic.GroupBy(x => x.Key.MetalType).ToDictionary(x => x.Key.ToString(), x => x.Sum(y => y.Value.Sum(z => z.HoldingMoneyValue)) / totalHoldingValue * 100);
        }

        private Dictionary<string, string> CalculateMetalHoldingFormatted(Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic, Currency currency)
        {
            return priceGroupDic.GroupBy(x => x.Key.MetalType).ToDictionary(x => x.Key.ToString(), x => (new Money(x.Sum(y => y.Value.Sum(z => z.HoldingMoneyValue)), currency)).ToString());
        }

        private Dictionary<string, decimal> CalculateProductTypeHoldingPercentage(Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic, decimal totalHoldingValue)
        {
            if (totalHoldingValue == 0)
                return priceGroupDic.GroupBy(x => x.Key.MetalType).ToDictionary(x => x.Key.ToString(), x => Decimal.Zero);

            return priceGroupDic.GroupBy(x => x.Key.BullionType).ToDictionary(x => x.Key.ToString(), x => x.Sum(y => y.Value.Sum(z => z.HoldingMoneyValue)) / totalHoldingValue * 100);
        }

        private Dictionary<string, string> CalculateProductTypeHoldingFormatted(Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic, Currency currency)
        {
            return priceGroupDic.GroupBy(x => x.Key.BullionType).ToDictionary(x => x.Key.ToString(), x => (new Money(x.Sum(y => y.Value.Sum(z => z.HoldingMoneyValue)), currency)).ToString());
        }

        private Dictionary<string, decimal> CalculateMetalAndProductTypeHoldingPercentage(Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic, decimal totalHoldingValue)
        {
            if (totalHoldingValue == 0)
                return priceGroupDic.GroupBy(x => x.Key.MetalType).ToDictionary(x => x.Key.ToString(), x => Decimal.Zero);

            return priceGroupDic.ToDictionary(x => $"{x.Key.BullionType}_{x.Key.MetalType}", y => y.Value.Sum(z => z.HoldingMoneyValue) / totalHoldingValue * 100);
        }

        private Dictionary<string, string> CalculateMetalAndProductTypeHoldingFormatted(Dictionary<MetalWithVariantType, IEnumerable<PortfolioMetalModel>> priceGroupDic, Currency currency)
        {
            return priceGroupDic.ToDictionary(x => $"{x.Key.BullionType}_{x.Key.MetalType}", y => (new Money(y.Value.Sum(z => z.HoldingMoneyValue), currency)).ToString());
        }

        private Dictionary<string, string> GetCategoriesFilter()
        {
            var result = new Dictionary<string, string>();
            var categories = Enum.GetValues(typeof(Enums.BullionVariantType)).Cast<Enums.BullionVariantType>();
            foreach (var bullionCategory in categories)
            {
                if (bullionCategory.Equals(Enums.BullionVariantType.None)) continue;
                result.TryAdd(((int)bullionCategory).ToString(), bullionCategory.GetDescriptionAttribute());
            }

            return result;
        }

        private Dictionary<string, string> GetMetalTypesFilter()
        {
            var result = new Dictionary<string, string>();
            var metals = Enum.GetValues(typeof(PricingAndTradingService.Models.Constants.MetalType)).Cast<PricingAndTradingService.Models.Constants.MetalType>();
            foreach (var metalType in metals)
            {
                result.TryAdd(((int)metalType).ToString(), metalType.ToString());
            }

            return result;
        }

        private string GetSellFromVaultUrl()
        {
            var startPage = _customerContext.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.SellBullionDefaultLandingPage == null) return string.Empty;

            return startPage.SellBullionDefaultLandingPage.GetExternalUrl_V2();
        }

        private string GetDeliverFromVaultUrl()
        {
            var startPage = _customerContext.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage == null || startPage.DeliverBullionLandingPage == null) return string.Empty;

            return startPage.DeliverBullionLandingPage.GetExternalUrl_V2();
        }
    }
}