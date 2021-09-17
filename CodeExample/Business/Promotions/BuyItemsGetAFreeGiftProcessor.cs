using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Marketing;
using EPiServer.Core.Internal;
using EPiServer.ServiceLocation;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Business.Promotions
{
    [ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    public class BuyItemsGetAFreeGiftProcessor : EntryPromotionProcessorBase<BuyItemsGetAFreeGift>
    {
        private readonly ContentLoader _contentLoader;
        private readonly GiftItemFactory _giftItemFactory;

        public BuyItemsGetAFreeGiftProcessor(RedemptionDescriptionFactory redemptionDescriptionFactory,
            ContentLoader contentLoader, GiftItemFactory giftItemFactory) :
            base(redemptionDescriptionFactory)
        {
            _contentLoader = contentLoader;
            _giftItemFactory = giftItemFactory;
        }

        protected override PromotionItems GetPromotionItems(BuyItemsGetAFreeGift promotionData)
        {
            return new PromotionItems(promotionData,
                new CatalogItemSelection(promotionData.Items, CatalogItemSelectionType.Specific, true),
                new CatalogItemSelection(promotionData.GiftItems, CatalogItemSelectionType.Specific, true));
        }

        protected override RewardDescription Evaluate(BuyItemsGetAFreeGift promotionData, PromotionProcessorContext context)
        {
            if (!context.OrderForm.Shipments.Any() || 
                !context.OrderForm.Shipments.SelectMany(x => x.LineItems).Any() ||
                promotionData.RequiredQuantity == 0 ||
                promotionData.Items == null ||
                !promotionData.Items.Any())
            {
                    return NotFulfilledRewardDescription(promotionData, context, FulfillmentStatus.NotFulfilled);
            }

            var allQualifyingItems = new List<EntryContentBase>();
            var promotionDataCatalogContent = promotionData.Items.Select(x => _contentLoader.Get<CatalogContentBase>(x));
            foreach (var pdCatalogContent in promotionDataCatalogContent)
            {
                var ecb = pdCatalogContent as EntryContentBase;
                if (ecb != null)
                {
                    allQualifyingItems.Add(ecb);
                }
                else
                {
                    var ncb = pdCatalogContent as NodeContentBase;
                    if (ncb != null)
                    {
                        var children = _contentLoader.GetChildren<EntryContentBase>(ncb.ContentLink);
                        allQualifyingItems.AddRange(children);
                    }
                }
            }

            var trmQualifyingItems = allQualifyingItems.Where(x => !(x is PreciousMetalsVariantBase)).Select(x => x.Code);
            var bullionQualifyingItems = allQualifyingItems.Where(x => x is PreciousMetalsVariantBase).Select(x => x.Code);
            
            var allLineItems = context.OrderForm.Shipments.SelectMany(x => x.LineItems).ToList();
            var itemsInCart = allLineItems.Select(x => x.Code).ToList();
            var trmPromotionItemsInCart = trmQualifyingItems.Intersect(itemsInCart).ToList();
            var bullionPromotionItemsInCart = bullionQualifyingItems.Intersect(itemsInCart).ToList();
            
            if (!trmPromotionItemsInCart.Any() && !bullionPromotionItemsInCart.Any())
            {
                return NotFulfilledRewardDescription(promotionData, context, FulfillmentStatus.NotFulfilled);
            }

            var numberOfTrmQualifyingItems = allLineItems.Where(x => trmPromotionItemsInCart.Contains(x.Code)).Sum(x => x.Quantity);
            var numberOfBullionQualifyingItems = allLineItems.Where(x => bullionPromotionItemsInCart.Contains(x.Code)).Sum(x => x.Quantity);

            var numberOfTrmGiftItemsToAdd = CheckRedemtionLimits(promotionData, (int)numberOfTrmQualifyingItems / promotionData.RequiredQuantity);
            var numberOfBullionGiftItemsToAdd = CheckRedemtionLimits(promotionData, (int)numberOfBullionQualifyingItems / promotionData.RequiredQuantity);

            if (numberOfTrmGiftItemsToAdd == decimal.Zero && numberOfBullionGiftItemsToAdd == decimal.Zero)
            {
                return NotFulfilledRewardDescription(promotionData, context, FulfillmentStatus.NotFulfilled);
            }

            var trmRedemptions = GetRedemptions(promotionData, context, numberOfTrmGiftItemsToAdd, false);
            var bullionRedemptions = GetRedemptions(promotionData, context, numberOfBullionGiftItemsToAdd, true);

            var redemptions = trmRedemptions.Concat(bullionRedemptions);

            return RewardDescription.CreateGiftItemsReward(FulfillmentStatus.Fulfilled, redemptions, promotionData, "fullfilled");
        }

        private int CheckRedemtionLimits(BuyItemsGetAFreeGift promotionData, int numberOfGiftItemsToAdd)
        {
            var value = numberOfGiftItemsToAdd;

            if (promotionData.RedemptionLimits.PerCustomer.HasValue &&
                promotionData.RedemptionLimits.PerCustomer < value)
            {
                value = promotionData.RedemptionLimits.PerCustomer.Value;
            }

            if (promotionData.RedemptionLimits.PerOrderForm.HasValue &&
                promotionData.RedemptionLimits.PerOrderForm < value)
            {
                value = promotionData.RedemptionLimits.PerOrderForm.Value;
            }

            if (promotionData.RedemptionLimits.PerPromotion.HasValue &&
                promotionData.RedemptionLimits.PerPromotion < value)
            {
                value = promotionData.RedemptionLimits.PerPromotion.Value;
            }

            return Math.Min(value, numberOfGiftItemsToAdd);
        }

        private IEnumerable<RedemptionDescription> GetRedemptions(BuyItemsGetAFreeGift promotionData, PromotionProcessorContext context, decimal numberOfGiftItemsToAdd, bool isPreciousMetalsVariantBase)
        {
            var redemptionDescriptionList = new List<RedemptionDescription>();

            var allGiftItems = promotionData.GiftItems.Select(x => _contentLoader.Get<TrmVariant>(x));

            var giftItemsOfType = isPreciousMetalsVariantBase
                ? allGiftItems.Where(x => x is PreciousMetalsVariantBase).ToList()
                : allGiftItems.Where(x => !(x is PreciousMetalsVariantBase)).ToList();

            if (!giftItemsOfType.Any()) return redemptionDescriptionList;

            var giftItems = _giftItemFactory.CreateGiftItems(giftItemsOfType.Select(x => x.ContentLink), context);
            for (var i = 0; i < numberOfGiftItemsToAdd; i++)
            {
                redemptionDescriptionList.Add(CreateRedemptionDescription(giftItems));
            }
            return redemptionDescriptionList;
        }
    }
}
