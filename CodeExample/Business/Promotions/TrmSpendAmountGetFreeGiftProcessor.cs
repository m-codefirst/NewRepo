using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Framework.Localization;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Marketing.Promotions;
using EPiServer.Commerce.Order;
using EPiServer.Core.Internal;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Business.Promotions
{
    public class TrmSpendAmountGetFreeGiftProcessor : SpendAmountGetGiftItemsProcessor
    {
        private readonly GiftItemFactory _giftItemFactory;
        private readonly ContentLoader _contentLoader;

        public TrmSpendAmountGetFreeGiftProcessor(FulfillmentEvaluator fulfillmentEvaluator,
            GiftItemFactory giftItemFactory, LocalizationService localizationService,
            RedemptionDescriptionFactory redemptionDescriptionFactory, ContentLoader contentLoader)
            : base(fulfillmentEvaluator, giftItemFactory, localizationService, redemptionDescriptionFactory)
        {
            _giftItemFactory = giftItemFactory;
            _contentLoader = contentLoader;
        }
        
        protected override RewardDescription Evaluate(SpendAmountGetGiftItems promotionData, PromotionProcessorContext context)
        {
            if (!context.OrderForm.Shipments.Any() ||
                !context.OrderForm.Shipments.SelectMany(x => x.LineItems).Any() ||
                !IsWithinRedemtionLimits(promotionData))
            {
                return NotFulfilledRewardDescription(promotionData, context, FulfillmentStatus.NotFulfilled);
            }

            var qualifyingAmountForCurrency = promotionData.Condition.Amounts.First(x => x.Currency == context.OrderGroup.Currency);
            var allTrmLineItems = context.OrderForm.Shipments.SelectMany(x => x.LineItems).Where(x => !(x.GetEntryContent() is PreciousMetalsVariantBase)).ToList();
            var allBullionLineItems = context.OrderForm.Shipments.SelectMany(x => x.LineItems).Where(x => x.GetEntryContent() is PreciousMetalsVariantBase).ToList();

            if (!allTrmLineItems.Any() && !allBullionLineItems.Any())
            {
                return NotFulfilledRewardDescription(promotionData, context, FulfillmentStatus.NotFulfilled);
            }

            var redemptions = new List<RedemptionDescription>();
            if (qualifyingAmountForCurrency <= allTrmLineItems.Sum(x => x.PlacedPrice * x.Quantity))
            {
                redemptions.AddRange(GetRedemption<TrmVariant>(promotionData, context));
            }

            if (qualifyingAmountForCurrency <= allBullionLineItems.Sum(x => x.PlacedPrice * x.Quantity))
            {
                redemptions.AddRange(GetRedemption<PreciousMetalsVariantBase>(promotionData, context));
            }

            if (!redemptions.Any())
            {
                return NotFulfilledRewardDescription(promotionData, context, FulfillmentStatus.NotFulfilled);
            }

            return RewardDescription.CreateGiftItemsReward(FulfillmentStatus.Fulfilled, redemptions, promotionData, "fullfilled");
        }

        private List<RedemptionDescription> GetRedemption<T>(SpendAmountGetGiftItems promotionData, PromotionProcessorContext context) where T : EntryContentBase
        {
            var redemptionDescriptionList = new List<RedemptionDescription>();

            var giftItemsOfType = promotionData.GiftItems.Select(x => _contentLoader.Get<EntryContentBase>(x) as T).Where(x => x != null).ToList();

            if (!giftItemsOfType.Any()) return redemptionDescriptionList;

            var giftItems = _giftItemFactory.CreateGiftItems(giftItemsOfType.Select(x => x.ContentLink), context);

            redemptionDescriptionList.Add(CreateRedemptionDescription(giftItems));

            return redemptionDescriptionList;
        }

        private bool IsWithinRedemtionLimits(SpendAmountGetGiftItems promotionData)
        {
            if (promotionData.RedemptionLimits.PerCustomer.HasValue &&
                promotionData.RedemptionLimits.PerCustomer < 1)
            {
                return false;
            }

            if (promotionData.RedemptionLimits.PerOrderForm.HasValue &&
                promotionData.RedemptionLimits.PerOrderForm < 1)
            {
                return false;
            }

            if (promotionData.RedemptionLimits.PerPromotion.HasValue &&
                promotionData.RedemptionLimits.PerPromotion < 1)
            {
                return false;
            }

            return true;
        }
    }
}
