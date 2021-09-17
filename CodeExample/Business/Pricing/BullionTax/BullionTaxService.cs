using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using System;
using System.Linq;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Business.DataAccess;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DDS.BullionTax;
using static TRM.Web.Constants.Enums;

namespace TRM.Web.Business.Pricing.BullionTax
{
    public interface IBullionTaxService
    {
        /// <summary>
        /// 2. For the Vat Status, if "Zero?" = Yes, rate is 0%, otherwise use the Vat Rate from the country table,
        /// using the country of the Bullion KYC address.
        /// 3. Record the Vat code as well as the amount.
        /// </summary>
        decimal GetVatRateAmount(string vatCode);
        decimal GetVatRateAmount(string vatCode, CustomerContact customerContact);

        decimal GetVatRateAmount(string countryCode, string vatCode);

        string GetVatCode(ILineItem lineItem);

        VatRule GetVatRule(
            BullionActionType actionName,
            PricingAndTradingService.Models.Constants.MetalType metalType,
            BullionVariantType bullionVariantType = BullionVariantType.None);

        VatRule GetVatRule(
            BullionActionType actionName);
    }

    [ServiceConfiguration(typeof(IBullionTaxService))]
    [ServiceConfiguration(typeof(BullionTaxService))]
    public class BullionTaxService : IBullionTaxService
    {
        private const string UnitedKingdomCode = "GBR";
        private readonly IBullionTaxRepository _bullionTaxRepository;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionTaxService(IBullionTaxRepository bullionTaxRepository, IAmBullionContactHelper bullionContactHelper)
        {
            _bullionTaxRepository = bullionTaxRepository;
            _bullionContactHelper = bullionContactHelper;
        }

        public virtual decimal GetVatRateAmount(string vatCode)
        {
            return GetVatRateAmount(GetBullionKycAddress(), vatCode);
        }
        public virtual decimal GetVatRateAmount(string vatCode, CustomerContact customerContact)
        {
            return GetVatRateAmount(GetBullionKycAddress(customerContact), vatCode);
        }

        public virtual decimal GetVatRateAmount(string countryCode, string vatCode)
        {
            var amount = decimal.Zero;
            if (vatCode.Equals(VatCode.Zero)) return amount;

            if (vatCode.Equals(VatCode.Standard))
            {
                var vatRate = GetVatRate(countryCode, vatCode);
                return vatRate?.Rate ?? amount;
            }

            if (vatCode.Equals(VatCode.UKVault))
            {
                var vatRate = GetVatRate(UnitedKingdomCode, VatCode.Standard);
                return vatRate?.Rate ?? amount;
            }

            return amount;
        }

        public string GetVatCode(ILineItem lineItem)
        {
            var bullionDeliver = lineItem.GetPropertyValue<int>(StringConstants.CustomFields.BullionDeliver);
            VatRule vatRule;
            if (bullionDeliver == (int)BullionDeliver.Deliver)
            {
                vatRule = GetVatRule(BullionActionType.BuyMetalForDelivery, lineItem);
                return vatRule != null ? vatRule.VatCode : VatCode.Zero;
            }

            if (bullionDeliver != (int) BullionDeliver.Vault) return VatCode.Zero;
            
            vatRule = GetVatRule(BullionActionType.BuyForVault, lineItem);
            return vatRule != null ? vatRule.VatCode : VatCode.Zero;
        }

        public virtual VatRule GetVatRule(
            BullionActionType actionName,
            PricingAndTradingService.Models.Constants.MetalType metalType,
            BullionVariantType bullionVariantType = BullionVariantType.None)
        {
            return _bullionTaxRepository.GetVatRuleList()?.FirstOrDefault(x =>
                x.Action.Equals(actionName) &&
                x.MetalType.Equals(metalType) &&
                (bullionVariantType.Equals(BullionVariantType.None) || x.BullionVariantType.Equals(bullionVariantType)));
        }

        public virtual VatRule GetVatRule(BullionActionType actionName)
        {
            return _bullionTaxRepository.GetVatRuleList()?.FirstOrDefault(x =>
                x.Action.Equals(actionName));
        }

        private VatRule GetVatRule(BullionActionType actionName, ILineItem lineItem)
        {
            var bullionVariant = lineItem.Code.GetVariantByCode() as IAmPremiumVariant;
            if (bullionVariant == null) return null;

            return GetVatRule(actionName, bullionVariant.MetalType,
                (bullionVariant as TrmVariant).GetBullionVariantType());
        }

        private VatRate GetVatRate(string countryCode, string vatCode)
        {
            return _bullionTaxRepository.GetVatRateList()?.FirstOrDefault(x =>
                x.CountryCode.Equals(countryCode)
                && x.VatCode.Equals(vatCode, StringComparison.InvariantCultureIgnoreCase));
        }

        private string GetBullionKycAddress(CustomerContact customerContact = null)
        {
            var currentContact = customerContact ?? CustomerContext.Current.CurrentContact;
            if (currentContact == null) return UnitedKingdomCode;

            var kycAddress = _bullionContactHelper.GetBullionAddress(currentContact);
            if (kycAddress == null) return UnitedKingdomCode;

            return kycAddress.CountryCode;
        }
    }

}