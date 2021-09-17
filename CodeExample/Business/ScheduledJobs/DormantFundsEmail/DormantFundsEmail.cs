using EPiServer;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRM.Web.Business.Email;
using TRM.Web.Facades;
using TRM.Web.Helpers;
using TRM.Web.Models.Pages;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.DormantFundsEmail 
{
    [ScheduledPlugIn(
        DisplayName = "Dormant Funds Email", 
        Description = "Notify users with dormant funds.")]
    public class DormantFundsEmail : ScheduledJobBase
    {
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IEmailHelper _emailHelper;      
        private readonly Injected<CustomerContextFacade> _customerContextFacade;

        private readonly DormantFundsSettingsPage _dormantFundsSettingsPage;
        private readonly EmailSettingsPage _emailSettingsPage;
        private readonly IContentLoader _contentLoader;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public DormantFundsEmail(IAmTransactionHistoryHelper transactionHistoryHelper, IEmailHelper emailHelper)
        {
            _transactionHistoryHelper = transactionHistoryHelper;
            _emailHelper = emailHelper;

            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);

            if(startPage != null && startPage.DormantFundsSettingsPage != null && startPage.EmailSettingsPage != null)
            {
                _dormantFundsSettingsPage = _contentLoader.Get<DormantFundsSettingsPage>(startPage.DormantFundsSettingsPage);
                _emailSettingsPage = _contentLoader.Get<EmailSettingsPage>(startPage.EmailSettingsPage);
            } 
            else
            {
                throw new Exception("Dormant fund and Email settings pages need configuring in StartPage.");
            }
        }

        public override string Execute()
        {
            try
            {
                if (_dormantFundsSettingsPage != null && _emailSettingsPage != null)
                {
                    var earlyDormantCustomers = _transactionHistoryHelper.GetCustomerByDormantFunds(_dormantFundsSettingsPage.EarlyNotificationThreshold)
                        .Select(x => x.CustomerRef)
                        .ToList();

                    var reminderDormantCustomers = _transactionHistoryHelper.GetCustomerByDormantFunds(_dormantFundsSettingsPage.ReminderEmailThreshold)
                        .Select(x => x.CustomerRef)
                        .Where(x => !earlyDormantCustomers.Any(y => y == x))
                        .ToList();

                    var finalDormantCustomers = _transactionHistoryHelper.GetCustomerByDormantFunds(_dormantFundsSettingsPage.FinalEmailThreshold)
                        .Select(x => x.CustomerRef)
                        .Where(x => !earlyDormantCustomers.Any(y => y == x))
                        .Where(x => !reminderDormantCustomers.Any(y => y == x))
                        .ToList();

                    if (_emailSettingsPage.DormantFundEarlyNotificationEmail == null ||
                        _emailSettingsPage.DormantFundReminderEmail == null ||
                        _emailSettingsPage.DormantFundFinalEmail == null)
                    {
                        throw new Exception("Dormant fund email settings need configuring.");
                    }

                    //get email pages
                    TRMEmailPage earlyNotificationEmail = _contentLoader.Get<TRMEmailPage>(_emailSettingsPage.DormantFundEarlyNotificationEmail);
                    TRMEmailPage reminderNotificationEmail = _contentLoader.Get<TRMEmailPage>(_emailSettingsPage.DormantFundReminderEmail);
                    TRMEmailPage finalNotificationEmail = _contentLoader.Get<TRMEmailPage>(_emailSettingsPage.DormantFundFinalEmail);

                    if (earlyNotificationEmail == null || reminderNotificationEmail == null || finalNotificationEmail == null)
                    {
                        throw new Exception("Could not load all notification email pages.");
                    }

                    SendCustomerEmails(earlyNotificationEmail, earlyDormantCustomers);
                    SendCustomerEmails(reminderNotificationEmail, reminderDormantCustomers);
                    SendCustomerEmails(finalNotificationEmail, finalDormantCustomers);

                    return "Reminder emails sent.";
                }
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
            
            throw new Exception("Dormant funds and/or email settings page not found.");
        }

        private void SendCustomerEmails(TRMEmailPage emailPage, IEnumerable<string> customerRefs)
        {
            var customers = customerRefs
                .Select(x => _customerContextFacade.Service.GetContactById(new Guid(x)))
                .ToList();

            foreach(var c in customers)
            {
                _emailHelper.SendDormantFundsEmail(emailPage, c);
            }
        }
    }
}