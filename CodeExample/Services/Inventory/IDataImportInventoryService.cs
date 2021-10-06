using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs.Bullion;

namespace TRM.Web.Services.Inventory
{
    public interface IDataImportInventoryService
    {
        bool SaveInventoryFromAx(AxImportData.ItemInventory inventory, IAmPremiumVariant entryContent);
    }
}