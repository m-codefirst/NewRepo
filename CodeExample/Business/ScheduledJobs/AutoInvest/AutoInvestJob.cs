using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Shared.Extensions;
using TRM.Web.Extentions;
using TRM.Web.Helpers.AutoInvest;
using TRM.Web.Models.EntityFramework.CustomerContactContext;
using TRM.Web.Services;
using TRM.Web.Services.AutoInvest;

namespace TRM.Web.Business.ScheduledJobs.AutoInvest
{
    [ScheduledPlugIn(DisplayName = "Auto Invest Job")]
    public class AutoInvestJob : ScheduledJobBase
    {
        private bool _stopSignaled;
        private readonly IAutoPurchaseService _autoPurchaseService;
        private readonly IAutoPurchaseHelper _autoPurchaseHelper;
        private readonly IAutoPurchaseMailingService _autoPurchaseMailingService;
        private readonly IAutoPurchaseUsersProvider _autoPurchaseUsersProvider;
        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() => ServiceLocator.Current.GetInstance<IJobFailedHandler>());

        public AutoInvestJob(IAutoPurchaseService autoPurchaseService, IAutoPurchaseHelper autoPurchaseHelper, IAutoPurchaseMailingService autoPurchaseMailingService, IAutoPurchaseUsersProvider autoPurchaseUsersProvider)
        {
            IsStoppable = true;
            _autoPurchaseService = autoPurchaseService;
            _autoPurchaseHelper = autoPurchaseHelper;
            _autoPurchaseMailingService = autoPurchaseMailingService;
            _autoPurchaseUsersProvider = autoPurchaseUsersProvider;
        }

        public override string Execute()
        {
            try
            {
                OnStatusChanged($"Starting execution of {this.GetType()}");
                var currentDate = DateTime.Now.Date;

                if (!_autoPurchaseHelper.IsValidAutoInvestPageConfigureInStartPage())
                {
                    return "Auto Invest Setting Page is not configured in Start page.";
                }
                if (_autoPurchaseHelper.IsStopTradingActivated())
                {
                    return "Stop Trading flag is enabled";
                }
                if (!_autoPurchaseHelper.IsAutoInvestActivated())
                {
                    return "Auto Invest is not activated";
                }
                if (_autoPurchaseHelper.IsDisallowedDate(currentDate))
                {
                    return "Current date is disallowed";
                }

                var startDate = _autoPurchaseHelper.GetStartDate(currentDate);

                var usersToProcess = _autoPurchaseUsersProvider.GetUsers(startDate, currentDate).ToList();
                if (!usersToProcess.Any())
                {
                    return $"Job finished. No Auto Invest user found for processing.";
                }

                var chunks = usersToProcess.GetChunks(_autoPurchaseHelper.GetBatchSize());
                var processedUsers = new List<AutoPurchaseProcessedUserDto>();
                var delay = _autoPurchaseHelper.GetBatchDelay();

                foreach (List<cls_Contact> contacts in chunks)
                {
                    var processedInChunk = _autoPurchaseService.UpdateContactsInRange(contacts);
                    processedUsers.AddRange(processedInChunk);
                    _autoPurchaseMailingService.SendMailing(processedInChunk);

                    OnStatusChanged(FormatJobStatus(usersToProcess, processedUsers));

                    if (_stopSignaled)
                    {
                        break;
                    }

                    Delay(delay);
                }

                return "Job Finished. " + FormatJobStatus(usersToProcess, processedUsers, "<br/>");
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

        private static void Delay(int delay)
        {
            if (delay <= 0)
            {
                return;
            }

            System.Threading.Thread.Sleep(delay);
        }

        private string FormatJobStatus(List<cls_Contact> usersToProcess, List<AutoPurchaseProcessedUserDto> processedUsers, string newLine = " ")
        {
            string formatted = $"Processed: {processedUsers.Count} of {usersToProcess.Count} ||{newLine}" +
                                       $"{string.Join(newLine, processedUsers.GroupBy(x => x.Status).Select(x => $"{x.Key.GetDescriptionAttribute() }: {x.Count()}"))}";

            if (_stopSignaled)
            {
                formatted = "Job stopped manually.<br/> " + formatted;
            }

            return formatted;
        }
    }
}