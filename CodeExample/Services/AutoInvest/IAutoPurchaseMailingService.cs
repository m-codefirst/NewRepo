using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using TRM.Web.Constants;
using TRM.Web.Models.ViewModels.Bullion.MixedCheckout;

namespace TRM.Web.Services.AutoInvest
{
    public interface IAutoPurchaseMailingService
    {
        void SendMailing(List<AutoPurchaseProcessedUserDto> processedContacts);
        void SendAutoInvestUpdateMessage(AutoPurchaseProcessedUserDto processedContact, Enums.AutoInvestUpdateMessageType type);
        void AutoPurchaseBullionOrderConfirmationEmail(InvestmentPurchaseOrder po, CustomerContact currentContact);
    }
}

