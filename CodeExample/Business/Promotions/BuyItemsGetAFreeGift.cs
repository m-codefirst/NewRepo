using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Marketing.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Business.Promotions
{
    [ContentType(DisplayName = "Buy Items Get A Free Gift", GUID = "DA3AF983-ADE0-4E1D-9451-04DF2CEF1028", GroupName = "Free Gift Promotions", Order = 10501)]
    [ImageUrl("Images/SpendAmountGetGiftItems.png")]
    public class BuyItemsGetAFreeGift : EntryPromotion
    {
        [PromotionRegion("Condition")]
        [Display(Name = "Number of Items.", Order = 10)]
        [Range(1, 99)]
        public virtual int RequiredQuantity { get; set; }

        [DistinctList]
        [PromotionRegion("Condition")]
        [AllowedTypes(typeof(TrmVariant), typeof(TrmCategory))]
        [Display(Name = "Qualifying Items.", Order = 15)]
        public virtual IList<ContentReference> Items { get; set; }

        [PromotionRegion("Reward")]
        [AllowedTypes(typeof(TrmVariant))]
        [Display(Name = "Gift Item(s)", Order = 20)]
        public virtual IList<ContentReference> GiftItems { get; set; }
    }
}