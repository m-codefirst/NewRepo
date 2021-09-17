using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Business.Pricing
{
    public interface IPremiumCalculator<TPremium> where TPremium : IAmPremiumVariant
    {
        IEnumerable<PampMetalPriceResult> GetIndicativePrices(Currency currency, bool noCache = false);

        IAmBullionPremiumSetting GetPremiumSetting(
            IAmPremiumVariant premium,
            CustomerContact customerContact,
            Currency currency,
            bool isSell = false,
            Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> customPremiumSettingSelector = null);

        IAmQuantityBreakSetting GetQuantityBreakSetting(
            TPremium premium,
            IAmBullionPremiumSetting premiumSetting,
            decimal investmentAmountBreak,
            decimal quantityBreak,
            Currency currency,
            Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> customQuantityBreakSelector = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="premium">The bullion variant</param>
        /// <param name="originalPrice"></param>
        /// <param name="quantityBreak">The Quantity to get QuantityBreakSetting</param>
        /// <param name="customerContact"></param>
        /// <param name="currency"></param>
        /// <param name="isPampSell"></param>
        /// <param name="requestedQuantity">The Quantity to get premium</param>
        /// <param name="customPremiumSettingSelector"></param>
        /// <param name="customQuantityBreakSelector"></param>
        /// <returns></returns>
        IPremiumCalculatorResult GetPremiumForPhysicalVariant(
            TPremium premium,
            decimal originalPrice,
            decimal quantityBreak,
            CustomerContact customerContact,
            Currency currency,
            bool isPampSell = false,
            decimal requestedQuantity = 1,
            Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> customPremiumSettingSelector = null,
            Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> customQuantityBreakSelector = null);

        IPremiumCalculatorResult GetPremiumForPhysicalVariant(
           IAmPremiumVariant premium,
           decimal originalPrice,
           decimal quantityBreak,
           int premiumGroupInt,
           Currency currency,
           bool isSell = false,
           decimal requestedQuantity = 1,
           Func<IAmPremiumVariant, IEnumerable<IAmBullionPremiumSetting>> customPremiumSettingSelector = null,
           Func<IAmPremiumVariant, IHaveQuantityBreakSetting, IAmQuantityBreakSetting, decimal, decimal, bool> customQuantityBreakSelector = null);

        IPremiumCalculatorResult GetPremium(
            TPremium premium,
            IAmBullionPremiumSetting premiumSetting,
            IAmQuantityBreakSetting quantityBreakSetting,
            Currency currency,
            decimal originalPrice,
            decimal requestedQuantity = 1);
    }
}
