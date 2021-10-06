using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Find.Helpers.Text;
using EPiServer.Logging.Compatibility;
using Hephaestus.ContentTypes.Business.Extensions;
using Mediachase.Commerce.Customers;
using TRM.Web.Business.Email;
using TRM.Web.Business.ScheduledJobs.AutoInvest;
using TRM.Web.Constants;
using TRM.Web.Helpers.AutoInvest;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.Bullion.MixedCheckout;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoPurchaseMailingService : IAutoPurchaseMailingService
    {
        private readonly IEmailHelper _emailHelper;
        private readonly IAutoPurchaseHelper _autoPurchaseHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IBullionEmailHelper _bullionEmailHelper;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BullionEmailHelper));
        private readonly IJobFailedHandler _jobFailedHandler;


        public AutoPurchaseMailingService(IEmailHelper emailHelper, IAutoPurchaseHelper autoPurchaseHelper, IContentLoader contentLoader, IBullionEmailHelper bullionEmailHelper, IJobFailedHandler jobFailedHandler)
        {
            _emailHelper = emailHelper;
            _autoPurchaseHelper = autoPurchaseHelper;
            _contentLoader = contentLoader;
            _bullionEmailHelper = bullionEmailHelper;
            _jobFailedHandler = jobFailedHandler;
        }
        public void SendMailing(List<AutoPurchaseProcessedUserDto> processedContacts)
        {
            var emailPages = GetEmailPages();

            processedContacts = FilterOutSuccessStatuses(processedContacts);

            foreach (var contact in processedContacts)
            {
                var trmEmailPage = emailPages.ContainsKey(contact.Status) ? emailPages[contact.Status] : null;

                if (trmEmailPage == null)
                {
                    _jobFailedHandler.Handle(nameof(AutoInvestJob), GetErrorDetails(contact));
                }
                else
                {
                    var emailParams = GetEmailParams(contact);
                    _emailHelper.SendEmailToContact(trmEmailPage, contact.ContactEmail, emailParams);
                }
            }
        }

        public void SendAutoInvestUpdateMessage(AutoPurchaseProcessedUserDto processedContact, Enums.AutoInvestUpdateMessageType type)
        {
            var trmEmailPage = GetAutoInvestUpdateEmail(type);
            if (trmEmailPage == null)
            {
                return;
            }

            var emailParams = GetEmailParams(processedContact);
            _emailHelper.SendEmailToContact(trmEmailPage, processedContact.ContactEmail, emailParams);
        }

        private TRMEmailPage GetAutoInvestUpdateEmail(Enums.AutoInvestUpdateMessageType autoInvestCancelled)
        {
            var settingsPage = _autoPurchaseHelper.GetEmailSettingsPage();

            TRMEmailPage content;

            switch (autoInvestCancelled)
            {
                case Enums.AutoInvestUpdateMessageType.Updated:
                    _contentLoader.TryGet(settingsPage.AutoInvestUpdatedEmail, out content);
                    return content;

                case Enums.AutoInvestUpdateMessageType.Cancelled:
                    _contentLoader.TryGet(settingsPage.AutoInvestStoppedEmail, out content);
                    return content;
                
                case Enums.AutoInvestUpdateMessageType.Created:
                default:
                    _contentLoader.TryGet(settingsPage.AutoInvestCreatedEmail, out content);
                    return content;
            }
        }

        // If response from purchase is successful then no need to send confirmation email
        // Because it is already handle by purchase service.
        private static List<AutoPurchaseProcessedUserDto> FilterOutSuccessStatuses(List<AutoPurchaseProcessedUserDto> processedContacts)
        {
            return processedContacts.Where(x => x.Status != Enums.AutoInvestUpdateOrderStatus.Success).ToList();
        }

        private string GetErrorDetails(AutoPurchaseProcessedUserDto contact)
        {
            var lines = new List<string>
            {
                $"Contact: {contact.ContactEmail}",
                $"Status: {contact.Status.DescriptionAttr()}",
                $"Message: {contact.Message}"
            };

            return string.Join("<br />", lines);
        }
        
        public void AutoPurchaseBullionOrderConfirmationEmail(InvestmentPurchaseOrder po, CustomerContact currentContact)
        {
            if (po == null || currentContact == null) return;
            var emailPages = GetEmailPages();
            var trmEmailPage = GetEmailPage(emailPages, Enums.AutoInvestUpdateOrderStatus.Success);

            try
            {
                var sendingEmailErrorMessage = string.Empty;

                var toAddresses = _bullionEmailHelper.GetBullionMailAddressSentTo(currentContact);
                var emailParams = _bullionEmailHelper.GetOrderConfirmationEmailParams(po, currentContact);
                UpdateEmailParamsForSuccess(emailParams, po);

                if (_emailHelper.SendAutoInvestBullionEmail(trmEmailPage, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.ErrorFormat("Error when trying to send the email: {0}", sendingEmailErrorMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void UpdateEmailParamsForSuccess(Dictionary<string, object> emailParams, InvestmentPurchaseOrder po)
        {
            var firstProductDisplayName =
                po?.VaultItems != null && po.VaultItems.Any() ? po.VaultItems.FirstOrDefault()?.DisplayName : string.Empty;
            var settingsPage = _autoPurchaseHelper.GetAutoInvestSettingsPage();
            var displayNameParamValue = (settingsPage == null || settingsPage.DisplayNameInEmail.IsNullOrWhiteSpace())
                ? firstProductDisplayName
                : settingsPage?.DisplayNameInEmail;

            emailParams.Add(StringConstants.EmailParameters.DisplayName, displayNameParamValue);
        }

        public Dictionary<string, object> GetEmailParams(AutoPurchaseProcessedUserDto userDto)
        {
            return new Dictionary<string, object>
            {
                { StringConstants.EmailParameters.Title, userDto.Title },
                { StringConstants.EmailParameters.FirstName, userDto.FirstName },
                { StringConstants.EmailParameters.LastName, userDto.LastName },
            };
        }

        private static TRMEmailPage GetEmailPage(Dictionary<Enums.AutoInvestUpdateOrderStatus, TRMEmailPage> emailPages, Enums.AutoInvestUpdateOrderStatus status)
        {
            if (!emailPages.ContainsKey(status) || emailPages[status] == null)
            {
                return emailPages[Enums.AutoInvestUpdateOrderStatus.Error];
            }

            return emailPages[status];
        }

        private Dictionary<Enums.AutoInvestUpdateOrderStatus, TRMEmailPage> GetEmailPages()
        {
            var settingsPage = _autoPurchaseHelper.GetEmailSettingsPage();

            var result = new Dictionary<Enums.AutoInvestUpdateOrderStatus, TRMEmailPage>
            {
                {Enums.AutoInvestUpdateOrderStatus.Success,  _contentLoader.Get<TRMEmailPage>(settingsPage.AutoInvestSuccessEmailPage)},
                {Enums.AutoInvestUpdateOrderStatus.InsufficientFunds, _contentLoader.Get<TRMEmailPage>(settingsPage.AutoInvestInsufficientFundsEmail)},
                {Enums.AutoInvestUpdateOrderStatus.KycNotApproved, _contentLoader.Get<TRMEmailPage>(settingsPage.AutoInvestKycNotApprovedEmailPage)},
            };

            return result;
        }
    }
}