using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using TRM.IntegrationServices.Models.Export.Emails;
using TRM.IntegrationServices.Models.Export.Orders;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Models.ViewModels.Bullion.CustomerBankAccount;
using TRM.Web.Models.ViewModels.Bullion.MixedCheckout;

namespace TRM.Web.Business.Email
{
    public interface IBullionEmailHelper
    {
        void SendBullionRequestWithdrawalEmail(CustomerContact currentContact, BankAccountViewModel bankAccountToUse);
        void SendBullionAddFundsEmail(CustomerContact currentContact, ManualPaymentDto manualPaymentDto);
        void SendBullionSellRequestEmail(CustomerContact currentContact, SellBullionDefaultLandingViewModel sellBullion);
        void SendBullionDeliverFromVaultEmail(CustomerContact currentContact, DeliverBullionLandingViewModel deliverBullionModel);
        void SendBullionStatementAvailableEmail(Guid customerId, DateTime statementDate);
        void SendBullionDispatchFromVaultEmail(Guid customerId, DispatchFromVaultOrderDto dispatchFromVaultOrderDto);
        void SendBullionOrderConfirmationEmail(InvestmentPurchaseOrder po);
        void SendBullionInvoiceAvailableEmail(Guid customerId, DateTime invoiceDate);
        void SendBankTransferFundsNowAvailableEmail(Guid customerId, BankDepositeModel bankDeposite);
        void SendCancelPurchaseOrderEmail(CancelPurchaseOrderModel cancelPurchaseOrderModel);

        List<MailAddress> GetBullionMailAddressSentTo(CustomerContact customer);
        Dictionary<string, object> GetOrderConfirmationEmailParams(InvestmentPurchaseOrder po, CustomerContact currentContact);
    }
}