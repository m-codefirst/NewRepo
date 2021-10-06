using System.Collections.Generic;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.EntityFramework.BullionPortfolio;
using TRM.Web.Models.ViewModels.Bullion;

namespace TRM.Web.Services.SellFromVault
{
    public interface IBullionSellFromVaultService
    {
        SellBullionDefaultLandingViewModel BuildSellBullionDefaultLandingViewModel(string variantCode, decimal? sellQuantity = null);
        SellBullionDefaultLandingViewModel BuildSellBullionDefaultLandingViewModel(TrmSellTransaction sellTransaction);
        decimal ConvertSellMoneyToQuantityInOz(string variantCode, decimal sellMoney);
        TrmSellTransaction SaveSellTransaction(SellOrDeliverBullionViewModel sellBullionViewModel, ref Dictionary<string, string> messages);
        /// <summary>
        /// Calculate the sell premium base on quantity be passed. If Signature, pass quantity in Oz (Weight) otherwise pass quantity
        /// </summary>
        /// <param name="premiumVariant"></param>
        /// <param name="priceAmountWithoutPremiums"></param>
        /// <param name="quantityToBreak"></param>
        /// <returns></returns>
        decimal GetSellPremium(PreciousMetalsVariantBase premiumVariant, decimal priceAmountWithoutPremiums, decimal quantityToBreak);
        SellBullionDefaultLandingViewModel BuildSellBullionDefaultLandingViewModelForSingleQuantity(string variantCode);
    }
}