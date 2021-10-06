using EPiServer;
using EPiServer.Web;
using System;
using TRM.Web.Business.Email;
using TRM.Web.Models.Pages;

namespace TRM.Web.Services
{
    public interface IJobFailedHandler
    {
        void Handle(string jobName, Exception ex);

        void Handle(string jobName, string ex);
    }
    public class JobFailedEmailHandler : IJobFailedHandler
    {
        private readonly IEmailHelper _emailHelper;
        private readonly IContentLoader _contentLoader;

        public JobFailedEmailHandler(IEmailHelper emailHelper, IContentLoader contentLoader)
        {
            _emailHelper = emailHelper;
            _contentLoader = contentLoader;
        }

        public void Handle(string jobName, Exception ex)
        {
            this.Handle(jobName, ex.ToString());
        }

        public void Handle(string jobName, string ex)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage?.EmailSettingsPage == null) return;
            var emailSettingsPage = _contentLoader.Get<EmailSettingsPage>(startPage.EmailSettingsPage);
            if (emailSettingsPage.JobFailNotificationEmail == null) return;
            var emailErrorPage = _contentLoader.Get<TrmErrorEmailPage>(emailSettingsPage.JobFailNotificationEmail);
            _emailHelper.SendJobFailedEmail(emailErrorPage, jobName, ex);
        }
    }
}