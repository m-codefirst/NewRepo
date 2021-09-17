using System;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Services;
using TRM.Web.Services.InvoiceStatements;

namespace TRM.Web.Business.ScheduledJobs.Documents
{
    [ScheduledPlugIn(
        DisplayName = "[Bullion] Download Statements/Invoices Pdf")]
    public class DownloadStatementsInvoicesPdfServiceJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        private readonly IBullionDocumentService _bullionDocumentService;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public DownloadStatementsInvoicesPdfServiceJob(IBullionDocumentService bullionDocumentService)
        {
            _bullionDocumentService = bullionDocumentService;
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

                //Add implementation
                var downloadResult = _bullionDocumentService.DownloadAndSavePdfFilesAsync().Result;

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                return $"Finished {this.GetType()}.<br>{downloadResult}";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
