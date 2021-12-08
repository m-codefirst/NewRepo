using EPiServer.Commerce.Order;

namespace TRM.Shared.Helpers
{
    public interface IAmOrderGroupAuditHelper
    {
        void WriteAudit(IOrderGroup orderGroup, string title, string note);
    }
}
