using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Calculator;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Framework;
using Mediachase.Commerce;
using Mediachase.Commerce.Markets;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Constants;

namespace TRM.Web.Business.Calculators
{
    public class OrderGroupCalculator : DefaultOrderGroupCalculator, IOrderGroupCalculator
    {
        private readonly IOrderFormCalculator _orderFormCalculator;
        private readonly IMarketService _marketService;
        private readonly IReturnOrderFormCalculator _returnOrderFormCalculator;
        public OrderGroupCalculator(IOrderFormCalculator orderFormCalculator,
            IReturnOrderFormCalculator returnOrderFormCalculator, IMarketService marketService) : base(
            orderFormCalculator, returnOrderFormCalculator, marketService)
        {
            _orderFormCalculator = orderFormCalculator;
            _marketService = marketService;
            _returnOrderFormCalculator = returnOrderFormCalculator;
        }

        public new Money GetOrderDiscountTotal(IOrderGroup orderGroup)
        {
            // Default behaviour
            if (orderGroup.Name == DefaultCartNames.Default)
            {
                return base.GetOrderDiscountTotal(orderGroup);
            }

            // BULL-2308 Line Level Discount applied twice
            // Quick fix to close the ticket on time
            // Need more review on this.
            var amount = orderGroup.Forms.SelectMany(x => x.Promotions)
                .Where(x => x.DiscountType == EPiServer.Commerce.Marketing.DiscountType.Order)
                .Sum(x => x.SavedAmount);

            return new Money(amount, orderGroup.Currency);
        }

        public new OrderGroupTotals GetOrderGroupTotals(IOrderGroup orderGroup)
        {
            // We defined NEW GetOrderDiscountTotal so we need this NEW too.
            Validator.ThrowIfNull("orderGroup", orderGroup);

            Currency currency = orderGroup.Currency;
            IMarket market = _marketService.GetMarket(orderGroup.MarketId);
            Dictionary<IOrderForm, OrderFormTotals> dictionary = orderGroup.Forms.ToDictionary((IOrderForm form) => form, (IOrderForm form) => _orderFormCalculator.GetOrderFormTotals(form, market, currency));
            Money taxTotal;
            Money shippingTotal;
            Money subTotal;
            Money handingTotal = taxTotal = (shippingTotal = (subTotal = new Money(0m, currency)));
            foreach (KeyValuePair<IOrderForm, OrderFormTotals> item in dictionary)
            {
                subTotal += item.Value.SubTotal;
                shippingTotal += item.Value.ShippingTotal;
                taxTotal += item.Value.TaxTotal;
                handingTotal += item.Value.HandlingTotal;
            }
            IOrderGroupCalculatedAmount orderGroupCalculatedAmount;
            if ((orderGroupCalculatedAmount = (orderGroup as IOrderGroupCalculatedAmount)) != null && !orderGroupCalculatedAmount.IsTaxTotalUpToDate)
            {
                orderGroupCalculatedAmount.TaxTotal = taxTotal.Amount;
                orderGroupCalculatedAmount.IsTaxTotalUpToDate = true;
            }
            Money total = subTotal + shippingTotal + handingTotal - GetOrderDiscountTotal(orderGroup);
            if (!orderGroup.PricesIncludeTax)
            {
                total += taxTotal;
            }
            IPurchaseOrder purchaseOrder;
            if ((purchaseOrder = (orderGroup as IPurchaseOrder)) != null)
            {
                foreach (IReturnOrderForm returnForm in purchaseOrder.ReturnForms)
                {
                    dictionary.Add(returnForm, _returnOrderFormCalculator.GetReturnOrderFormTotals(returnForm, market, currency));
                }
            }
            return new OrderGroupTotals(subTotal, total, shippingTotal, taxTotal, handingTotal, dictionary);
        }

        protected override Money CalculateTotal(IOrderGroup orderGroup)
        {
            // We defined NEW GetOrderDiscountTotal
            // Truong changed this back to default episerver. Need to re-test BULL-1342
            var subTotal = GetSubTotal(orderGroup);
            var handlingTotal = GetHandlingTotal(orderGroup);
            var shippingSubTotal = GetShippingSubTotal(orderGroup);
            var discountTotal = GetOrderDiscountTotal(orderGroup);
            var shippingDiscountTotal = orderGroup.GetShippingDiscountTotal();

            Money money = subTotal + handlingTotal + shippingSubTotal - discountTotal - shippingDiscountTotal;
            if (!orderGroup.PricesIncludeTax)
            {
                var taxTotal = GetTaxTotal(orderGroup);
                money += taxTotal;
            }

            // Temp fix: Exception when calling GetCartTotalWithoutRecuringItems. 
            if (money.Amount < 0)
            {
                return new Money(0, orderGroup.Currency);
            }

            return money;
        }

    }
}