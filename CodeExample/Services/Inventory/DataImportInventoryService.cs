using System;
using System.Collections.Generic;
using Mediachase.Commerce.InventoryService;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs.Bullion;

namespace TRM.Web.Services.Inventory
{
    public class DataImportInventoryService : IDataImportInventoryService
    {
        private const string InventoryDefault = "default";
        private readonly IInventoryService _epiInventoryService;

        public DataImportInventoryService(IInventoryService epiInventoryService)
        {
            _epiInventoryService = epiInventoryService;
        }

        public bool SaveInventoryFromAx(AxImportData.ItemInventory inventory, IAmPremiumVariant premiumVariant)
        {
            var inventoryRecordList = new List<InventoryRecord>();

            var defaultWarehouse = string.IsNullOrEmpty(inventory.DefaultWarehouse)
                ? InventoryDefault
                : inventory.DefaultWarehouse;

            var existingInventoryRecord = _epiInventoryService.Get(inventory.ItemId, defaultWarehouse);

            //for tube coin
            var tubeCoinVariant = premiumVariant as CoinVariant;
            var tubeQuantity = tubeCoinVariant?.CoinTubeQuantity;
            if (tubeQuantity != null && tubeQuantity > 0)
            {
                var updatedAvailablePhysical = Convert.ToInt32(Math.Floor((decimal)(inventory.AvailablePhysical / tubeQuantity)));
                inventory.AvailablePhysical = updatedAvailablePhysical;
                var updatedAvailableToOrder = Convert.ToInt32(Math.Floor((decimal)(inventory.AvailableToOrder / tubeQuantity)));
                inventory.AvailableToOrder = updatedAvailableToOrder;
            }

            InventoryRecord writableClone;
            if (existingInventoryRecord == null)
            {
                var newInventoryRecord = new InventoryRecord
                {
                    CatalogEntryCode = inventory.ItemId,
                    WarehouseCode = defaultWarehouse,
                    IsTracked = true
                };
                writableClone = newInventoryRecord.CreateWritableClone();
            }
            else
            {
                writableClone = existingInventoryRecord.CreateWritableClone();
            }
            
            writableClone.PurchaseAvailableQuantity = inventory.AvailablePhysical > 0 ? inventory.AvailablePhysical : decimal.Zero;
            writableClone.BackorderAvailableQuantity = decimal.Zero;
            writableClone.PreorderAvailableQuantity = decimal.Zero;

            inventoryRecordList.Add(writableClone);
            _epiInventoryService.Save(inventoryRecordList);

            return true;
        }
    }
}