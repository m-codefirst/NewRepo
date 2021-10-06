using System;
using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Web.Business.User;
using TRM.Web.Constants;
using TRM.Web.Helpers.Interfaces;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Cart;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoInvestPurchaseService : IAutoInvestPurchaseService
    {
        private const int DefaultQuantity = 1;

        private readonly IUserService _userService;
        private readonly IProcessCartHelper _processCartHelper;
        private const string DefaultIpAddress = "AutoInvestPurchaseService.ScheduledJob";

        public AutoInvestPurchaseService(IUserService userService, IProcessCartHelper processCartHelper, IAutoInvestUserService autoInvestUserService)
        {
            _userService = userService;
            _processCartHelper = processCartHelper;
        }
        public AutoInvestUpdateOrderResponse UpdateOrder(string bullionObsAccountNumber, Dictionary<string, decimal> products)
        {
            var response = new AutoInvestUpdateOrderResponse();
            try
            {
                var customerContact = _userService.GetCustomerContactByBullionObsAccountNumber(bullionObsAccountNumber);
                if (customerContact == null)
                {
                    response.Status = AutoInvestUpdateOrderStatus.KycNotApproved;
                    response.Message = $"Customer not found with given Obs Account Number: {bullionObsAccountNumber}";
                    return response;
                }

                using (new CustomerContactScope(customerContact))
                {
                    UpdateOrderInternal(customerContact, products, response);
                }
            }
            catch (Exception ex)
            {
                response.Status = AutoInvestUpdateOrderStatus.Error;
                response.Message = $"{ex}";
            }

            return response;
        }

        private void UpdateOrderInternal(CustomerContact customerContact, Dictionary<string, decimal> products,
            AutoInvestUpdateOrderResponse response)
        {
            AddToCartResponse addToCart = null;

            foreach (var product in products)
            {
                var cartItem = new CartItemDto
                {
                    Code = product.Key,
                    Quantity = DefaultQuantity,
                    InvestmentAmount = product.Value
                };

                addToCart = _processCartHelper.AddToCart(cartItem, DefaultConstants.IsBullionTrue,
                    DefaultCartNames.DefaultAutoInvestCartName, customerContact);

                if (addToCart?.Cart == null || (!addToCart.Success && addToCart.Status != AutoInvestUpdateOrderStatus.Success))
                {
                    response.Message = addToCart?.Message ?? "addToCart is null";
                    response.Status = addToCart?.Status ?? AutoInvestUpdateOrderStatus.Error;
                    addToCart = null;
                    break;
                }
            }

            if (addToCart == null)
            {
                return;
            }

            var result = _processCartHelper.BuyBullionByAutoInvest(addToCart.Cart, DefaultConstants.DefaultOrderNumberPrefix, customerContact, DefaultIpAddress);

            if (result != null)
            {
                response.Message = result.Message;
                response.Status = result.Status;
            }
            else
            {
                response.Message = $"Bullion Buy object is null";
                response.Status = AutoInvestUpdateOrderStatus.Error;
            }
        }
    }
}