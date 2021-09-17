using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Helpers
{
    public interface IStorageRateHelper
    {
        BullionStorageRatesBlock GetMatchingStorageRateBlock(CustomerContact customer,
            PreciousMetalsVariantBase bullionVariant);

        void UpdateStorageFieldsFor(ILineItem lineItem, BullionStorageRatesBlock storageBlock);
    }
}
