using System;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Calculator;
using EPiServer.ServiceLocation;
using TRM.Web.Business.Pricing.BullionTax;
using log4net;
using Mediachase.Commerce;
using Mediachase.Commerce.Orders;
using TRM.Web.Extentions;

namespace TRM.Web.Business.Pricing
{
    /// <summary>
    /// Override and use EPiServer DefaultTaxCalculator
    /// <see cref="EPiServer.Commerce.Order.Calculator.DefaultShippingCalculator"/>
    /// </summary>
    public class TrmShippingCalculator : DefaultShippingCalculator, IShippingCalculator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TrmShippingCalculator));

        private readonly IBullionTaxService _bullionTaxService;
        private readonly ILineItemCalculator _lineItemCalculator;

        public TrmShippingCalculator(
            IBullionTaxService bullionTaxService,
            ILineItemCalculator lineItemCalculator,
            IReturnLineItemCalculator returnLineItemCalculator,
            ITaxCalculator taxCalculator,
            ServiceCollectionAccessor<IShippingPlugin> shippingPluginsAccessor,
            ServiceCollectionAccessor<IShippingGateway> shippingGatewaysAccessor) :
            base(lineItemCalculator, returnLineItemCalculator, taxCalculator, shippingPluginsAccessor, shippingGatewaysAccessor)
        {
            _bullionTaxService = bullionTaxService;
            _lineItemCalculator = lineItemCalculator;
        }

        // EPiServer.Commerce.Order.Calculator.DefaultShippingCalculator
        /// <summary>
        /// Calculates the shipping tax of an <see cref="T:EPiServer.Commerce.Order.IShipment" />.
        /// </summary>
        /// <param name="shipment">The shipment.</param>
        /// <param name="market">The market to be used in the calculation.</param>
        /// <param name="currency">The currency to be used in the calculation.</param>
        /// <returns>The shipping tax.</returns>
        protected override Money CalculateShippingTax(IShipment shipment, IMarket market, Currency currency)
        {
            var vatCode = shipment.GetShippingVatCode();
            if(string.IsNullOrEmpty(vatCode)) return base.CalculateShippingTax(shipment, market, currency);
            
            var shippingItemsTotal = GetShippingItemsTotal(shipment, currency);

            return CalculateShippingTax(shipment, market, currency, shippingItemsTotal, vatCode);
        }

        // EPiServer.Commerce.Order.Calculator.DefaultShippingCalculator
        private Money CalculateShippingTax(IShipment shipment, IMarket market, Currency currency, Money shipmentSubtotal, string vatCode)
        {
            if (shipment.ShippingAddress == null)
            {
                return new Money(decimal.Zero, currency);
            }
            var vatAmountPercentage = _bullionTaxService.GetVatRateAmount(shipment.ShippingAddress.CountryCode, vatCode);

            var num = 0m;
            var amount = TryGetDiscountedShippingAmount(shipment, market, currency);
            var d = shipment.LineItems.Sum(item => item.Quantity);
            foreach (var current in shipment.LineItems)
            {
                var d2 = current.Quantity;
                if (d2 <= decimal.Zero) continue;

                var amount2 = _lineItemCalculator.GetDiscountedPrice(current, currency).Amount;
                var amount3 = amount * ((shipmentSubtotal.Amount == decimal.Zero) ? (d2 / d) : (amount2 / shipmentSubtotal.Amount));
                var basePrice = new Money(amount3, currency);
                num += GetShippingTax(market, basePrice, vatAmountPercentage).Amount;
            }
            return new Money(num, currency).Round();
        }

        private decimal TryGetDiscountedShippingAmount(IShipment shipment, IMarket market, Currency currency)
        {
            try
            {
                return GetDiscountedShippingAmount(shipment, market, currency).Amount;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to get discounted shipping amount", e);
                return decimal.Zero;
            }
        }

        private Money GetShippingTax(IMarket market, Money basePrice, decimal vatAmountPercentage)
        {
            var premiumTax = vatAmountPercentage / (market.PricesIncludeTax ? (vatAmountPercentage + 100m) : 100m);
            return new Money(basePrice.Amount * premiumTax, basePrice.Currency);
        }
    }
}