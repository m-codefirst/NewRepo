using System;
using System.Linq;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Business.DataAccess;
using TRM.Web.Constants;
using TRM.Web.Helpers;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.GDPR
{
    [ScheduledPlugIn(
        DisplayName = "Special Event Send Email",
        Description = "Special Event Send Email")]
    public class SpecialEventSendEmailJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        private readonly IAmSpecialEventsHelper _specialEventsHelper = ServiceLocator.Current.GetInstance<IAmSpecialEventsHelper>();
        private readonly ISpecialEventsRepository _specialEventsRepository = ServiceLocator.Current.GetInstance<ISpecialEventsRepository>();

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public SpecialEventSendEmailJob()
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
                OnStatusChanged("Starting execution of a scheduler to send email for Special Event table");

                var msg = SendEmail();
                //For long running jobs periodically check if stop is signaled and if so stop execution
                return _stopSignaled ? "Stop of job was called" : msg;
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        private string SendEmail()
        {
            var settingPage = _specialEventsHelper.GetSettingPage();
            if (null == settingPage)
                throw new ContentNotFoundException("Please set the Occasion Setting Page on the start page in order to execute Special Event Email Job.");

            var sendBefore = settingPage.SendNotificationBeforeEvent;

            var checkDate = System.DateTime.UtcNow.Date.AddDays(sendBefore);

            var appointments = _specialEventsHelper.Find(x => x.Date == checkDate, true);

            OnStatusChanged($"Found {appointments.Count()} appointment able to send.");
            int sent = 0;
            var skipValue = settingPage.SkipSendEmailInDays;
            skipValue = skipValue > 0 ? skipValue : DefaultConstants.InvalidDateRange;
            foreach (var appointment in appointments)
            {
                var app = _specialEventsRepository.GetAppointment(System.Guid.Parse(appointment.Id));
                if (app.SentAt.HasValue && app.SentAt.Value.Date.AddDays(skipValue) > System.DateTime.UtcNow.Date)
                    continue;
                sent += 1;
                OnStatusChanged($"Sending appointment: {appointment.Name} to {appointment.ContactName}.");
                if (_specialEventsHelper.SendEmail(appointment))
                {
                    //Update status for appointment.
                    app.SentAt = System.DateTime.UtcNow.Date;
                    if (app.RepeatsAnnually)
                        app.Date = app.Date.AddYears(1);
                    _specialEventsRepository.UpdateAppointment(app);
                }
            }
            return $"Finished Special Event Email Job. Found {appointments.Count()} and sent {sent} appointment(s).";
        }
    }
}