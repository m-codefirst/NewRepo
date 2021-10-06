using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Shared.Extensions;
using TrmWallet;
using TrmWallet.Entity;
using TRM.Web.Business.Pricing.BullionTax;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Models;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Models.ViewModels.Bullion;

namespace TRM.Web.Services.DeliverFromVault
{
    [ServiceConfiguration(typeof(IBullionDeliverFromVaultService), Lifecycle = ServiceInstanceScope.Transient)]
    public class BullionDeliverFromVaultService : IBullionDeliverFromVaultService
    {
        private readonly IUserService _userService;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly CustomerContext _customerContext;
        private readonly ITrmWalletRepository _walletRepository;
        private readonly IBullionTaxService _bullionTaxService;
        private readonly IAmShippingMethodHelper _shippingMethodHelper;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionDeliverFromVaultService(
            IUserService userService,
            IAmCurrencyHelper currencyHelper,
            CustomerContext customerContext,
            ITrmWalletRepository walletRepository,
            IBullionTaxService bullionTaxService,
            IAmShippingMethodHelper shippingMethodHelper,
            IAmTransactionHistoryHelper transactionHistoryHelper, IAmBullionContactHelper bullionContactHelper)
        {
            _userService = userService;
            _currencyHelper = currencyHelper;
            _customerContext = customerContext;
            _walletRepository = walletRepository;
            _bullionTaxService = bullionTaxService;
            _shippingMethodHelper = shippingMethodHelper;
            _transactionHistoryHelper = transactionHistoryHelper;
            _bullionContactHelper = bullionContactHelper;
        }

        public DeliverBullionLandingViewModel BuildDeliverBullionLandingViewModel(string variantCode, decimal? quantityToDeliver = null)
        {
            var premiumVariant = variantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return null;

            var portfolioItems = _walletRepository.GetCustomerWalletItems(_customerContext.CurrentContactId, true);
            if (portfolioItems == null) return null;

            var walletItems = portfolioItems.ToList();
            if (!walletItems.Any()) return null;

            var portfolioItemsByCode = walletItems.Where(x => x.VariantCode.Equals(variantCode) && x.QuantityInVault > 0).ToList();

            var quantityInVault = portfolioItemsByCode.Sum(x => x.QuantityInVault);

            var quantityAvailableToDeliver = portfolioItemsByCode
                .Where(x => x.Status.Equals(StringConstants.PortfolioContentStatus.Settled))
                .Sum(x => x.QuantityInVault);

            if (quantityToDeliver == null || quantityToDeliver > quantityAvailableToDeliver) quantityToDeliver = quantityAvailableToDeliver;

            var deliverTotals = CalculateDeliverTotals(portfolioItemsByCode, premiumVariant, quantityToDeliver.Value);

            var currentContact = _customerContext.CurrentContact;
            var deliverBullionViewModel = new DeliverBullionLandingViewModel
            {
                DeliverVariant = new SellOrDeliverVariantViewModel(premiumVariant)
                {
                    QuantityInVault = quantityInVault,
                    AvailableToSell = quantityAvailableToDeliver,
                    QuantityToSellOrDeliver = quantityToDeliver.Value,
                    Weight = Math.Round(quantityToDeliver.Value * premiumVariant.TroyOzWeightConfiguration, 3)
                },
                DeliverAddress = _bullionContactHelper.GetBullionAddress(currentContact).GetAddressString(),
                InvestmentVat = deliverTotals.InvestmentVat,
                ShippingMethod = deliverTotals.ShippingMethod,
                DeliverCost = deliverTotals.DeliverCost,
                DeliverVat = deliverTotals.DeliverVat,
                DeliveryTotal = deliverTotals.DeliveryTotal,
                WalletItems = portfolioItemsByCode.Where(x => x.Status.Equals(StringConstants.PortfolioContentStatus.Settled)).ToList(),
                AvailableWalletBalance = _bullionContactHelper.GetMoneyAvailableToInvest(currentContact),
                UnableToDeliver = _customerContext.CurrentContact.GetBooleanProperty(Shared.Constants.StringConstants.CustomFields.BullionUnableToSellorDeliverFromVault)
            };

            return deliverBullionViewModel;
        }

