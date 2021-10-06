using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Shared.Extensions;
using PricingAndTradingService.Models;
using TrmWallet;
using TrmWallet.Entity;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Pricing;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Models.ViewModels.Bullion;

namespace TRM.Web.Services.SellFromVault
{
    [ServiceConfiguration(typeof(IBullionSellFromVaultService), Lifecycle = ServiceInstanceScope.Transient)]
    public class BullionSellFromVaultService : IBullionSellFromVaultService
    {
        private readonly IUserService _userService;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly IPremiumCalculator<IAmPremiumVariant> _premiumCalculator;
        private readonly CustomerContext _customerContext;
        private readonly ITrmWalletRepository _walletRepository;
        private readonly IBullionTradingService _bullionTradingService;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly IGlobalPurchaseLimitService _globalPurchaseLimitService;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionSellFromVaultService(
            IUserService userService,
            IAmCurrencyHelper currencyHelper,
            IPremiumCalculator<IAmPremiumVariant> premiumCalculator,
            CustomerContext customerContext,
            ITrmWalletRepository walletRepository,
            IBullionTradingService bullionTradingService,
            IBullionPriceHelper bullionPriceHelper,
            IGlobalPurchaseLimitService globalPurchaseLimitService,
            IAmTransactionHistoryHelper transactionHistoryHelper, IAmBullionContactHelper bullionContactHelper)
        {
            _userService = userService;
            _currencyHelper = currencyHelper;
            _premiumCalculator = premiumCalculator;
            _customerContext = customerContext;
            _walletRepository = walletRepository;
            _bullionTradingService = bullionTradingService;
            _bullionPriceHelper = bullionPriceHelper;
            _globalPurchaseLimitService = globalPurchaseLimitService;
            _transactionHistoryHelper = transactionHistoryHelper;
            _bullionContactHelper = bullionContactHelper;
        }

        public SellBullionDefaultLandingViewModel BuildSellBullionDefaultLandingViewModel(string variantCode, decimal? sellQuantity = null)
        {
            var premiumVariant = variantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return null;

            var portfolioItems = _walletRepository.GetCustomerWalletItems(_customerContext.CurrentContactId, true);
            if (portfolioItems == null || !portfolioItems.Any()) return null;

            var portfolioItemsByCode = portfolioItems.Where(x => x.VariantCode.Equals(variantCode)).ToList();

            var sellViewModel = premiumVariant.GetBullionVariantType() == Enums.BullionVariantType.Signature ?
                CreateModelForSignatureVariant(premiumVariant, portfolioItemsByCode, sellQuantity) :
                CreateModelForPhysicalVariant(premiumVariant, portfolioItemsByCode, sellQuantity);

            if (sellViewModel != null)
                sellViewModel.UnableToSell = _customerContext.CurrentContact.GetBooleanProperty(Shared.Constants
                    .StringConstants.CustomFields.BullionUnableToSellorDeliverFromVault);

            return sellViewModel;
        }

        public SellBullionDefaultLandingViewModel BuildSellBullionDefaultLandingViewModel(TrmSellTransaction sellTransaction)
        {
            var premiumVariant = sellTransaction.VariantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return null;

            var sellTotalAmount = sellTransaction.MetalPrice - sellTransaction.SellPremium;

            var combinedWeightInSale = premiumVariant.GetBullionVariantType().Equals(Enums.BullionVariantType.Signature) ?
                sellTransaction.RequestedQuantityToSell : sellTransaction.RequestedQuantityToSell * premiumVariant.TroyOzWeight;

            var metalPricePerOneOz = sellTotalAmount / combinedWeightInSale;

            return new SellBullionDefaultLandingViewModel
            {
                SellVariant = new SellOrDeliverVariantViewModel(premiumVariant)
                {
                    AvailableToSell = sellTransaction.RequestedQuantityToSell
                },
                SellTransactionNumber = $"{sellTransaction.TransactionNumberPrefix}{sellTransaction.TransactionNumber}",
                PremiumPricePerOzIncludingPremium = new Money(metalPricePerOneOz, _bullionContactHelper.GetDefaultCurrencyCode(_customerContext.CurrentContact)),
                CombinedWeightInSale = combinedWeightInSale,
                SellTotal = new Money(sellTotalAmount, _currencyHelper.GetDefaultCurrencyCode()),
                BullionAccountDashboardUrl = sellTransaction.GetMyAccountPageUrl()
            };
        }

