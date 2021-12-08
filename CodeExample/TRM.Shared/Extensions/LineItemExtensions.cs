using EPiServer.Commerce.Order;
using System;
using TRM.Shared.Constants;

namespace TRM.Shared.Extensions
{
    public static class LineItemExtensions
    {
        public static decimal GetCustomLineItemPlacedPrice(this ILineItem lineItem)
        {
            // TODO: Need to review this hard to control
            var pampMetalPricePerUnitWithoutPremium = lineItem.Properties[StringConstants.CustomFields.PampMetalPricePerUnitWithoutPremium];
            if (pampMetalPricePerUnitWithoutPremium == null) return lineItem.PlacedPrice;

            if (decimal.TryParse(pampMetalPricePerUnitWithoutPremium.ToString(), out var originalMetalCost))
            {
                return lineItem.PlacedPrice + originalMetalCost;
            }
            return lineItem.PlacedPrice;
        }
    }
}
