using System;
using System.Collections.Generic;
using EPiServer;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Calculator;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using TRM.Web.Business.Pricing.BullionTax;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Shared.Extensions;
using TRM.Web.Extentions;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Business.Pricing
{
    public class TrmDefaultTaxCalculator : DefaultTaxCalculator 
    {
        private readonly IContentRepository _contentRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IBullionTaxService _bullionTaxService;

        public TrmDefaultTaxCalculator(IContentRepository contentRepository, ReferenceConverter referenceConverter, IBullionTaxService bullionTaxService) : base(contentRepository, referenceConverter)
        {
            this._contentRepository = contentRepository;
            this._referenceConverter = referenceConverter;
            this._bullionTaxService = bullionTaxService;
        }

        protected override Money CalculateSalesTax(
          IEnumerable<ILineItem> lineItems,
          IMarket market,
          IOrderAddress shippingAddress,
          Currency currency)
        {
            Decimal amount = new Decimal();
            foreach (ILineItem lineItem in lineItems)
            {
                var bullionVariant = lineItem.Code.GetVariantByCode() as IAmPremiumVariant;

                var basePrice = new Money(GetTotalPriceWithPremium(lineItem), currency);

                if (bullionVariant == null)
                    return base.CalculateSalesTax(lineItem, market, shippingAddress, new Money(basePrice, currency));

                var vatCode = _bullionTaxService.GetVatCode(lineItem);
                var vatRate = _bullionTaxService.GetVatRateAmount(vatCode);
                var premiumTax = vatRate / (market.PricesIncludeTax ? (vatRate + 100m) : 100m);
                var vatAmount = new Money(basePrice.Amount * premiumTax, basePrice.Currency).Round();

                UpdateBullionLineItemVATMetaFields(lineItem, vatCode, vatRate, vatAmount.ToString());
                amount += vatAmount;
            }

            return new Money(amount, currency);
        }
        protected override Money CalculateSalesTax(ILineItem lineItem, IMarket market, IOrderAddress shippingAddress, Money basePrice)
        {
            var bullionVariant = lineItem.Code.GetVariantByCode() as IAmPremiumVariant;
            if (bullionVariant == null) return base.CalculateSalesTax(lineItem, market, shippingAddress, basePrice);

            var vatCode = _bullionTaxService.GetVatCode(lineItem);
            var vatRate = _bullionTaxService.GetVatRateAmount(vatCode);
            var premiumTax = vatRate / (market.PricesIncludeTax ? (vatRate + 100m) : 100m);
            var vatAmount = new Money(basePrice.Amount * premiumTax, basePrice.Currency).Round();

            UpdateBullionLineItemVATMetaFields(lineItem, vatCode, vatRate, vatAmount.ToString());

            return vatAmount;
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

        private void UpdateBullionLineItemVATMetaFields(ILineItem lineItem, string vatCode, decimal vatRate, string vatAmount)
        {
            lineItem.Properties[StringConstants.CustomFields.BullionVATStatus] = vatCode;
            lineItem.Properties[StringConstants.CustomFields.BullionVATRate] = vatRate;
            lineItem.Properties[StringConstants.CustomFields.BullionVATCost] = vatAmount;
        }

    }


}