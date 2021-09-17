using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Extensions;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Extentions.MetalPrice;
using TRM.Web.Helpers;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.Catalog.Bullion;
using Currency = Mediachase.Commerce.Currency;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Business.Pricing
{
    [ServiceConfiguration(typeof(TrmDefaultPremiumCalculator), Lifecycle = ServiceInstanceScope.Transient)]
    [ServiceConfiguration(typeof(IPremiumCalculator<IAmPremiumVariant>), Lifecycle = ServiceInstanceScope.Transient)]
    public class TrmDefaultPremiumCalculator : IPremiumCalculator<IAmPremiumVariant>
    {
        protected readonly Lazy<PampMetalPriceSyncRepository> PampMetalPriceSyncRepository = new Lazy<PampMetalPriceSyncRepository>(() => ServiceLocator.Current.GetInstance<PampMetalPriceSyncRepository>());
        public IEnumerable<PampMetalPriceResult> GetIndicativePrices(Currency currency, bool noCache = false)
        {
            var pricesInCurrency = PampMetalPriceSyncRepository.Value.GetLastMetalPriceListInCurrency(currency.ToString());
            if (noCache)
            {
                pricesInCurrency = PampMetalPriceSyncRepository.Value.GetLivePrices(currency.ToString());
            }

            if (pricesInCurrency == null || !pricesInCurrency.Any()) return null;

            return pricesInCurrency.Select(x => new PampMetalPriceResult
            {
                MetalPrice = x,
                PampMetal = x.GetPampMetal()
            });
        }

        #region PremiumSetting

        public IAmBullionPremiumSetting GetPremiumSetting(
            IAmPremiumVariant premium,
            CustomerContact customerContact,
            Currency currency,
            bool isSell = false,
            Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> customPremiumSettingSelector = null)
        {

            var premiumGroupInteger = customerContact == null
                ? 0
                : customerContact.GetIntegerProperty(StringConstants.CustomFields.BullionPremiumGroupInt);

            return GetPremiumSetting(premium, premiumGroupInteger, currency, isSell, customPremiumSettingSelector);
        }

        public IAmBullionPremiumSetting GetPremiumSetting(
           IAmPremiumVariant premium,
           int customerPremiumGroupInt,
           Currency currency,
           bool isSell = false,
           Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> customPremiumSettingSelector = null)
        {
            if (premium == null) return null;

            var premiumSettingSelector = customPremiumSettingSelector ??
                (isSell ? DefaultGetBuyPremiumSettingItemsSelector : DefaultGetSellPremiumSettingItemsSelector);

            IEnumerable<IAmBullionPremiumSetting> settings = premiumSettingSelector.Invoke(premium);
            if (settings == null || !settings.Any()) return null;

            var defaultSetting = settings.LastOrDefault(x => x.Currencies != null && x.Currencies.Contains(currency.ToString()));

            if (customerPremiumGroupInt == 0) return defaultSetting;
            
            var setting = settings.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.CustomerPremiumGroups)  && x.CustomerPremiumGroups.Split(',').Contains(customerPremiumGroupInt.ToString()) &&
                x.Currencies != null && x.Currencies.Contains(currency.ToString()));

            return setting ?? defaultSetting;
        }

        private static readonly Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> DefaultGetBuyPremiumSettingItemsSelector =
            (IAmPremiumVariant premium) =>
            {
                return premium.BuyPremiums?.FilteredItems.Select(x => x.GetContent()).OfType<IAmBullionPremiumSetting>();
            };

        private static readonly Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> DefaultGetSellPremiumSettingItemsSelector =
            (IAmPremiumVariant premium) =>
            {
                return premium.SellPremiums?.FilteredItems.Select(x => x.GetContent()).OfType<IAmBullionPremiumSetting>();
            };

        #endregion

        #region QuantityBreakSetting

        public IAmQuantityBreakSetting GetQuantityBreakSetting(
            IAmPremiumVariant premium,
            IAmBullionPremiumSetting premiumSetting,
            decimal investmentAmountBreak,
            decimal quantityBreak,
            Currency currency,
            Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> customQuantityBreakSelector = null)
        {
            if (premium == null) return null;

            var quantityBreakSettings = premiumSetting as IHaveQuantityBreakSetting;
            if (quantityBreakSettings == null) return null;

            var quantityBreakSelector = customQuantityBreakSelector ?? DefaultQuantityBreakSelector;
            var quantityBreakSetting = quantityBreakSettings.QuantityBreak?.FilteredItems
                .Select(x => x.GetContent())
                .OfType<IAmQuantityBreakSetting>()
                .LastOrDefault(x => quantityBreakSelector.Invoke(premium, quantityBreakSettings, x, investmentAmountBreak, quantityBreak));

            return quantityBreakSetting;
        }

        private static readonly Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> DefaultQuantityBreakSelector =
            (p, s, x, investmentAmountBreak, quantityBreak) =>
            {
                if (p is PhysicalVariantBase)
                    return x != null && x.MinimumPhysicalQuantity <= quantityBreak;

                if (p is VirtualVariantBase)
                    return x != null && x.MinimumVirtualValue <= investmentAmountBreak;

                return false;
            };

        private readonly IBullionPremiumGroupHelper _bullionPremiumGroupHelper;

        public TrmDefaultPremiumCalculator(IBullionPremiumGroupHelper bullionPremiumGroupHelper)
        {
            _bullionPremiumGroupHelper = bullionPremiumGroupHelper;
        }

        #endregion

        #region GetPremimum

        public IPremiumCalculatorResult GetPremiumForPhysicalVariant(
            IAmPremiumVariant premium, decimal originalPrice, decimal quantityBreak,
            CustomerContact customerContact, Currency currency, bool isSell = false, decimal requestedQuantity = 1,
            Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> customPremiumSettingSelector = null,
            Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> customQuantityBreakSelector = null)
        {
            // Flow to get premium
            var premiumSetting = GetPremiumSetting(premium, customerContact, currency, isSell, customPremiumSettingSelector);

            var quantityBreakSetting = GetQuantityBreakSetting(
                    premium, premiumSetting, 0, quantityBreak,
                    currency, customQuantityBreakSelector);

            return GetPremium(premium, premiumSetting, quantityBreakSetting, currency, originalPrice, requestedQuantity);
        }

        public IPremiumCalculatorResult GetPremiumForPhysicalVariant(
           IAmPremiumVariant premium, decimal originalPrice, decimal quantityBreak,
           int premiumGroupInt, Currency currency, bool isSell = false, decimal requestedQuantity = 1,
           Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> customPremiumSettingSelector = null,
           Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> customQuantityBreakSelector = null)
        {
            // Flow to get premium
            var premiumSetting = GetPremiumSetting(premium, premiumGroupInt, currency, isSell, customPremiumSettingSelector);

            var quantityBreakSetting = GetQuantityBreakSetting(
                    premium, premiumSetting, 0, quantityBreak,
                    currency, customQuantityBreakSelector);

            return GetPremium(premium, premiumSetting, quantityBreakSetting, currency, originalPrice, requestedQuantity);
        }

        public IPremiumCalculatorResult GetPremium(
            IAmPremiumVariant premium,
            IAmBullionPremiumSetting premiumSetting,
            IAmQuantityBreakSetting quantityBreakSetting,
            Currency currency,
            decimal originalPrice,
            decimal requestedQuantity = 1)
        {
            var premiumCalculatorResult = new PremiumCalculatorResult(
                    premiumSetting,
                    quantityBreakSetting,
                    originalPrice);

            if (premium == null || premiumSetting == null) return premiumCalculatorResult;

            if (quantityBreakSetting == null) return premiumCalculatorResult;

            var premiumValue = quantityBreakSetting.PremiumValue;

            if (quantityBreakSetting.PremiumType == Enums.PremiumType.Monetary)
            {
                premiumCalculatorResult.PriceIncludedPremium = new Money(originalPrice + premiumValue * requestedQuantity, currency);
                premiumCalculatorResult.PremiumValue = premiumValue * requestedQuantity;
            }

            if (quantityBreakSetting.PremiumType == Enums.PremiumType.Percentage)
            {
                premiumCalculatorResult.PriceIncludedPremium = new Money(originalPrice + originalPrice * premiumValue / 100, currency);
                premiumCalculatorResult.PremiumValue = originalPrice * premiumValue / 100;
            }

            return premiumCalculatorResult;
        }

        #endregion
    }
}