        private SellBullionDefaultLandingViewModel CreateModelForPhysicalVariant(PreciousMetalsVariantBase premiumVariant, IEnumerable<IWalletItem> portfolioItemsByCode, decimal? sellQuantity)
        {
            var quantityInVault = portfolioItemsByCode.Sum(x => x.QuantityInVault);

            var quantityAvailableToSell = portfolioItemsByCode
                .Where(x => x.Status.Equals(StringConstants.PortfolioContentStatus.Settled))
                .Sum(x => x.QuantityInVault);

            if (sellQuantity == null || sellQuantity > quantityAvailableToSell) sellQuantity = quantityAvailableToSell;

            var premiumRequestId = Guid.Empty;

            var sellTotalAmountResponse = GetSellTotalAmount(premiumVariant, sellQuantity.Value, out premiumRequestId);
            //var sellTotalAmount = sellQuantity != 0 ? GetSellTotalAmount(premiumVariant, sellQuantity.Value, out premiumRequestId) : 0;
            var sellTotalAmount = sellQuantity != 0 ? sellTotalAmountResponse.SellTotalAmount : 0;
            var combinedWeightInSale = sellQuantity.Value * premiumVariant.TroyOzWeight;

            var metalPricePerOneOz = sellQuantity != 0 ? sellTotalAmount / combinedWeightInSale : 0;

            return new SellBullionDefaultLandingViewModel
            {
                SellVariant = new SellOrDeliverVariantViewModel(premiumVariant)
                {
                    QuantityInVault = quantityInVault,
                    AvailableToSell = quantityAvailableToSell,
                    QuantityToSellOrDeliver = sellQuantity.Value
                },
                WalletItems = portfolioItemsByCode.Where(x => x.QuantityInVault > 0 && x.Status.Equals(StringConstants.PortfolioContentStatus.Settled)).ToList(),
                PremiumPricePerOzIncludingPremium = new Money(metalPricePerOneOz, _bullionContactHelper.GetDefaultCurrencyCode(_customerContext.CurrentContact)),
                CombinedWeightInSale = Math.Round(combinedWeightInSale, 3),
                PremiumRequestId = premiumRequestId,
                SellTotal = new Money(sellTotalAmount, _currencyHelper.GetDefaultCurrencyCode()),
                SellPremiumValue = sellTotalAmountResponse.SellPremiumValue
            };
        }

        private SellBullionDefaultLandingViewModel CreateModelForSignatureVariant(PreciousMetalsVariantBase premiumVariant, IEnumerable<IWalletItem> portfolioItemsByCode, decimal? sellQuantityInOz)
        {
            var quantityInVault = portfolioItemsByCode.Sum(x => x.WeightInVault);

            var quantityAvailableToSell = portfolioItemsByCode
                .Where(x => x.Status.Equals(StringConstants.PortfolioContentStatus.Settled))
                .Sum(x => x.WeightInVault);

            if (sellQuantityInOz == null || sellQuantityInOz > quantityAvailableToSell) sellQuantityInOz = quantityAvailableToSell;

            var premiumRequestId = Guid.Empty;
            var sellTotalAmountResponse = GetSellTotalAmount(premiumVariant, sellQuantityInOz.Value, out premiumRequestId);
            //var sellTotalAmount = sellQuantityInOz != 0 ? GetSellTotalAmount(premiumVariant, sellQuantityInOz.Value, out premiumRequestId) : 0;
            var sellTotalAmount = sellQuantityInOz != 0 ? sellTotalAmountResponse.SellTotalAmount : 0;

            var metalPricePerOneOz = sellQuantityInOz != 0 ? sellTotalAmount / sellQuantityInOz.Value : 0;

            return new SellBullionDefaultLandingViewModel
            {
                SellVariant = new SellOrDeliverVariantViewModel(premiumVariant)
                {
                    QuantityInVault = quantityInVault,
                    AvailableToSell = quantityAvailableToSell,
                    QuantityToSellOrDeliver = sellQuantityInOz.Value
                },
                WalletItems = portfolioItemsByCode.Where(x => x.WeightInVault > 0 && x.Status.Equals(StringConstants.PortfolioContentStatus.Settled)).ToList(),
                PremiumPricePerOzIncludingPremium = new Money(metalPricePerOneOz, _bullionContactHelper.GetDefaultCurrencyCode(_customerContext.CurrentContact)),
                CombinedWeightInSale = Math.Round(sellQuantityInOz.Value, 3),
                PremiumRequestId = premiumRequestId,
                SellTotal = new Money(sellTotalAmount, _currencyHelper.GetDefaultCurrencyCode()),
                SellPremiumValue = sellTotalAmountResponse.SellPremiumValue
            };
        }

