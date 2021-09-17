using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Find.Helpers.Text;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Events;
using Mediachase.Commerce.InventoryService;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class InventoryEvents : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var broadcaster = ServiceLocator.Current.GetInstance<CatalogKeyEventBroadcaster>();
            broadcaster.InventoryUpdated += Broadcaster_InventoryUpdated;
        }

        public void Uninitialize(InitializationEngine context)
        {
            var broadcaster = ServiceLocator.Current.GetInstance<CatalogKeyEventBroadcaster>();
            broadcaster.InventoryUpdated -= Broadcaster_InventoryUpdated;
        }

        private void Broadcaster_InventoryUpdated(object sender, InventoryUpdateEventArgs e)
        {
            try
            {
                var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
                var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();
                var inventoryHelper = ServiceLocator.Current.GetInstance<IAmInventoryHelper>();

                var records = sender as IEnumerable<InventoryRecord>;
                if (records == null) return;

                foreach (var record in records)
                {
                    if (record.CatalogEntryCode.IsNullOrEmpty()) continue;
                    var reference = referenceConverter.GetContentLink(record.CatalogEntryCode);

                    var content = contentRepo.Get<IContent>(reference);
                    var variant = content as TrmVariant;

                    if (variant == null) continue;

                    var canBeBought = record.PurchaseAvailableQuantity > 0m || inventoryHelper.CanBackOrder(record) || inventoryHelper.CanPreOrder(record);

                    if (variant.IsInStock != canBeBought)
                    {
                        var writableVariant = variant.CreateWritableClone<TrmVariant>();
                        writableVariant.IsInStock = canBeBought;
                        contentRepo.Save(writableVariant, SaveAction.Publish, AccessLevel.NoAccess);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
                // Hung.Dang: To pass the error "Media is not found. Navigate to Assets tab and remove it in order to publish" and finish checkout process
            }
        }

    }
}