        public DeliverBullionLandingViewModel BuildDeliverBullionLandingViewModel(ITrmDeliverTransaction deliverTransaction)
        {
            var premiumVariant = deliverTransaction.VariantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return null;
            var currentContact = _customerContext.CurrentContact;
            var currency = _bullionContactHelper.GetDefaultCurrencyCode(currentContact);
            return new DeliverBullionLandingViewModel
            {
                DeliverVariant = new SellOrDeliverVariantViewModel(premiumVariant)
                {
                    QuantityToSellOrDeliver = deliverTransaction.QuantityToDeliver,
                    Weight = Math.Round(deliverTransaction.QuantityToDeliver * premiumVariant.TroyOzWeightConfiguration, 3)
                },
                DeliverTransactionNumber = $"{deliverTransaction.TransactionNumberPrefix}{deliverTransaction.TransactionNumber}",
                DeliverAddress = _bullionContactHelper.GetBullionAddress(currentContact).GetAddressString(),
                DeliverCost = new Money(deliverTransaction.ShippingFee, currency),
                DeliverVat = new Money(deliverTransaction.ShippingFeeVAT, currency),
                DeliveryTotal = new Money(deliverTransaction.TotalAmount, currency),
                InvestmentVat = new Money(deliverTransaction.TotalAmount - deliverTransaction.ShippingFeeVAT - deliverTransaction.ShippingFee, currency),
                BullionAccountDashboardUrl = deliverTransaction.GetMyAccountPageUrl(),
                ShippingMethod = new ShippingMethodSummary
                {
                    FriendlyName = deliverTransaction.ShippingMethod
                }
            };
        }

        public TrmDeliverTransaction SaveDeliverTransaction(SellOrDeliverBullionViewModel deliverBullionViewModel,
            ref Dictionary<string, string> messages)
        {
            var premiumVariant = deliverBullionViewModel.VariantCode.GetVariantByCode() as PreciousMetalsVariantBase;
            if (premiumVariant == null) return null;

            var portfolioItems = _walletRepository.GetCustomerWalletItems(_customerContext.CurrentContactId, true);
            if (portfolioItems == null) return null;

            var walletItems = portfolioItems.ToList();
            if (!walletItems.Any()) return null;

            //Calculate totals and get quantity to deliver for each portfolio items
            var deliverTotals = CalculateDeliverTotals(walletItems, premiumVariant, deliverBullionViewModel.QuantityToSell);

            var vatRule = _bullionTaxService.GetVatRule(Enums.BullionActionType.DeliverMetalFromVault,
                premiumVariant.MetalType, premiumVariant.GetBullionVariantType());
            var shippingVatRule = _bullionTaxService.GetVatRule(Enums.BullionActionType.DeliveryFee, premiumVariant.MetalType);

            //Update quantity to deliver for each portfolio item
            foreach (var walletItem in deliverBullionViewModel.WalletItems)
            {
                var originalItem = walletItems.FirstOrDefault(x => x.Id.Equals(walletItem.Id));
                if (originalItem != null) walletItem.QuantityToDeliver = originalItem.QuantityToDeliver;
            }

            //Tracking impersonate
            var impersonateUser = _userService.GetImpersonateUser();
            var currentContact = _customerContext.CurrentContact;
            var deliverTransaction = new TrmDeliverTransaction
            {
                TransactionNumberPrefix = deliverBullionViewModel.TransactionNumberPrefix,
                CustomerId = _customerContext.CurrentContactId,
                Customer = currentContact,
                QuantityToDeliver = deliverBullionViewModel.QuantityToSell,
                VariantCode = deliverBullionViewModel.VariantCode,
                Currency = _bullionContactHelper.GetDefaultCurrencyCode(currentContact),
                TransactionPerformedBy = impersonateUser != null ? impersonateUser.UserType : Shared.Constants.StringConstants.Customer,
                TransactionPerformedByUser = impersonateUser?.UserId,
                TotalAmount = deliverTotals.DeliveryTotal.Amount,
                ShippingMethod = deliverTotals.ShippingMethod.FriendlyName,
                CarriageCode = deliverTotals.ShippingMethod.ObsMethodName,
                ShippingFee = deliverTotals.DeliverCost,
                ShippingFeeVAT = deliverTotals.DeliverVat,
                ShippingVATCode = shippingVatRule != null ? shippingVatRule.VatCode : string.Empty,
                VatCode = vatRule.VatCode,
                VatRate = _bullionTaxService.GetVatRateAmount(_bullionContactHelper.GetBullionAddress(currentContact).CountryCode, vatRule.VatCode),
                WalletItems = deliverBullionViewModel.WalletItems
            };

            var result = _walletRepository.SaveTrmDeliverTransaction(deliverTransaction);
            if (result)
            {
                UpdateContactBalances(deliverTransaction);

                // Save the transaction history
                _transactionHistoryHelper.LogTheDeliveryFromVaultTransactionHistory(deliverTransaction);
                return deliverTransaction;
            }

            messages.TryAdd(Enums.SellOrDeliverFromVaultIssue.CanNotSaveDB.ToString(), Enums.SellOrDeliverFromVaultIssue.CanNotSaveDB.GetDescriptionAttribute());
            return null;

        }

