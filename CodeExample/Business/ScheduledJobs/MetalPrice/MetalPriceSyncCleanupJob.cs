using System;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Business.DataAccess;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.MetalPrice
{
    [ScheduledPlugIn(
        DisplayName = "[Bullion] Metal Price Sync Cleanup Job",
        Description = @"Delete Old Indicative Prices")]
    public class MetalPriceSyncCleanupJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        protected Lazy<PampMetalPriceSyncRepository> PampMetalPriceSyncRepository = new Lazy<PampMetalPriceSyncRepository>(() =>
        {
            return ServiceLocator.Current.GetInstance<PampMetalPriceSyncRepository>();
        });

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public MetalPriceSyncCleanupJob()
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
                OnStatusChanged($"Starting execution of {this.GetType()}");

                // Add implementation
                PampMetalPriceSyncRepository.Value.CleanupData();

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                return "Finished deleting unnecessary data";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
