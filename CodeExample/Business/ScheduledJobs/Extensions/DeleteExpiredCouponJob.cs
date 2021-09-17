using System;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Services;
using TRM.Web.Services.Coupons;

namespace TRM.Web.Business.ScheduledJobs.Extensions
{
    [ScheduledPlugIn(DisplayName = "Delete Expired Coupons Job")]
    public class DeleteExpiredCouponJob : ScheduledJobBase
    {
        private bool _stopSignaled;
        private readonly UniqueCouponService _couponService;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public DeleteExpiredCouponJob(UniqueCouponService couponService)
        {
            IsStoppable = true;
            _couponService = couponService;
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
                var result = _couponService.DeleteExpiredCoupons();

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                if (result)
                {
                    return "Job was sucessfully run";
                }
                else
                {
                    return "There was a problem when running the job";
                }
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
