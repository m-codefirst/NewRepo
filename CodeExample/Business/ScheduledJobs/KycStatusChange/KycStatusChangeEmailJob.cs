using System;
using System.Linq;
using EPiServer;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Mediachase.Commerce.Customers;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Web.Business.Email;
using TRM.Web.Helpers;
using TRM.Web.Models.Pages;
using TRM.Web.Services;
using static TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Business.ScheduledJobs.KycStatusChangeEmail
{
    [ScheduledPlugIn(DisplayName = "[Bullion] Kyc Status Change Email Job")]
    public class KycStatusChangeEmailJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        protected Lazy<ICustomerContactService> ContactsService = new Lazy<ICustomerContactService>(() =>
        {
            return ServiceLocator.Current.GetInstance<ICustomerContactService>();
        });

        protected Lazy<IEmailHelper> EmailHelper = new Lazy<IEmailHelper>(() =>
        {
            return ServiceLocator.Current.GetInstance<IEmailHelper>();
        });

        protected Lazy<IContentLoader> ContentLoader = new Lazy<IContentLoader>(() =>
        {
            return ServiceLocator.Current.GetInstance<IContentLoader>();
        });

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public KycStatusChangeEmailJob()
        {
            IsStoppable = true;
        }

        /// <summary>
        /// Called when a user clicks on Stop for a manually started job, or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }

        /// <summary>
        /// Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log and visible from admin mode</returns>
        public override string Execute()
        {
            try
            {
                //Call OnStatusChanged to periodically notify progress of job for manually started jobs
                OnStatusChanged(String.Format("Starting execution of {0}", this.GetType()));

                //Add implementation
                var contactsByKycStatus =
                    from c in ContactsService.Value.GetAllContactsNotNotifiedOfKycStatusChange()
                    group c by c[CustomFields.KycStatusNotification] into grp
                    select grp;

                if (!contactsByKycStatus.Any())
                    return "Could not find any contacts not already notified of KYC status change";

                var startPage = ContentLoader.Value.Get<StartPage>(SiteDefinition.Current.StartPage);

                if (startPage == null)
                    return "Job Failed: StartPage cannot be null";

                var emailSettingsPage = ContentLoader.Value.Get<EmailSettingsPage>(startPage.EmailSettingsPage);
                
                if (emailSettingsPage == null)
                    return "Job Failed: EmailSettingsPage cannot be null";

                if (emailSettingsPage.KycStatusNotifications == null)
                    return "Job Failed: EmailSettingsPage.KycStatusNotifications property cannot be null";

                if (emailSettingsPage.KycStatusNotifications.Count == 0)
                    return "Job Failed: EmailSettingsPage.KycStatusNotifications property cannot be empty";

                var notificationsMap = emailSettingsPage
                    .KycStatusNotifications
                    .ToDictionary(k => k.KycStatus, v => ContentLoader.Value.Get<TRMEmailPage>(v.StatusEmail));
                var kycStatus = default(AccountKycStatus);

                foreach (var s in contactsByKycStatus)
                {
                    foreach (var c in s)
                    {
                        kycStatus = (AccountKycStatus)c[CustomFields.KycStatus];

                        if (!notificationsMap.ContainsKey(kycStatus))
                            return $"Job Failed: Notification email has not been defined for {kycStatus} of type AccountKycStatus";

                        if (IsContactValid(c))
                        {
                            EmailHelper.Value.SendEmailForKycStatusChange(notificationsMap[kycStatus], c);
                            c[CustomFields.KycStatusNotification] = c[CustomFields.KycDate];
                            c.SaveChanges();
                        }
                    }
                }

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                return "Customer contacts have been notified of their KYC status change.";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        private bool IsContactValid(CustomerContact contact)
        {
            if (contact == null)
                throw new ArgumentNullException("contact", "Contact cannot be null to check validity for notifications of Kyc Status changes");

            var notificationDate = contact.GetDatetimeProperty(CustomFields.KycStatusNotification);

            return notificationDate != null && notificationDate < contact.GetDatetimeProperty(CustomFields.KycDate);
        }
    }
}
