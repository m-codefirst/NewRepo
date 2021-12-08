using Mediachase.Commerce.Customers;

namespace TRM.Shared.Helpers
{
    public interface IAmContactAuditHelper
    {
        void WriteCustomerAudit(CustomerContact customer, string title,string message);
    }
}