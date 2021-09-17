using EPiServer.Commerce.Order;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using Enums = TRM.Web.Constants.Enums;

namespace TRM.Web.Extentions
{
    public static class LineItemExtensions
    {
        public static bool IsInVault(this ILineItem lineItem)
        {
            return lineItem.GetPropertyValue<int>(StringConstants.CustomFields.BullionDeliver) == (int)Enums.BullionDeliver.Vault;
        }

    }
}