        public TrmSellTransaction SaveSellTransaction(SellOrDeliverBullionViewModel sellBullionViewModel, ref Dictionary<string, string> messages)
        {
            var premiumVariant = sellBullionViewModel.VariantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return null;

            var variantType = premiumVariant.GetBullionVariantType();
            //Finish the quote
            var requestId = new Guid(sellBullionViewModel.PremiumRequestId);
            var executedQuote = _bullionTradingService.FinishQuoteRequest(requestId);
            if (executedQuote?.Result == null || !executedQuote.Result.Success)
            {
                messages.TryAdd(Enums.SellOrDeliverFromVaultIssue.CanNotFinishQuote.ToString(), Enums.SellOrDeliverFromVaultIssue.CanNotFinishQuote.GetDescriptionAttribute());
                return null;
            }

            var existingQuoteResponse = _bullionTradingService.GetResponseFromExistingPampRequest(requestId);
            if (existingQuoteResponse == null)
            {
                messages.TryAdd(Enums.SellOrDeliverFromVaultIssue.CanNotRequestQuote.ToString(), Enums.SellOrDeliverFromVaultIssue.CanNotRequestQuote.GetDescriptionAttribute());
                return null;
            }

            var metalPrices = _bullionTradingService.GetPampPriceForMetalsFromQuoteResponse(existingQuoteResponse);

            var sellPremium = variantType.Equals(Enums.BullionVariantType.Signature) ?
                GetSellPremiumForSignature(premiumVariant, metalPrices[premiumVariant.MetalType], sellBullionViewModel.QuantityToSell) :
                GetSellPremiumForPhysical(premiumVariant, metalPrices[premiumVariant.MetalType], sellBullionViewModel.QuantityToSell);

            //Tracking impersonate
            var impersonateUser = _userService.GetImpersonateUser();

            var currentContact = _customerContext.CurrentContact;
            var sellTransaction = new TrmSellTransaction
            {
                TransactionNumberPrefix = sellBullionViewModel.TransactionNumberPrefix,
                CustomerId = _customerContext.CurrentContactId,
                Customer = currentContact,
                RequestedQuantityToSell = sellBullionViewModel.QuantityToSell,
                VariantCode = sellBullionViewModel.VariantCode,
                TransactionPerformedBy = impersonateUser != null ? impersonateUser.UserType : Shared.Constants.StringConstants.Customer,
                TransactionPerformedByUser = impersonateUser?.UserId,
                Currency = _bullionContactHelper.GetDefaultCurrencyCode(currentContact),
                MetalPrice = metalPrices[premiumVariant.MetalType],
                UnitMetalWeightOz = premiumVariant.TroyOzWeight,
                SellPremium = sellPremium,
                PampQuoteId = executedQuote.QuoteId,
                WalletItems = sellBullionViewModel.WalletItems,
                Status = Enums.TransactionHistoryStatus.Inprogress.GetDescriptionAttribute()
            };

            var result = _walletRepository.SaveTrmSellTransaction(sellTransaction);
            if (result)
            {
                UpdateSellGlobalPurchaseLimit(premiumVariant, sellTransaction);
                UpdateContactBalances(sellTransaction);
                // save transacation history
                _transactionHistoryHelper.LogTheSellFromVaultTransactionHistory(sellTransaction);
                return sellTransaction;
            }

            messages.TryAdd(Enums.SellOrDeliverFromVaultIssue.CanNotSaveDB.ToString(), Enums.SellOrDeliverFromVaultIssue.CanNotSaveDB.GetDescriptionAttribute());
            return null;
        }

        public decimal GetSellPremium(PreciousMetalsVariantBase premiumVariant, decimal priceAmountWithoutPremiums, decimal quantityToBreak)
        {
            if (premiumVariant is SignatureVariant)
            {
                return GetSellPremiumForSignature(premiumVariant, priceAmountWithoutPremiums, quantityToBreak);
            }

            return GetSellPremiumForPhysical(premiumVariant, priceAmountWithoutPremiums, quantityToBreak);
        }

        private void UpdateSellGlobalPurchaseLimit(IAmPremiumVariant premiumVariant, ITrmSellTransaction sellTransaction)
        {
            var bullionTradeType = premiumVariant is VirtualVariantBase
                ? Enums.BullionTradeType.SignatureSell
                : Enums.BullionTradeType.PhysicalSell;

            var quantityInOz = premiumVariant is VirtualVariantBase
                ? sellTransaction.RequestedQuantityToSell
                : sellTransaction.RequestedQuantityToSell * premiumVariant.TroyOzWeightConfiguration;

            _globalPurchaseLimitService.UpdateMetalPurchaseLimit(premiumVariant.MetalType.ToString(), quantityInOz, bullionTradeType);
        }

        private void UpdateContactBalances(ITrmSellTransaction sellTransaction)
        {
            var contact = _customerContext.CurrentContact;
            var amount = sellTransaction.MetalPrice - sellTransaction.SellPremium;
            _bullionContactHelper.UpdateBalances(contact, amount);
        }

