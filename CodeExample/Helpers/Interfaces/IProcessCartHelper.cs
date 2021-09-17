using EPiServer.Commerce.Order;
using Hephaestus.Commerce.Shared.Models;
using Hephaestus.Core.Business.Attributes;
using Mediachase.Commerce.Customers;
using System.Collections.Generic;
using System.Web;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Cart;
using TRM.Web.Models.ViewModels.Bullion.QuickCheckout;
using Constant = PricingAndTradingService.Models.Constants;

namespace TRM.Web.Helpers.Interfaces
{
    public interface IProcessCartHelper
    {
        HttpResponseJsonResult AddToCart(CartItemDto addToCart, bool isBullion = false);
        //AddToCartResponse AddToCart(CartItemDto addToCart, CustomerContact customerContact);
        AddToCartResponse AddToCart(CartItemDto addToCart, bool isBullion, string cartName, CustomerContact customerContact);
        Dictionary<ILineItem, List<ValidationIssue>> GetValidationResults(Dictionary<ILineItem, List<ValidationIssue>> msgs);
        string GetResultMessage(Dictionary<ILineItem, List<ValidationIssue>> validationResults);
        BuyNowResponse BuyNow(ICart cart, string orderNumberPrefix, DeliveryOption deliveryOption, CustomerContact customerContact = null);
        AddressModel UpdateShippingAddress(IOrderGroup cart, bool deliver = false);
        bool UpdateShippingMethod(IOrderGroup cart, AddressModel addressModel);
        AddressModel GetDefaultAddress(IOrderGroup cart);
        bool CheckAndLoadCart(bool isInEditMode, string cartName, out ICart cart);
        BullionBuyResponse ProcessBullionCart(ICart cart, string orderNumberPrefix);
        IPurchaseOrder BullionConvertToPurchaseOrder(ICart cart, string orderNumberPrefix, out Constant.PampFinishQuoteStatus pampStatus, string clientIpAddress = "");
        IPurchaseOrder ConvertToPurchaseOrder(ICart cart, string orderNumberPrefix, out Constant.PampFinishQuoteStatus pampStatus, bool sentEmailConfirmation, CustomerContact customerContact, string clientIpAddress = "");
        BullionBuyLTResponse BuyBullionByAutoInvest(ICart cart, string orderNumberPrefix, CustomerContact customerContact, string clientIpAddress = "");
    }
}
