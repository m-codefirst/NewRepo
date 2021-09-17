using System.Collections.Generic;
using EPiServer.Commerce.Order;
using Hephaestus.Commerce.Cart.Models;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;

namespace Hephaestus.Commerce.Cart.Services
{
    public interface ICartService
    {
        bool AddToCart(ICart cart, string code, out string warningMessage);
        void ChangeCartItem(ICart cart, int shipmentId, string code, decimal quantity, string size, string newSize);
        void ChangeCartItem(ICart cart, int shipmentId, string code, decimal quantity, string size, string newSize, CustomerContact customerContact);
        void SetCartCurrency(ICart cart, Currency currency);
        void SetCartCurrency(ICart cart, Currency currency, CustomerContact customerContact);
        Dictionary<ILineItem, List<ValidationIssue>> ValidateCart(ICart cart);
        Dictionary<ILineItem, List<ValidationIssue>> ValidateCart(ICart cart, CustomerContact customerContact);
        Dictionary<ILineItem, List<ValidationIssue>> RequestInventory(ICart cart);
        void InventoryProcessorAdjustInventoryOrRemoveLineItem(ICart cart);
        string DefaultCartName { get; }
        string DefaultWishListName { get; }
        ICart LoadCart(string name);
        ICart LoadOrCreateCart(string name, CustomerContact customerContact = null);
        bool AddCouponCode(ICart cart, string couponCode);
        void RemoveCouponCode(ICart cart, string couponCode);
        void RecreateLineItemsBasedOnShipments(ICart cart, IEnumerable<CartItemModel> cartItems, IEnumerable<AddressModel> addresses);
        void MergeShipments(ICart cart);
    }
}
