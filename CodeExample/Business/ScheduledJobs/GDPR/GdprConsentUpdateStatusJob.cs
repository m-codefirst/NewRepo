using System;
using System.Linq;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Constants;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.GDPR
{
    [ScheduledPlugIn(DisplayName = "GDPR update consent status", Description = "GDPR update consent status")]
    public class GdprConsentUpdateStatusJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        private static readonly ILogger Logger = LogManager.GetLogger(typeof(GdprConsentUpdateStatusJob));
        private readonly IGdprService _gdprService = ServiceLocator.Current.GetInstance<IGdprService>();

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public GdprConsentUpdateStatusJob()
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
                OnStatusChanged("Starting execution of a scheduler to update status for GdprConsent table");

                var msg = UpdateStatusGdprConsentTable();
                //For long running jobs periodically check if stop is signaled and if so stop execution
                return _stopSignaled ? "Stop of job was called" : msg;
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        private string UpdateStatusGdprConsentTable()
        {
            try
            {
                var gdprConsentRows = _gdprService.GetAllGdprConsentsNeedToUpdateStatus().ToList();
                foreach (var gdprConsent in gdprConsentRows)
                {
                    if ((gdprConsent.CustomerType.Equals(StringConstants.GdprCustomerType.C) ||
                        gdprConsent.CustomerType.Equals(StringConstants.GdprCustomerType.B) ||
                        gdprConsent.CustomerType.Equals(StringConstants.GdprCustomerType.V)) &&
                        gdprConsent.Status == (int)Enums.GdprStatus.NotProcessed)
                    {
                        _gdprService.UpdateForCustomerTypeCBVAndStatusZero(gdprConsent);
                    }
                    else if ((gdprConsent.CustomerType.Equals(StringConstants.GdprCustomerType.C) && gdprConsent.Status == (int)Enums.GdprStatus.EmailSent))
                    {
                        _gdprService.UpdateForCustomerTypeCAndStatusOne(gdprConsent);
                    }
                    else if ((gdprConsent.CustomerType.Equals(StringConstants.GdprCustomerType.O) && gdprConsent.Status == (int)Enums.GdprStatus.EmailSent))
                    {
                        _gdprService.UpdateForCustomerTypeOAndStatusOne(gdprConsent);
                    }
                    else if (gdprConsent.Status == (int)Enums.GdprStatus.ConfirmEmailAddressRequired)
                    {
                        _gdprService.UpdateForCustomerHasStatusFour(gdprConsent);
                    }
                    OnStatusChanged($"Checked gdpr consent with id {gdprConsent.Id} in batch of {gdprConsentRows.Count()} items");
                }

                if (!gdprConsentRows.Any())
                {
                    return "There is not any gdpr consent item which need to update status!";
                }
                return $"Updated {gdprConsentRows.Count()} gdpr consent items";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GdprConsentUpdateStatusJob: {ex}");
                return ex.ToString();
            }
        }
    }
}