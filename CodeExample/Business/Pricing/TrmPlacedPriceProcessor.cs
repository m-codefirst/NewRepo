using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Pricing;
using System;
using System.Globalization;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Business.Pricing.BullionTax;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Business.Pricing
{

    public class TrmPlacedPriceProcessor : DefaultPlacedPriceProcessor, IPlacedPriceProcessor
    {
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IPremiumCalculator<IAmPremiumVariant> _premiumCalculator;
        private readonly IBullionPriceHelper _bullionPriceHelper;
        private readonly IBullionTaxService _bullionTaxService;

        public TrmPlacedPriceProcessor(
            IBullionTaxService bullionTaxService,
            IPriceService priceService,
            IContentLoader contentLoader,
            ReferenceConverter referenceConverter,
            MapUserKey mapUserKey,
            IContentLoader contentLoader1,
            ReferenceConverter referenceConverter1,
            IPremiumCalculator<IAmPremiumVariant> premiumCalculator,
            IBullionPriceHelper bullionPriceHelper)
            : base(priceService, contentLoader, referenceConverter, mapUserKey)
        {
            _contentLoader = contentLoader1;
            _referenceConverter = referenceConverter1;
            _premiumCalculator = premiumCalculator;
            _bullionPriceHelper = bullionPriceHelper;
            _bullionTaxService = bullionTaxService;
        }

        public override bool UpdatePlacedPrice(ILineItem lineItem, CustomerContact customerContact, MarketId marketId, Currency currency, Action<ILineItem, ValidationIssue> onValidationError)
        {
            //Save requested amount to line item.
            var entryContent = lineItem.GetEntryContent(_referenceConverter, _contentLoader);
            if (entryContent == null)
            {
                onValidationError(lineItem, ValidationIssue.RemovedDueToUnavailableItem);
                return false;
            }

            var placedPrice = ProcessPremium(lineItem, customerContact, marketId, currency, entryContent);
            if (placedPrice.HasValue)
            {
                if (new Money(lineItem.PlacedPrice, currency) == placedPrice.Value)
                    return true;

                onValidationError(lineItem, ValidationIssue.PlacedPricedChanged);
                lineItem.PlacedPrice = placedPrice.Value.Amount;

                return true;
            }

            //Handle error when can not get price from PAMP
            //Using ValidationIssue.RemovedDueToInvalidPrice since cannot extend Epi enum
            onValidationError(lineItem, ValidationIssue.RemovedDueToInvalidPrice);
            return false;
        }


        /// <summary>
        /// using for Consumer only
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="quantity"></param>
        /// <param name="customerContact"></param>
        /// <param name="marketId"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public override Money? GetPlacedPrice(EntryContentBase entry, decimal quantity, CustomerContact customerContact,
            MarketId marketId, Currency currency)
        {
            var premiumVariant = entry as IAmPremiumVariant;
            if (premiumVariant != null)
            {
                //always get sell price from Pamp for item in cart
                return _bullionPriceHelper.GetPricePerUnitForBullionVariant(premiumVariant, currency, true).Round();
            }

            return base.GetPlacedPrice(entry, quantity, customerContact, marketId, currency);
        }

        private Money? ProcessPremium(ILineItem lineItem, CustomerContact customerContact, MarketId marketId, Currency currency, EntryContentBase entryContent)
        {
            if (entryContent is IAmInvestmentVariant)
            {
                return ProcessPremiumForSignatureVariant(lineItem, customerContact, marketId, currency, entryContent);
            }

            if (entryContent is IAmPremiumVariant)
            {
                return ProcessPremiumForPhysicalVariant(lineItem, customerContact, marketId, currency, entryContent);
            }

            return base.GetPlacedPrice(entryContent, lineItem.Quantity, customerContact, marketId, currency);
        }

        private Money? ProcessPremiumForPhysicalVariant(ILineItem lineItem, CustomerContact customerContact,
            MarketId marketId, Currency currency, EntryContentBase entryContent)
        {
            var metalPrice = GetCustomMetalPrice(lineItem, entryContent, lineItem.Quantity, customerContact, marketId, currency);

            var amount = metalPrice?.Amount ?? decimal.Zero;
            var premium = _premiumCalculator.GetPremiumForPhysicalVariant((IAmPremiumVariant)entryContent, amount, lineItem.Quantity, customerContact, currency, true);
            if (premium == null) return null;

            var physicalVariantTotalPriceIncludePremium = new Money(premium.PriceIncludedPremium, currency).Round();

            UpdateBullionMetalFields(lineItem, (IAmPremiumVariant)entryContent, metalPrice, physicalVariantTotalPriceIncludePremium);
            return physicalVariantTotalPriceIncludePremium - metalPrice;
        }

        private Money? ProcessPremiumForSignatureVariant(ILineItem lineItem, CustomerContact customerContact,
            MarketId marketId, Currency currency, EntryContentBase entryContent)
        {
            var metalPrice = GetCustomMetalPrice(lineItem, entryContent, lineItem.Quantity, customerContact, marketId, currency);
            var amount = metalPrice?.Amount ?? decimal.Zero;

            decimal investmentAmount;
            decimal.TryParse(lineItem.GetPropertyValue<string>(StringConstants.CustomFields.BullionSignatureValueRequested), out investmentAmount);

            var signatureTotalPriceIncludePremium = _bullionPriceHelper.GetSignaturePricePerUnitIncludedPremium((IAmPremiumVariant)entryContent, investmentAmount, amount, customerContact).Round();

            lineItem.Quantity = GetSignatureVariantPurchaseQuantity(lineItem, (IAmPremiumVariant)entryContent, signatureTotalPriceIncludePremium.Amount, investmentAmount, customerContact);

            UpdateBullionMetalFields(lineItem, (IAmPremiumVariant)entryContent, metalPrice, signatureTotalPriceIncludePremium);
            //return signatureTotalPriceIncludePremium;
            return signatureTotalPriceIncludePremium - metalPrice;
        }

        private Money? GetCustomMetalPrice(ILineItem lineItem, EntryContentBase entryContent, decimal quantity, CustomerContact customerContact, MarketId marketId, Currency currency)
        {
            var tubeQuantity = entryContent is CoinVariant ? ((CoinVariant)entryContent).CoinTubeQuantity.GetValueOrDefault() : 0;
            decimal pampPricePerOneOz;
            var parseSuccess = decimal.TryParse(lineItem.Properties[StringConstants.CustomFields.PampMetalPricePerOneOz]?.ToString(), out pampPricePerOneOz);

            var premiumVariant = entryContent as IAmPremiumVariant;
            if (premiumVariant != null && parseSuccess)
            {
                var metalPrice = premiumVariant.TroyOzWeightConfiguration * pampPricePerOneOz;
                Money? baseMetalPrice = new Money(metalPrice, currency).Round();

                return tubeQuantity > 0 ? baseMetalPrice * tubeQuantity : baseMetalPrice;
            }
            return GetPlacedPrice(entryContent, quantity, customerContact, marketId, currency);
        }

        private void UpdateBullionMetalFields(ILineItem lineItem, IAmPremiumVariant premiumVariant, Money? metalPrice, Money? priceIncludePremium)
        {
            if (premiumVariant == null) return;
            lineItem.Properties[StringConstants.CustomFields.BullionWeight] = lineItem.Quantity * premiumVariant.TroyOzWeightConfiguration;
            if (metalPrice.HasValue)
            {
                lineItem.Properties[StringConstants.CustomFields.PampMetalPricePerUnitWithoutPremium] = metalPrice.Value.Amount;
                lineItem.Properties[StringConstants.CustomFields.BullionPAMPMetalCost] = metalPrice.Value.ToString(CultureInfo.CurrentCulture);
                lineItem.Properties[StringConstants.CustomFields.ExVatLineTotalMetalPrice] = (metalPrice * lineItem.Quantity).GetValueOrDefault().Amount.ToString(CultureInfo.InvariantCulture);
            }
            if (metalPrice.HasValue && priceIncludePremium.HasValue)
            {
                lineItem.Properties[StringConstants.CustomFields.BullionBuyPremiumCost] = (priceIncludePremium.Value - metalPrice.Value).Amount;
            }
        }

        private decimal GetSignatureVariantPurchaseQuantity(ILineItem lineItem, IAmPremiumVariant entryContent, decimal pricePerUnit, decimal investmentAmount, CustomerContact customerContact = null)
        {
            var quantityWithPampAdjusted = GetAdjustedQuantityWithPampQuote(lineItem);
            if (quantityWithPampAdjusted.HasValue) return quantityWithPampAdjusted.Value;
            //return 1 as default quantity
            if (pricePerUnit == 0) return 1;

            var troyOzWeightConfiguration = entryContent.TroyOzWeightConfiguration;
            var vatCode = _bullionTaxService.GetVatCode(lineItem);
            var vatRate = _bullionTaxService.GetVatRateAmount(vatCode, customerContact);

            var newInvestmentAmount = investmentAmount / (1 + vatRate / 100);

            var quantity = newInvestmentAmount / pricePerUnit;
            while (quantity * pricePerUnit * (1 + vatRate / 100) > investmentAmount)
            {
                quantity = quantity - (decimal)0.001 / troyOzWeightConfiguration;
            }

            return Math.Floor(quantity * 1000) / 1000;
        }

        private decimal? GetAdjustedQuantityWithPampQuote(ILineItem lineItem)
        {
            decimal adjustedQuantity;
            if (decimal.TryParse(lineItem.Properties[StringConstants.CustomFields.BullionAdjustedInvestmentQuantity]?.ToString(), out adjustedQuantity))
            {
                return adjustedQuantity;
            }
            return null;
        }
    }
}