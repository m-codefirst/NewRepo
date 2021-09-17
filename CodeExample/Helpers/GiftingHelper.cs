using EPiServer;
using EPiServer.Core;
using TRM.Shared.Interfaces;

namespace TRM.Web.Helpers
{
    public class GiftingHelper : IGiftingHelper
    {
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly IContentLoader _contentLoader;

        public GiftingHelper(IAmInventoryHelper inventoryHelper, IContentLoader contentLoader)
        {
            _inventoryHelper = inventoryHelper;
            _contentLoader = contentLoader;
        }


        public bool CanShowContent (ContentReference contentReference)
        {
            var content = _contentLoader.Get<IContent>(contentReference);
            var iControlIsGifting = content as IControlIsGifting;
            if (iControlIsGifting == null) return true;
            if (!iControlIsGifting.IsGifting)
            {
                return true;
            }
            var stock = _inventoryHelper.GetStockSummary(contentReference);
            return stock.PurchaseAvailableQuantity > 0 || stock.CanBackorder || stock.CanPreorder;
        }
    }
}