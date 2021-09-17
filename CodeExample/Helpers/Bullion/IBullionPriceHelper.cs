using EPiServer.Commerce.Order;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Helpers.Bullion
{
    public interface IBullionPriceHelper
    {
        Money GetFromPriceForPhysicalVariant(IAmPremiumVariant currentContent);
        Money GetPricePerUnitForBullionVariant(IAmPremiumVariant currentContent, Currency? currency = null, bool isPampSell = false);
        Money GetPhysicalVariantPriceIncludedPremium(IAmPremiumVariant currentContent, int quantity, bool isPampSell = false);
        Money GetSignaturePricePerUnitIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerUnitWithoutPremium);
        Money GetSignaturePricePerUnitIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerUnitWithoutPremium, CustomerContact customerContact);
        Money GetSignaturePricePerOneOzIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerOzWithoutPremium);
        Money GetSignaturePricePerOneOzIncludedPremium(CustomerContact customerContact, IAmPremiumVariant currentContent, decimal investmentAmount, decimal pricePerOzWithoutPremium);

        Money GetSignaturePriceIncludedPremium(IAmPremiumVariant currentContent, decimal investmentAmount,
            decimal priceWithoutPremium, bool isPampSell = false);
        Money GetSignaturePriceIncludedPremium(CustomerContact customerContact, IAmPremiumVariant currentContent, decimal investmentAmount, decimal priceWithoutPremium, bool isPampSell = false);
        Money ApplyBuyPremiumsForPhysicalItem(decimal priceAmountWithoutPremiums, IAmPremiumVariant variant, decimal quantity, bool isPampSell = false);
        Money ApplyBuyPremiumsForPhysicalItem(CustomerContact customerContact, decimal priceAmountWithoutPremiums, IAmPremiumVariant currentContent, decimal quantity, bool isPampSell = false);
        Money GetPricePerOneOzFromPamp(IAmPremiumVariant currentContent, Currency? currency = null, bool isPampSell = false);
        Money GetFromPriceForBullionVariant(IAmPremiumVariant bullionVariant, Currency currency, int premiumGroupInt);
    }
}