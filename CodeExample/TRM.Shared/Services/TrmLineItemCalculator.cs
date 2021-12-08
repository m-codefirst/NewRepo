using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Calculator;
using Mediachase.Commerce;
using System;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;

namespace TRM.Shared.Services
{
    public class TrmLineItemCalculator : DefaultLineItemCalculator
    {
        public TrmLineItemCalculator(ITaxCalculator taxCalculator) : base(taxCalculator)
        {
        }

        protected override Money CalculateDiscountedPrice(ILineItem lineItem, Currency currency)
        {
            //For personalisation can be applied for both consumer and bullion
            if (!decimal.TryParse(lineItem.Properties[StringConstants.CustomFields.PersonalisationCharge]?.ToString(), out var personalisationPrice))
            {
                personalisationPrice = decimal.Zero;
            }

            // Keep default episerver calculation
            if (!IsBullionCartLine(lineItem))
            {
                var baseDiscount = base.CalculateDiscountedPrice(lineItem, currency);
                return baseDiscount + new Money(personalisationPrice * lineItem.Quantity, currency);
            }

            //For get custom discount based on Matt's workaround
            if (!decimal.TryParse(lineItem.Properties[StringConstants.CustomFields.LineItemEntryDiscountAmount]?.ToString(), out var entryDiscount))
            {
                entryDiscount = decimal.Zero;
            }
            var placedPriceIncludePremium = GetTotalPriceWithPremium(lineItem);

            var discountedPrice = new Money(Math.Max(Decimal.Zero, placedPriceIncludePremium - entryDiscount + personalisationPrice * lineItem.Quantity), currency);
            return discountedPrice;
        }

        private decimal GetTotalPriceWithPremium(ILineItem lineItem)
        {
            if (decimal.TryParse(lineItem.Properties[StringConstants.CustomFields.BullionAdjustedTotalPriceIncludePremiums]?.ToString(),
                out var adjustedTotalPriceFromPampQuote) && adjustedTotalPriceFromPampQuote > decimal.Zero)
            {
                return adjustedTotalPriceFromPampQuote;
            }

            var customPlacedPrice = lineItem.GetCustomLineItemPlacedPrice();
            return customPlacedPrice * lineItem.Quantity;
        }

        protected bool IsBullionCartLine(ILineItem lineItem)
        {
            // TODO: Find a better way to check this
            return !string.IsNullOrWhiteSpace(lineItem.Properties[StringConstants.CustomFields.BullionDeliver]?.ToString());
        }
    }

}