using TRM.Web.Constants;
using TRM.Web.Models.EntityFramework.CustomerContactContext;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoPurchaseProcessedUserDto
    {
        public AutoPurchaseProcessedUserDto()
        {
            
        }

        public AutoPurchaseProcessedUserDto(cls_Contact contact)
        {
            this.ContactEmail = contact.Email;
            this.FirstName = contact.FirstName;
            this.LastName = contact.LastName;
            this.Title = contact.Title;
        }

        public AutoPurchaseProcessedUserDto(cls_Contact contact, Enums.AutoInvestUpdateOrderStatus status, string message = "") : this(contact)
        {
            this.Message = message;
            this.Status = status;
        }

        public Enums.AutoInvestUpdateOrderStatus Status { get; set; }

        public string Message { get; set; }
        public string ContactEmail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
    }
}