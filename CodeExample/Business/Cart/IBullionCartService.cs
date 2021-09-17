using EPiServer.Commerce.Order;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.Commerce.Customers;
using System;
using TRM.Web.Models.DTOs.Cart;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Business.Cart
{
    public interface IBullionCartService
    {
        string DefaultBuyNowCartName { get; }
        string DefaultBullionCartName { get; }
        ICart LoadOrCreateCart(CartItemDto cartItemDto);
        ICart LoadOrCreateCart(CartItemDto cartItemDto, string name);
        ICart LoadOrCreateCart(CartItemDto cartItemDto, string name, bool isBullion, CustomerContact customerContact);
        ICart LoadOrCreateBullionCart();
        ICart LoadOrCreateBullionCart(string name, CustomerContact customerContact = null);
        bool CheckMinInvestAmount(CartItemDto cartItemDto);
        bool CheckMinInvestAmount(CustomerContact customerContact);
        bool RemoveItemFromCart(RemoveFromCartDto removeFromCart);
        void RemoveCookieForEmptyMixedCart();
        bool ChangeShipment(ICart cart, string variantCode);
        void ValidateVaultShipment(ICart cart);
        SyncWithPampQuoteResult SyncAndAdjustCartWithPampQuote(ICart cart, bool isRecalculate = false);
        SyncWithPampQuoteResult SyncAndAdjustCartWithPampQuote(CustomerContact customerContact, ICart cart, bool isRecalculate = false);
        SyncWithPampQuoteResult SyncAndAdjustCartWithPampQuote(string cartName);
        AddressModel GetDefaultShippingAddressForVault();
        AddressModel GetDefaultShippingAddressForDelivery();
    }
}
