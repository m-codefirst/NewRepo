using System;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Services;
using System.Linq;

namespace TRM.Web.Business.ScheduledJobs.GlobalPurchaseLimit
{
    [ScheduledPlugIn(
        DisplayName = "[Bullion] Global Purchase Limit Scheduled Job",
        Description = @"A scheduled job will review all Precious Metal orders in the last 24 hours, both Signature and non-signature product variants.")]
    public class GlobalPurchaseLimitScheduledJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        protected Lazy<IGlobalPurchaseLimitService> GlobalPurchaseLimitService = new Lazy<IGlobalPurchaseLimitService>(() =>
        {
            return ServiceLocator.Current.GetInstance<IGlobalPurchaseLimitService>();
        });

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public GlobalPurchaseLimitScheduledJob()
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

        public override string Execute()
        {
            try
            {
                //Call OnStatusChanged to periodically notify progress of job for manually started jobs
                OnStatusChanged($"Starting execution of {this.GetType()}");

                var result = GlobalPurchaseLimitService.Value.MonitorPurchaseLimitExceeded();

                if (result.Messages.Any())
                {
                    return string.Join("\n\r", result.Messages);
                }

                //For long running jobs periodically check if stop is signaled and if so stop execution
                return _stopSignaled ? "Stop of job was called" : "Finished global purchase limit job!";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}