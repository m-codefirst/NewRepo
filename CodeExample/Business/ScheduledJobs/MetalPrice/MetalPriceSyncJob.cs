using System;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Business.DataAccess;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.MetalPrice
{
    [ScheduledPlugIn(
        DisplayName = "[Bullion] Metal Price Sync Job",
        Description = @"Cache Indicative Metal Prices- Buy and Sell price for each metal and each currency.
                        Should get every 30 seconds and store prices and timestamp.")]
    public class MetalPriceSyncJob : ScheduledJobBase
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

        public MetalPriceSyncJob()
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

                // Add implementation
                PampMetalPriceSyncRepository.Value.GetLivePrices();

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                return "Got metal price and stored prices and timestamp to our local Db.";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
