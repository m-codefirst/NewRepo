using System;
using System.Collections.Generic;
using EPiServer.Commerce.Order;
using System.Linq;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using TRM.Web.Constants;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.ViewModels.Cart;
using EPiServer.Commerce.Marketing;
using Hephaestus.Commerce.Helpers;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using static TRM.Shared.Constants.StringConstants;
using TRM.Shared.Extensions;

namespace TRM.Web.Extentions
{
    public static class OrderGroupExtensions
    {
        private static Injected<IAmStoreHelper> _storeHelper;
        private static Injected<ReferenceConverter> _referenceConverter;

        /// <summary>
        /// Return sum of entry discount and order discount
        /// </summary>
        /// <param name="orderGroup"></param>
        /// <returns></returns>
        public static Money GetSavedAmountWithoutDelivery(this IOrderGroup orderGroup)
        {
            var allOrderPromotions = orderGroup.Forms
                .SelectMany(x => x.Promotions)
                .Where(x => x.DiscountType != DiscountType.Shipping)
                .ToList();
            
            var savedAmount = allOrderPromotions.Sum(x => x.SavedAmount);

            foreach (var promotion in allOrderPromotions)
            {
                if (promotion.RewardType == RewardType.Gift)
                {
                    foreach (var entry in promotion.Entries)
                    {
                        if (entry.SavedAmount > decimal.Zero) continue;

                        var contentLink = _referenceConverter.Service.GetContentLink(entry.EntryCode);
                        savedAmount += _storeHelper.Service.GetDiscountPrice(contentLink, 1).Money.Amount;
                    }
                }
            }

            return new Money(savedAmount, orderGroup.Currency);
        }

        public static List<PromotionViewModel> GetOrderPromotionSummary(this IOrderGroup orderGroup)
        {
            var promotions = new List<PromotionViewModel>();

            if (orderGroup == null) return promotions;

            foreach (var form in orderGroup.Forms)
            {
                foreach (var promo in form.Promotions)
                {
                    //if (promotions.Any(p => p.Code == (string.IsNullOrWhiteSpace(promo.CouponCode) ? promo.Name : promo.CouponCode))) continue;
                    if (AlreadyIncludePromotion(promotions, promo)) continue;

                    promotions.Add(new PromotionViewModel
                    {
                        //Code = string.IsNullOrWhiteSpace(promo.CouponCode) ? promo.Name : promo.CouponCode,
                        Code = promo.CouponCode,
                        Description = string.IsNullOrWhiteSpace(promo.Description) ? promo.Name : promo.Description,
                        SavedAmount = promo.SavedAmount,
                        Name = promo.Name
                    });
                }
            }

            return promotions;
        }

        private static bool AlreadyIncludePromotion(List<PromotionViewModel> promotions, PromotionInformation promo)
        {
            if (string.IsNullOrWhiteSpace(promo.CouponCode))
            {
                return promotions.Any(x => string.IsNullOrWhiteSpace(x.Code) && promo.Name == x.Name);
            }
            return promotions.Any(x => promo.CouponCode.Equals(x.Code));
        }

        public static string GetOrderStatus(this IOrderGroup orderGroup)
        {
            var status = orderGroup.OrderStatus;
            if (status == OrderStatus.OnHold)
            {
                return StringConstants.TranslationFallback.OrderStatusOnHold;
            }
            if (status == OrderStatus.PartiallyShipped)
            {
                return StringConstants.TranslationFallback.OrderStatusPartiallyShipped;
            }
            if (status == OrderStatus.InProgress)
            {
                return StringConstants.TranslationFallback.OrderStatusInProgres;
            }
            if (status == OrderStatus.Completed)
            {
                return StringConstants.TranslationFallback.OrderStatusCompleted;
            }
            if (status == OrderStatus.Cancelled)
            {
                return StringConstants.TranslationFallback.OrderStatusCancelled;
            }
            if (status == OrderStatus.AwaitingExchange)
            {
                return StringConstants.TranslationFallback.OrderStatusAwaitingExchange;
            }
            throw new ArgumentOutOfRangeException(status.ToString(), status, null);
        }

        public static string GetOrderDeliveryName(this IOrderGroup orderGroup)
        {
            var shipment = orderGroup.GetFirstShipment();
            if (shipment?.ShippingAddress == null) return string.Empty;

            var fullName = $"{shipment.ShippingAddress.FirstName?.Trim()} {shipment.ShippingAddress.LastName?.Trim()}";

            return string.IsNullOrWhiteSpace(shipment.ShippingAddress.Organization)
                ? fullName.Trim()
                : shipment.ShippingAddress.Organization;
        }

        public static bool GetShippingStatus(this IOrderGroup cart)
        {
            var inventoryHelper = ServiceLocator.Current.GetInstance<IAmInventoryHelper>();

            var lines = cart.GetAllLineItems().Select(x => new { 
                item = x,
                variant = x.GetEntryContent() as TrmVariant
            });

            var containsGift = lines.Select(x => x.variant).Any(x => x != null && x.IsGifting);
            var isMixedCheckout = cart.GetBooleanProperty(CustomFields.IsForMixedCheckout);
            var isVaulted = cart.Forms
                .SelectMany(f => f.Shipments)
                .Any(x => x.ShippingAddress?.Id == "VaultShippingAddress" && x.LineItems?.Count > 0);

            foreach (var line in lines)
            {
                if (isVaulted)
                {
                    if (isMixedCheckout && line.variant.GetBullionVariantType() != Enums.BullionVariantType.None)
                        return false;

                    if (line.variant.GetBullionVariantType() == Enums.BullionVariantType.Signature)
                        return false;
                }

                var reference = line.item.GetEntryContent()?.ContentLink;

                var stock = inventoryHelper.GetStockSummary(reference);

                var oneOutOfStock = stock.PurchaseAvailableQuantity < line.item.Quantity;

                if (oneOutOfStock && containsGift)
                {
                    return false;
                }
            }

            return true;
        }
    }
}