        private DeliverBullionLandingViewModel CalculateDeliverTotals(IEnumerable<IWalletItem> portfolioItems, PreciousMetalsVariantBase premiumVariant, decimal quantityToDeliver)
        {
            var chargePortfolioItems = GetWalletItemsWillBeDeliver(portfolioItems, premiumVariant.Code, quantityToDeliver);
            var currencyCode = _currencyHelper.GetDefaultCurrencyCode();

            var deliverTotals = new DeliverBullionLandingViewModel
            {
                InvestmentVat =
                    new Money(CalculateInvestmentVat(chargePortfolioItems, premiumVariant, quantityToDeliver),
                        currencyCode)
            };
            deliverTotals.ShippingMethod = GetShippingMethod(premiumVariant, quantityToDeliver, deliverTotals.InvestmentVat.Amount, currencyCode);
            deliverTotals.DeliverCost = new Money(deliverTotals.ShippingMethod?.DeliveryCostDecimal ?? 0, currencyCode);
            deliverTotals.DeliverVat = new Money(CalculateDeliverVat(premiumVariant, deliverTotals.DeliverCost.Amount), currencyCode);
            deliverTotals.DeliveryTotal = deliverTotals.InvestmentVat + deliverTotals.DeliverCost + deliverTotals.DeliverVat;

            return deliverTotals;
        }

        private IEnumerable<IWalletItem> GetWalletItemsWillBeDeliver(IEnumerable<IWalletItem> sourceItems, string variantCode, decimal quantity)
        {
            var resultItems = new List<IWalletItem>();
            var stackItems = sourceItems
                .Where(x => x.VariantCode.Equals(variantCode) &&
                            x.QuantityInVault > 0 &&
                            x.Status.Equals(StringConstants.PortfolioContentStatus.Settled))
                .OrderBy(x => x.PurchaseDate)
                .ToList();

            foreach (var stackItem in stackItems)
            {
                if (quantity == 0) break;

                if (0 < stackItem.QuantityInVault && stackItem.QuantityInVault <= quantity)
                {
                    quantity = quantity - stackItem.QuantityInVault;
                    stackItem.QuantityToDeliver = stackItem.QuantityInVault;
                    resultItems.Add(stackItem);
                    continue;
                }

                if (stackItem.QuantityInVault <= quantity) continue;

                stackItem.QuantityToDeliver = quantity;
                quantity = 0;
                resultItems.Add(stackItem);
            }

            return resultItems;
        }

        private void UpdateContactBalances(ITrmDeliverTransaction sellTransaction)
        {
            var contact = _customerContext.CurrentContact;
            var amount = sellTransaction.TotalAmount;
            _bullionContactHelper.UpdateBalances(contact , - amount, -amount, -amount);
        }

        private ShippingMethodSummary GetShippingMethod(PreciousMetalsVariantBase variant, decimal quantity, decimal investmentAmount, string currencyCode)
        {
            return _shippingMethodHelper.GetShippingMethodForDeliverFromVault(
                _customerContext.CurrentContact, investmentAmount, (decimal)variant.Weight * quantity, currencyCode);
        }

        private decimal CalculateDeliverVat(PreciousMetalsVariantBase variant, decimal deliverCost)
        {
            var vatRule = _bullionTaxService.GetVatRule(Enums.BullionActionType.DeliveryFee, variant.MetalType);
            if (vatRule == null) return 0;

            var vatRate = _bullionTaxService.GetVatRateAmount(_bullionContactHelper.GetBullionAddress(_customerContext.CurrentContact).CountryCode, vatRule.VatCode);
            return deliverCost > 0 ? deliverCost * vatRate / 100 : 0;
        }

        private decimal CalculateInvestmentVat(IEnumerable<IWalletItem> portfolioItems, PreciousMetalsVariantBase variant, decimal quantity)
        {
            var vatRate = GetInvestmentVatRate(variant);
            return portfolioItems.Sum(x => x.OriginalUnitPrice * x.QuantityToDeliver * vatRate / 100);
        }

        private decimal GetInvestmentVatRate(PreciousMetalsVariantBase variant)
        {
            var vatRule = _bullionTaxService.GetVatRule(Enums.BullionActionType.DeliverMetalFromVault, variant.MetalType, variant.GetBullionVariantType());
            return vatRule == null ? 0 : _bullionTaxService.GetVatRateAmount(_bullionContactHelper.GetBullionAddress(_customerContext.CurrentContact).CountryCode, vatRule.VatCode);
        }
    }
}