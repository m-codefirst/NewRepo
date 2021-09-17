using System;
using System.Collections.Generic;
using EPiServer.Commerce.Marketing;
using EPiServer.ServiceLocation;
using EPiServer.Commerce.Marketing.Promotions;
using EPiServer.Framework.Localization;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using Mediachase.Commerce;
using EPiServer.Commerce.Marketing.Internal;
using Mediachase.Commerce.Markets;

namespace TRM.Web.Business.Promotions.Bullion
{
    //[ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    //public class BullionPremiumPriceOrderPromotionProcessor : OrderPromotionProcessorBase<BullionPremiumPriceOrderPromotion>
    //{
    //    private readonly SpendAmountGetOrderDiscountProcessor _baseProcessor;

    //    public BullionPremiumPriceOrderPromotionProcessor(SpendAmountGetOrderDiscountProcessor baseProcessor)
    //    {
    //        _baseProcessor = baseProcessor;
    //    }

    //    protected override RewardDescription Evaluate(BullionPremiumPriceOrderPromotion promotionData, PromotionProcessorContext context)
    //    {
    //        // TODO: Custom promotion for Premium here!
    //        return _baseProcessor.Evaluate(promotionData, context);
    //    }

    //    protected override PromotionItems GetPromotionItems(BullionPremiumPriceOrderPromotion promotionData)
    //    {
    //        return _baseProcessor.GetPromotionItems(promotionData);
    //    }
    //}

    //[ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    //public class BullionPremiumPriceLinePromotionProcessor : EntryPromotionProcessorBase<BullionPremiumPriceLinePromotion>
    //{
    //    private readonly SpendAmountGetItemDiscountProcessor _baseProcessor;

    //    public BullionPremiumPriceLinePromotionProcessor(
    //        SpendAmountGetItemDiscountProcessor baseProcessor,
    //        RedemptionDescriptionFactory redemptionDescriptionFactory)
    //        : base(redemptionDescriptionFactory)
    //    {
    //        _baseProcessor = baseProcessor;
    //    }

    //    protected override RewardDescription Evaluate(BullionPremiumPriceLinePromotion promotionData, PromotionProcessorContext context)
    //    {
    //        // TODO: Custom promotion for Premium here!
    //        return _baseProcessor.Evaluate(promotionData, context);
    //    }

    //    protected override PromotionItems GetPromotionItems(BullionPremiumPriceLinePromotion promotionData)
    //    {
    //        return _baseProcessor.GetPromotionItems(promotionData);
    //    }
    //}

    //[ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    //public class BullionPremiumPriceShippingPromotionProcessor : SpendAmountGetFreeShippingProcessor
    //{
    //    public BullionPremiumPriceShippingPromotionProcessor(
    //        FulfillmentEvaluator fulfillmentEvaluator,
    //        LocalizationService localizationService) : base(fulfillmentEvaluator, localizationService)
    //    {
    //    }
    //}
}