using EPiServer.Commerce.Marketing;
using EPiServer.DataAnnotations;

namespace TRM.Web.Business.Promotions
{
    [ContentType(DisplayName = "Zero Price Promotion", GUID = "21991501-0f1b-44ab-a4ca-15cc065f16b6", Description = "Adds a promotion code to the basket with no discount")]
    [ImageUrl("Images/BuyQuantityPayFixedAmount.png")]
    public class ZeroPricePromotion : OrderPromotion
    {

    }
}