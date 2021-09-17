using System;
using System.Collections.Generic;
using EPiServer.Commerce.Marketing;
using EPiServer.ServiceLocation;

namespace TRM.Web.Business.Promotions
{
    [ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    public class ZeroPricePromotionProcessor : OrderPromotionProcessorBase<ZeroPricePromotion>
    {

        protected override PromotionItems GetPromotionItems(ZeroPricePromotion promotionData)
        {
            throw new NotImplementedException();
        }

        protected override RewardDescription Evaluate(ZeroPricePromotion promotionData, PromotionProcessorContext context)
        {

            var reward = new RewardDescription(FulfillmentStatus.Fulfilled, new List<RedemptionDescription>(), promotionData, 0m, 0m, RewardType.None, promotionData.Description);

            return reward;

        }
    }
}