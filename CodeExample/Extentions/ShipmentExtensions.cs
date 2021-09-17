using EPiServer.Commerce.Order;
using System.Linq;
using EPiServer.ServiceLocation;
using TRM.Web.Business.Pricing.BullionTax;
using TRM.Web.Constants;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DDS.BullionTax;

namespace TRM.Web.Extentions
{
    public static class ShipmentExtensions
    {
        public static string GetShippingVatCode(this IShipment shipment)
        {
            if (shipment == null) return string.Empty;

            var premiumVariants = shipment.LineItems.Select(x => x.Code.GetVariantByCode() as IAmPremiumVariant).Where(x => x != null).ToList();
            if (!premiumVariants.Any()) return string.Empty;

            string vatCode;
            var isHasMixMetal = premiumVariants.GroupBy(x => x.MetalType).Count() > 1;
            if (isHasMixMetal)
            {
                vatCode = VatCode.Standard;
            }
            else
            {
                var metalType = premiumVariants.GroupBy(x => x.MetalType).Select(x => x.Key).FirstOrDefault();
                vatCode = ServiceLocator.Current.GetInstance<IBullionTaxService>().GetVatRule(Enums.BullionActionType.DeliveryFee, metalType)?.VatCode ?? VatCode.Zero;
            }

            return vatCode;
        }

    }
}