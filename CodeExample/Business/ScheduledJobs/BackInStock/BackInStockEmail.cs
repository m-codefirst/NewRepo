using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using System;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.BackInStock
{
    [ScheduledPlugIn(
        DisplayName = "Back In Stock Email", 
        Description = "Notify subcribed users if variant is back in stock")]
    public class BackInStockEmail : ScheduledJobBase
    {
        private readonly IBackInStockService _backInStockService;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public BackInStockEmail(IBackInStockService backInStockService)
        {
            _backInStockService = backInStockService;
        }        
        
        public override string Execute()
        {
            try
            {
                OnStatusChanged($"Starting execution of {this.GetType()}");

                return _backInStockService.NotifyForAvailableVariants();
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
