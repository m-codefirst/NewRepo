using System;
using System.Linq;
using System.Web.Configuration;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using TRM.Shared.Constants;
using TRM.Web.Helpers;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.DeleteAnonymousContact
{
    [ScheduledPlugIn(
        DisplayName = "Delete anonymous contact job", 
        Description = "Delete anonymous contact job")]
    public class DeleteAnonymousContactJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        private static readonly ILogger Logger = LogManager.GetLogger(typeof(DeleteAnonymousContactJob));

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public DeleteAnonymousContactJob()
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
                OnStatusChanged("Starting execution of a scheduler to Delete anonymous contact");
                var expiredDays = WebConfigurationManager.AppSettings.GetValue("ExpiredAnonymousContactDays", 7);

                var expiredModifiedFilter = new FilterElement(StringConstants.CustomFields.Created, FilterElementType.Less, DateTime.UtcNow.AddDays(-expiredDays));
                var fullNameFilter = new FilterElement(StringConstants.CustomFields.FullName, FilterElementType.Equal, StringConstants.Anonymous);
                var registrationSourceFilter = new FilterElement(StringConstants.CustomFields.RegistrationSource, FilterElementType.Equal, StringConstants.RegistrationSource.CheckoutPageSource);
                var guessFilter = new FilterElement(StringConstants.CustomFields.IsGuest, FilterElementType.Equal, true);

                var filters = new FilterElement[] { expiredModifiedFilter, fullNameFilter, registrationSourceFilter, guessFilter };
                var contacts = BusinessManager.List(StringConstants.CustomFields.ContactClassName, filters, new SortingElement[0], 0, 50).ToList();

                foreach (var entityObject in contacts)
                {
                    BusinessManager.Delete(StringConstants.CustomFields.ContactClassName, (PrimaryKeyId)entityObject.PrimaryKeyId);
                }

                var message = contacts.Count > 0
                    ? $"Deleted {contacts.Count} anonymous contacts successfully"
                    : "There is not any anonymous contact found";

                //For long running jobs periodically check if stop is signaled and if so stop execution
                return _stopSignaled ? "Stop of job was called" : message;
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
