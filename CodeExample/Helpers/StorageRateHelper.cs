using System.Linq;
using EPiServer;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Models.Blocks.Bullion;
using TRM.Web.Models.Catalog.Bullion;

namespace TRM.Web.Helpers
{
    public class StorageRateHelper : IStorageRateHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IBullionPremiumGroupHelper _bullionPremiumGroupHelper;

        public StorageRateHelper(IContentLoader contentLoader, IBullionPremiumGroupHelper bullionPremiumGroupHelper)
        {
            _contentLoader = contentLoader;
            _bullionPremiumGroupHelper = bullionPremiumGroupHelper;
        }
        public BullionStorageRatesBlock GetMatchingStorageRateBlock(CustomerContact customer, PreciousMetalsVariantBase bullionVariant)
        {
            if (customer == null || 
                string.IsNullOrEmpty(customer.PreferredCurrency) || 
                bullionVariant?.StorageRates == null || 
                !bullionVariant.StorageRates.FilteredItems.Any()) return null;

            var customerPremiumGroupInt = customer.GetIntegerProperty(StringConstants.CustomFields.BullionPremiumGroupInt);

            var storageBlocks = bullionVariant.StorageRates.FilteredItems
                .Select(a => _contentLoader.Get<BullionStorageRatesBlock>(a.ContentLink))
                .Where(a => a != null).ToList();

            var matchingStorageBlock = storageBlocks.FirstOrDefault(x =>
                                           !string.IsNullOrEmpty(x.Currencies) && x.Currencies.Split(',').Contains(customer.PreferredCurrency) &&
                                           !string.IsNullOrEmpty(x.CustomerPremiumGroups) && x.CustomerPremiumGroups.Split(',').Contains(customerPremiumGroupInt.ToString())) ?? 
                                       storageBlocks.LastOrDefault(x => !string.IsNullOrEmpty(x.Currencies) && x.CustomerPremiumGroups.Split(',').Contains(customer.PreferredCurrency));

            return matchingStorageBlock;
        }

        public void UpdateStorageFieldsFor(ILineItem lineItem, BullionStorageRatesBlock storageBlock)
        {
            if (lineItem == null) return;
            lineItem.Properties[StringConstants.CustomFields.BullionStoragePremiumInitialRate] = storageBlock?.InitialStorageRate ?? 0;
            lineItem.Properties[StringConstants.CustomFields.BullionStoragePremiumInitialPeriod] = storageBlock?.InitialStorageRateDays ?? 0;
            lineItem.Properties[StringConstants.CustomFields.BullionStoragePremiumSubsequentRate] = storageBlock?.OngoingStorageRate ?? 0;
        }
    }
}