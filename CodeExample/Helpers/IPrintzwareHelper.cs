using EPiServer.Commerce.Order;

namespace TRM.Web.Helpers
{
    public interface IPrintzwareHelper
    {
        string OpenUrl(string orderId, string code, bool edit);

        string GetThumb(string orderId);

        bool ConfirmOrder(IPurchaseOrder order);

        string GetUniqueId(string id);

        bool UpdateLineItemPersonalisation(ICart cart, string pwOrderId, string pwEpiserverImageId);
    }
}