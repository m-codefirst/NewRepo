using EPiServer;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using System;
using TRM.IntegrationServices.Interfaces;
using TRM.Web.Models.Pages;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.Security
{
    [ScheduledPlugIn(
        DisplayName = "[Security] Delete Impersonation Log Job",
        Description = @"Deletes impersonation logs n days old")]
    public class DeleteImpersonationLogJob : ScheduledJobBase
    {
        private Lazy<IContentLoader> ContentLoader => new Lazy<IContentLoader>(() =>
        {
            return ServiceLocator.Current.GetInstance<IContentLoader>();
        });

        private Lazy<IImpersonationLogService> ImpersonationLogService => new Lazy<IImpersonationLogService>(() =>
        {
            return ServiceLocator.Current.GetInstance<IImpersonationLogService>();
        });
        
        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public DeleteImpersonationLogJob()
        {
            IsStoppable = false;
        }

        public override string Execute()
        {
            try
            {
                OnStatusChanged($"Starting execution of {GetType()}");

                var startPage = ContentLoader.Value.Get<StartPage>(ContentReference.StartPage);
                var settingsPage = ContentLoader.Value.Get<CustomToolSettingsPage>(startPage.CustomToolSettingsPage);
                if (settingsPage == null)
                {
                    return "The 'Custom Tool Settings Page' could not be found.";
                }

                var daysOld = settingsPage.LogRetentionDays;

                if (ImpersonationLogService.Value.DeleteOlderThan(DateTime.Now.AddDays(-daysOld)))
                {
                    return "Job successfully completed";
                }
                return "Job failed to complete";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}