using System;
using System.Data.Entity;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Models.DDS;
using System.Linq;
using TRM.Web.Business.DataAccess;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.MetalPrice
{
    [ScheduledPlugIn(
        DisplayName = "[Bullion] Metal Price Sync History Job",
        Description =
            @"A scheduled job needs to be created that runs at about 22:00 GMT daily.
            It will populate the 'close of play' prices for the previous day that are used in the current metal price header.

            This will be a single record for each currency/metal pair and will be replaced on each running of the job - if appropriate.

            The decision to replace the values in the table will be based on the time of running the job and the age of the data in the table. 
            i.e. If the current time is after 22:00 and the data is from before 22:00 then replace it.

            This needs to be held seperately from the main historical prices table for speed of access as the main table will typically have > million rows."
    )]
    public class MetalPriceSyncHistoryJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        protected Lazy<PampMetalPriceSyncRepository> PampMetalPriceSyncRepository = new Lazy<PampMetalPriceSyncRepository>(() =>
        {
            return ServiceLocator.Current.GetInstance<PampMetalPriceSyncRepository>();
        });

        protected Lazy<PampMetalPriceSyncHistoryRepository> PampMetalHistoryRepository = new Lazy<PampMetalPriceSyncHistoryRepository>(() =>
        {
            return ServiceLocator.Current.GetInstance<PampMetalPriceSyncHistoryRepository>();
        });

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public MetalPriceSyncHistoryJob()
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
                var oldData = PampMetalPriceSyncRepository.Value
                    .GetList()
                    .Where(x => DbFunctions.DiffMinutes(x.CreatedDate, DateTime.UtcNow) <= 0)
                    .ToList()
                    .Select(x => new PampMetalPriceSyncHistory()
                    {
                        Id = x.Id,
                        CreatedDate = x.CreatedDate,
                        Currency = x.Currency,
                        CustomerBuy = x.CustomerBuy,
                        GoldPrice = x.GoldPrice,
                        PlatinumPrice = x.PlatinumPrice,
                        SilverPrice = x.SilverPrice
                    });

                int movedCount = 0;
                if (oldData.Any())
                {
                    movedCount += PampMetalHistoryRepository.Value.BulkInsert(oldData);
                }

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                return $"Moved {movedCount} metal prices to history table.";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
