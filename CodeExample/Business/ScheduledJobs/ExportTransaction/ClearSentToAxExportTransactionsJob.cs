using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.IntegrationServices.Constants;
using TRM.IntegrationServices.Interfaces;
using TRM.Web.Models.Pages;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.ExportTransaction
{
    [ScheduledPlugIn(
        DisplayName = "[Bullion] Delete SentToAX Export Transactions Job", 
        Description = "Delete the SentToAx Export Transaction Records after n days (n days setting on startpage)")]
    public class ClearSentToAxExportTransactionsJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        protected Lazy<IExportTransactionsRepository> ExportTransactionsRepository = new Lazy<IExportTransactionsRepository>(() => ServiceLocator.Current.GetInstance<IExportTransactionsRepository>());

        protected Lazy<IContentLoader> ContentLoader = new Lazy<IContentLoader>(() => ServiceLocator.Current.GetInstance<IContentLoader>());

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public ClearSentToAxExportTransactionsJob()
        {
            IsStoppable = true;
        }

        public override string Execute()
        {
            try
            {
                //Call OnStatusChanged to periodically notify progress of job for manually started jobs
                OnStatusChanged($"Starting execution of {GetType()}");

                var startPage = ContentLoader.Value.Get<StartPage>(ContentReference.StartPage);

                if (startPage == null) return "Please set up Start Page";

                var daysToDelete = startPage.DaysToDelete > 0 ? startPage.DaysToDelete : 7;

                string errorMsg;
                var numberDeleteRecords = ExportTransactionsRepository.Value.DeleteExportTransactionByModifiedDate(
                    StringConstants.AxIntegrationStatus.SentToAX, daysToDelete, out errorMsg);

                //For long running jobs periodically check if stop is signaled and if so stop execution
                return _stopSignaled ? "Stop of job was called"
                    : numberDeleteRecords == 0 && !string.IsNullOrEmpty(errorMsg)
                    ? errorMsg : $"Deleted {numberDeleteRecords} SentToAX Export Transaction records successfully";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }
    }
}