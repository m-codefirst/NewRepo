using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using Hephaestus.Commerce.Cart.Services;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using System.Collections.Generic;
using TRM.Web.Models.DTOs.Cart;

namespace TRM.Web.Business.Cart
{
    public interface ITrmCartService : ICartService, IBullionCartService
    {
        ICart LoadOrCreateCart(string name, MarketId marketId);

        bool AddItemWithQuantity(ICart cart, CartItemDto itemToAdd, out Dictionary<ILineItem, List<ValidationIssue>> warningMessages, bool shouldAudit = true);
        bool AddItemWithQuantity(CustomerContact customerContact, ICart cart, CartItemDto itemToAdd, out Dictionary<ILineItem, List<ValidationIssue>> warningMessages, bool shouldAudit = true);

        bool SetSubscription(ICart cart, UpdateSubscribedItemDto subscribedItem);

        bool TryApplyCoupon(ICart cart, string couponCode);

        Dictionary<ILineItem, List<ValidationIssue>> ValidateCart(ICart cart, bool shouldAudit);

        string GetPwOrderId(ILineItem lineItem);

        ICart LoadCart(string name, bool isMiniBasket = false);

        ICart LoadOrCreateCart(string name, bool useValidationWithCustomPromotionEngineSetting = false, CustomerContact customerContact = null);

        IEnumerable<RewardDescription> ApplyPromotionsToCart(IOrderGroup orderGroup, Dictionary<ILineItem, List<ValidationIssue>> validationIssues = null);
    }
}