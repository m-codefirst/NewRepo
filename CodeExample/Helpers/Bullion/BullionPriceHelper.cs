using Castle.Core.Internal;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Web.Business.Pricing;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Helpers.Bullion
{
    [ServiceConfiguration(typeof(IBullionPriceHelper), Lifecycle = ServiceInstanceScope.Transient)]
    public class BullionPriceHelper : IBullionPriceHelper
    {
        private readonly IPremiumCalculator<IAmPremiumVariant> _premiumCalculator;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly CustomerContext _customerContext;

        public BullionPriceHelper(IPremiumCalculator<IAmPremiumVariant> premiumCalculator,
            IAmCurrencyHelper currencyHelper,
            CustomerContext customerContext)
        {
            _premiumCalculator = premiumCalculator;
            _currencyHelper = currencyHelper;
            _customerContext = customerContext;
        }

        public Money GetFromPriceForPhysicalVariant(IAmPremiumVariant currentContent)
        {
            var originalPrice = GetPricePerUnitForBullionVariant(currentContent, null, true);

            var currency = _currencyHelper.GetDefaultCurrencyCode();
            var premium = _premiumCalculator.GetPremiumForPhysicalVariant(currentContent, originalPrice.Amount, int.MaxValue,
                _customerContext.CurrentContact, currency, true, 1, null, GetHightestQuantityBreakSelector);

            return premium != null ? new Money(premium.PriceIncludedPremium, currency) : new Money(0, currency);
        }

        public Money GetFromPriceForBullionVariant(IAmPremiumVariant bullionVariant, Currency currency, int premiumGroupInt)
        {
            if (bullionVariant == null) return new Money();
            if (bullionVariant is PhysicalVariantBase)
            {
                return GetFromPriceForPhysicalVariant(bullionVariant, currency, premiumGroupInt);
            }

            var signatureVariant = bullionVariant as SignatureVariant;
            if (signatureVariant != null)
            {
                return GetFromPriceForSignatureVariant(signatureVariant, currency);
            }
            return new Money();
        }

        private Money GetFromPriceForPhysicalVariant(IAmPremiumVariant bullionVariant, Currency currency, int premiumGroupInt)
        {
            var originalPrice = GetPricePerUnitForBullionVariant(bullionVariant, currency, true);
            var premium = _premiumCalculator.GetPremiumForPhysicalVariant(bullionVariant, originalPrice.Amount, int.MaxValue,
                premiumGroupInt, currency, true, 1, null, GetHightestQuantityBreakSelector);

            return premium != null ? new Money(premium.PriceIncludedPremium, currency) : new Money(0, currency);
        }

        private Money GetFromPriceForSignatureVariant(SignatureVariant bullionVariant, Currency currency)
        {
            var minSpendConfigs = bullionVariant.MinSpendConfigs;
            if (minSpendConfigs.IsNullOrEmpty() || minSpendConfigs.All(x => x.Currency != currency.CurrencyCode)) return new Money(decimal.Zero, currency);
            var amount = minSpendConfigs.First(x => x.Currency == currency.CurrencyCode).Amount;
            return new Money(amount, currency.CurrencyCode);
        }

        public Money GetPricePerUnitForBullionVariant(IAmPremiumVariant currentContent, Currency? currency = null, bool isPampSell = false)
        {
            var price = GetPricePerOneOzFromPamp(currentContent, currency, isPampSell);

            var indicativePrice = currentContent.TroyOzWeightConfiguration * price.Amount;

            var coinVariant = currentContent as CoinVariant;
            if (coinVariant != null)
            {
                var tubeConfiguration = coinVariant.CoinTubeQuantity.GetValueOrDefault();
                if (tubeConfiguration > 0)
                {
                    indicativePrice = indicativePrice * tubeConfiguration;
                }
            }

            return new Money(indicativePrice, price.Currency);
        }

        public Money GetPhysicalVariantPriceIncludedPremium(IAmPremiumVariant currentContent, int quantity, bool isPampSell = false)
        {
            var currency = _currencyHelper.GetDefaultCurrencyCode();

            var originalPrice = GetPricePerUnitForBullionVariant(currentContent, currency, isPampSell);

            var premium = _premiumCalculator.GetPremiumForPhysicalVariant(currentContent, originalPrice.Amount, quantity,
                _customerContext.CurrentContact, currency, isPampSell);

            return premium != null ? new Money(premium.PriceIncludedPremium, currency) : new Money(decimal.Zero, currency);
        }

        //default get sell price from pamp
        public Money GetSignaturePricePerOneOzIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerOzWithoutPremium)
        {
            return GetSignaturePricePerOneOzIncludedPremium(null, currentContent, investmentAmount, pricePerOzWithoutPremium);
        }
        public Money GetSignaturePricePerOneOzIncludedPremium(CustomerContact customerContact, IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerOzWithoutPremium)
        {
            return GetSignaturePriceIncludedPremium(customerContact, currentContent, investmentAmount, pricePerOzWithoutPremium, true);
        }

        public Money GetSignaturePricePerUnitIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerUnitWithoutPremium)
        {
            return GetSignaturePricePerUnitIncludedPremium(currentContent,  investmentAmount,  pricePerUnitWithoutPremium, null);
        }
        public Money GetSignaturePricePerUnitIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerUnitWithoutPremium, CustomerContact customerContact)
        {
            return GetSignaturePriceIncludedPremium(customerContact, currentContent, investmentAmount, pricePerUnitWithoutPremium, true);
        }

        public Money GetPricePerOneOzFromPamp(IAmPremiumVariant currentContent, Currency? currency = null, bool isPampSell = false)
        {
            if (currency == null || !currency.HasValue)
            {
                currency = _currencyHelper.GetDefaultCurrencyCode();
            }
            //get indicative Metal Prices from PAMP
            var indicativeMetalPrices = _premiumCalculator.GetIndicativePrices(currency.Value);

            var price = indicativeMetalPrices?.FirstOrDefault(x => x.PampMetal != null && x.PampMetal.Code.Equals(currentContent.MetalType.GetDescriptionAttribute()));

            if (price == null) return new Money(decimal.Zero, currency.Value);

            var matchPrice = isPampSell ? price.MetalPrice.SellPrice : price.MetalPrice.BuyPrice;

            return new Money(matchPrice, currency.Value);
        }

        public Money ApplyBuyPremiumsForPhysicalItem(decimal priceAmountWithoutPremiums, IAmPremiumVariant currentContent, decimal quantity, bool isPampSell = false)
        {
            return ApplyBuyPremiumsForPhysicalItem(null, priceAmountWithoutPremiums, currentContent, quantity, isPampSell);
        }
        public Money ApplyBuyPremiumsForPhysicalItem(CustomerContact customerContact, decimal priceAmountWithoutPremiums, IAmPremiumVariant currentContent, decimal quantity, bool isPampSell = false)
        {
            var currency = _currencyHelper.GetDefaultCurrencyCode(customerContact);

            var premiumCalculatorResult = _premiumCalculator.GetPremiumForPhysicalVariant(currentContent, priceAmountWithoutPremiums,
                quantity, _customerContext.CurrentContact, currency, true, quantity);

            return new Money(premiumCalculatorResult.PriceIncludedPremium, currency);
        }

        public Money GetSignaturePriceIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount, decimal priceWithoutPremium, bool isPampSell = false)
        {
            return GetSignaturePriceIncludedPremium(null, currentContent, investmentAmount, priceWithoutPremium, isPampSell);
        }
        public Money GetSignaturePriceIncludedPremium(CustomerContact customerContact, IAmPremiumVariant currentContent, decimal investmentAmount, decimal priceWithoutPremium, bool isPampSell = false)
        {
            var currency = _currencyHelper.GetDefaultCurrencyCode(customerContact);

            //get premium setting
            var premiumSetting = _premiumCalculator.GetPremiumSetting(currentContent, customerContact ?? _customerContext.CurrentContact, currency, isPampSell);

            //get investment break based on investment amount
            var investmentBreakSetting = _premiumCalculator.GetQuantityBreakSetting(currentContent, premiumSetting, investmentAmount, 1, currency);

            //calculate price per one oz including premium
            var premium = _premiumCalculator.GetPremium(currentContent, premiumSetting, investmentBreakSetting, currency, priceWithoutPremium);
            return premium != null ? new Money(premium.PriceIncludedPremium, currency) : new Money(decimal.Zero, currency);
        }

        private static readonly Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> GetHightestQuantityBreakSelector =
            (p, s, x, placedPrice, quantity) =>
            {
                var sources = s.QuantityBreak?.FilteredItems.Select(t => t.GetContent()).OfType<IAmQuantityBreakSetting>();

                if (p is PhysicalVariantBase)
                    return CustomPhysicalVariantQuantityBreakSelector.Invoke(sources, x, quantity);

                return false;
            };

        private static readonly Func<IEnumerable<IAmQuantityBreakSetting>, IAmQuantityBreakSetting, decimal, bool> CustomPhysicalVariantQuantityBreakSelector =
            (sources, x, quantity) =>
            {
                return x != null && CustomPhysicalVariantQuantityBreakValueSelector.Invoke(x) >= sources.Max(s => s.MinimumPhysicalQuantity);
            };

        private static readonly Func<IAmQuantityBreakSetting, decimal> CustomPhysicalVariantQuantityBreakValueSelector =
            x => x.MinimumPhysicalQuantity;
    }
}