        private SellTotalAmountResponse GetSellTotalAmount(PreciousMetalsVariantBase premiumVariant, decimal sellQuantity, out Guid premiumRequestId)
        {
            var variantType = premiumVariant.GetBullionVariantType();
            var metalQuantities = new List<MetalQuantity>()
            {
                new MetalQuantity
                {
                    Currency = _bullionContactHelper.GetDefaultCurrencyCode(_customerContext.CurrentContact),
                    Metal = premiumVariant.MetalType,
                    QuantityInOz = variantType.Equals(Enums.BullionVariantType.Signature) ? sellQuantity : sellQuantity * premiumVariant.TroyOzWeight
                }
            };

            var quoteResponse = _bullionTradingService.RequestPampForQuote(metalQuantities, PricingAndTradingService.Models.Constants.DealerTradeSide.PampBuys);
            if (quoteResponse?.MetalPriceMap == null)
            {
                premiumRequestId = Guid.Empty;
                return new SellTotalAmountResponse { SellPremiumValue = 0, SellTotalAmount = 0 };
                //return 0;
            }

            var metalPrices = _bullionTradingService.GetPampPriceForMetalsFromQuoteResponse(quoteResponse);

            var sellPremium = GetSellPremium(premiumVariant, metalPrices[premiumVariant.MetalType], sellQuantity);

            premiumRequestId = quoteResponse.QuoteDtoId;

            return new SellTotalAmountResponse
            {
                SellTotalAmount = metalPrices[premiumVariant.MetalType] - sellPremium,
                SellPremiumValue = sellPremium
            };
            //return metalPrices[premiumVariant.MetalType] - sellPremium;
        }

        public SellBullionDefaultLandingViewModel BuildSellBullionDefaultLandingViewModelForSingleQuantity(string variantCode)
        {
            var singleQuantity = 1;
            var premiumVariant = variantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return null;

            var portfolioItems = _walletRepository.GetCustomerWalletItems(_customerContext.CurrentContactId, true);
            if (portfolioItems == null || !portfolioItems.Any()) return null;

            var portfolioItemsByCode = portfolioItems.Where(x => x.VariantCode.Equals(variantCode)).ToList();

            var sellViewModel = premiumVariant.GetBullionVariantType() == Enums.BullionVariantType.Signature ?
                CreateModelForSignatureVariant(premiumVariant, portfolioItemsByCode, singleQuantity) :
                CreateModelForPhysicalVariant(premiumVariant, portfolioItemsByCode, singleQuantity);

            if (sellViewModel != null)
                sellViewModel.UnableToSell = _customerContext.CurrentContact.GetBooleanProperty(Shared.Constants
                    .StringConstants.CustomFields.BullionUnableToSellorDeliverFromVault);

            return sellViewModel;
        }

        private decimal GetSellPremiumForPhysical(IAmPremiumVariant currentContent, decimal priceAmountWithoutPremiums, decimal quantity)
        {
            var currentContact = _customerContext.CurrentContact;
            var priceIncludedPremium = _premiumCalculator.GetPremiumForPhysicalVariant(currentContent, priceAmountWithoutPremiums,
                quantity, currentContact, _bullionContactHelper.GetDefaultCurrencyCode(currentContact), false, quantity);

            return priceIncludedPremium.PremiumValue;
        }

        private decimal GetSellPremiumForSignature(IAmPremiumVariant currentContent, decimal priceAmountWithoutPremiums, decimal quantityInOz)
        {
            var pampBuyPricePerOneOz = _bullionPriceHelper.GetPricePerOneOzFromPamp(currentContent, null, false);
            var indicativeInvestmentValue = quantityInOz * pampBuyPricePerOneOz;
            var priceIncludePremium = _bullionPriceHelper.GetSignaturePriceIncludedPremium(currentContent,
                indicativeInvestmentValue, priceAmountWithoutPremiums, false);

            return priceIncludePremium - priceAmountWithoutPremiums;
        }

        public decimal ConvertSellMoneyToQuantityInOz(string variantCode, decimal sellMoney)
        {
            if (string.IsNullOrEmpty(variantCode)) return decimal.Zero;

            var premiumVariant = variantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return decimal.Zero;

            var indicativeBuyPrice = _bullionPriceHelper.GetPricePerOneOzFromPamp(premiumVariant, null, false);
            var priceIncludePremium = _bullionPriceHelper.GetSignaturePriceIncludedPremium(premiumVariant, sellMoney, sellMoney, false);
            return indicativeBuyPrice.Amount != 0 ? (priceIncludePremium.Amount / indicativeBuyPrice.Amount).RoundDown(3) : decimal.Zero;
        }
    }
}