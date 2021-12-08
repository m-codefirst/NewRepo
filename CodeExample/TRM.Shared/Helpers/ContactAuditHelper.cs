using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Customers;

namespace TRM.Shared.Helpers
{
    public class ContactAuditHelper : IAmContactAuditHelper
    {
        public void WriteCustomerAudit(CustomerContact customer, string title,string message)
        {
            if (!customer.PrimaryKeyId.HasValue) return;

            var note = new EntityObject("ContactNote");

            note["ContactId"] = customer.PrimaryKeyId.Value;
            note["NoteTitle"] = title;
            note["NoteContent"] = message;
        
            
            BusinessManager.Create(note);
        }
        
    }
}
