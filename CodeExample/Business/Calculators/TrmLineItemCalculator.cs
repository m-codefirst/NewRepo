using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Calculator;
using EPiServer.Commerce.Order.Internal;
using Mediachase.Commerce;
using TRM.Shared.Constants;

namespace TRM.Web.Business.Calculators
{
    public class TrmLineItemCalculator :DefaultLineItemCalculator
    {
        public TrmLineItemCalculator(ITaxCalculator taxCalculator) : base(taxCalculator)
        {
        }

        protected override Money CalculateDiscountedPrice(ILineItem lineItem, Currency currency)
        {

            var personalisationPrice = 0m;

            decimal.TryParse(lineItem.Properties[StringConstants.CustomFields.PersonalisationCharge]?.ToString() ?? string.Empty, out personalisationPrice);

            var val2 = lineItem.PlacedPrice * lineItem.Quantity - lineItem.GetEntryDiscountValue() + (personalisationPrice * lineItem.Quantity);
            return new Money(Math.Max(decimal.Zero, val2), currency);
        }

    }
}