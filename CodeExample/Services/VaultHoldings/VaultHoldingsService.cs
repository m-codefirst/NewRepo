using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion.Portfolio;
using TRM.Web.Services.Portfolio;
using MetalType = PricingAndTradingService.Models.Constants.MetalType;

namespace TRM.Web.Services.VaultHoldings
{
    public class VaultHoldingsService : IVaultHoldingsService
    {
        private readonly IBullionPortfolioService _bullionPortfolioService;

        public VaultHoldingsService(IBullionPortfolioService bullionPortfolioService)
        {
            _bullionPortfolioService = bullionPortfolioService;
        }

        public VaultHoldingsDto GetVaultHoldings(CustomerContact contact)
        {
            var portfolioVariantItems = _bullionPortfolioService.GetPortfolioVariantItems().ToList();
            var currency = contact.PreferredCurrency;
            var result = new VaultHoldingsDto
            {
                Gold = new VaultHoldingsMetalGroupDto
                {
                    Bars = ToGroupDto(currency, portfolioVariantItems, MetalType.Gold, Enums.BullionVariantType.Bar),
                    Coins = ToGroupDto(currency, portfolioVariantItems, MetalType.Gold, Enums.BullionVariantType.Coin),
                    Digital = ToGroupDto(currency, portfolioVariantItems, MetalType.Gold, Enums.BullionVariantType.Signature),
                    LittleTreasure = ToGroupDto(currency, portfolioVariantItems, MetalType.Gold, Enums.BullionVariantType.Signature, true)
                },
                Silver = new VaultHoldingsMetalGroupDto
                {
                    Bars = ToGroupDto(currency, portfolioVariantItems, MetalType.Silver, Enums.BullionVariantType.Bar),
                    Coins = ToGroupDto(currency, portfolioVariantItems, MetalType.Silver, Enums.BullionVariantType.Coin),
                    Digital = ToGroupDto(currency, portfolioVariantItems, MetalType.Silver, Enums.BullionVariantType.Signature),
                },
                Platinum = new VaultHoldingsMetalGroupDto
                {
                    Bars = ToGroupDto(currency, portfolioVariantItems, MetalType.Platinum, Enums.BullionVariantType.Bar),
                    Coins = ToGroupDto(currency, portfolioVariantItems, MetalType.Platinum, Enums.BullionVariantType.Coin),
                    Digital = ToGroupDto(currency, portfolioVariantItems, MetalType.Platinum, Enums.BullionVariantType.Signature),
                }
            };

            ApplyAmountsPerMetal(result, contact.PreferredCurrency);
            ApplyPriceChanges(result, portfolioVariantItems);

            return result;
        }

        private void ApplyPriceChanges(VaultHoldingsDto result, List<PortfolioVariantItemModel> portfolioVariantItems)
        {
            result.Gold.PriceChange = _bullionPortfolioService.GetPortfolioMetalModel(portfolioVariantItems, MetalType.Gold).PriceChange;
            result.Silver.PriceChange = _bullionPortfolioService.GetPortfolioMetalModel(portfolioVariantItems, MetalType.Silver).PriceChange;
            result.Platinum.PriceChange = _bullionPortfolioService.GetPortfolioMetalModel(portfolioVariantItems, MetalType.Platinum).PriceChange;
        }

        private void ApplyAmountsPerMetal(VaultHoldingsDto result, string currency)
        {
            result.Gold.Amount = new Money(Sum(result.Gold), currency);
            result.Silver.Amount = new Money(Sum(result.Silver), currency);
            result.Platinum.Amount = new Money(Sum(result.Platinum), currency);
        }

        private decimal Sum(VaultHoldingsMetalGroupDto metalGroup)
        {
            return new List<VaultHoldingsTypeGroupDto> { metalGroup.Bars, metalGroup.Coins, metalGroup.Digital, metalGroup.LittleTreasure }
                .Where(x => x != null).Sum(x => x.Amount);
        }

        private VaultHoldingsTypeGroupDto ToGroupDto(string currency, List<PortfolioVariantItemModel> portfolioVariantItems,
            PricingAndTradingService.Models.Constants.MetalType metalType, Enums.BullionVariantType type, bool isLt = false)
        {
            var items = portfolioVariantItems
                .Where(x => x.MetalType == metalType)
                .Where(x => x.BullionType == type)
                .Where(x => x.IsLittleTreasure == isLt);

            return new VaultHoldingsTypeGroupDto
            {
                Amount = new Money(items.Sum(x => x.CurrentPriceValue), currency)
            };
        }
    }
}