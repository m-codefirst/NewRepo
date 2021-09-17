using System.Collections.Generic;
using EPiServer.Commerce.Order;
using System.Linq;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using Enums = TRM.Web.Constants.Enums;

namespace TRM.Web.Extentions
{
    public static class PurchaseOrderExtensions
    {
        private const int KycShippingAddressIndex = 0;
        private const int VaultedShippingAddressIndex = 1;

        public static IOrderAddress GetVaultedShippingAddress(this IPurchaseOrder po)
        {
            if (po == null || po.GetFirstForm()?.Shipments?.Count < (VaultedShippingAddressIndex + 1)) return null;
            return po.GetFirstForm().Shipments.ElementAt(VaultedShippingAddressIndex).ShippingAddress;
        }

        public static IOrderAddress GetKycShippingAddress(this IPurchaseOrder po)
        {
            return po?.GetFirstShipment()?.ShippingAddress == null ? null : po.GetFirstShipment().ShippingAddress;
        }

        public static IEnumerable<ILineItem> GetAllVaultedItems(this IPurchaseOrder po)
        {
            var allLineItems = po?.GetAllLineItems();
            if (allLineItems == null || !allLineItems.Any()) return null;

            return allLineItems.Where(lineItem => lineItem.GetPropertyValue<int>(StringConstants.CustomFields.BullionDeliver) == (int)Enums.BullionDeliver.Vault);
        }
    }
}