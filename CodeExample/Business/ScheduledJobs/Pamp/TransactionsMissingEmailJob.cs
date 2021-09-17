using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Newtonsoft.Json.Linq;
using TRM.IntegrationServices.Interfaces;
using TRM.Web.Business.Email;
using TRM.Web.Models.EntityFramework.PampQuoteRequest;
using TRM.Web.Models.Pages;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.Pamp
{
    [ScheduledPlugIn(DisplayName = "[Bullion] Transactions Missing Email")]
    public class TransactionsMissingEmailJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        protected Lazy<IEmailHelper> EmailHelper = new Lazy<IEmailHelper>(() =>
        {
            return ServiceLocator.Current.GetInstance<IEmailHelper>();
        });

        protected Lazy<IExportTransactionsRepository> ExportTransactionRepository = new Lazy<IExportTransactionsRepository>(() =>
        {
            return ServiceLocator.Current.GetInstance<IExportTransactionsRepository>();
        });

        protected Lazy<IContentLoader> ContentLoader = new Lazy<IContentLoader>(() =>
        {
            return ServiceLocator.Current.GetInstance<IContentLoader>();
        });

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public TransactionsMissingEmailJob()
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

                var startPage = ContentLoader.Value.Get<StartPage>(SiteDefinition.Current.StartPage);

                if (startPage == null)
                    return "Job Failed: StartPage cannot be null";

                if (startPage.PampQuotesCheckEmail == null)
                    return "Job Failed: StartPage.PampQuotesCheckEmail property cannot be null";

                if (startPage.PampQuotesCheckEmailDays == 0)
                    return "Job Failed: StartPage.PampQuotesCheckEmailDays property cannot be zero";

                if (string.IsNullOrEmpty(startPage.PampQuotesCheckEmailAddress))
                    return "Job Failed: StartPage.PampQuotesCheckEmailAddress property cannot be null or empty";

                var dt = DateTime.Now;
                var quoteIdsFrom = dt.AddDays(-startPage.PampQuotesCheckEmailDays).Date;
                var quoteIdsTo = dt;

                var messaging = new StringBuilder();

                messaging.AppendLine($"PAMP Descrepencies for {quoteIdsFrom:dd MMM yyyy} to {quoteIdsTo:dd MMM yyyy}");

                //Add implementation
                var pampQuoteIds = GetPampQuoteIds(quoteIdsFrom, quoteIdsTo);

                if (pampQuoteIds == null || !pampQuoteIds.Any())
                    messaging.AppendLine($"No PAMP Quote Ids found");
                else
                {
                    var transactionsFrom = quoteIdsFrom;
                    var transactionsTo = dt;

                    var purchaseOrderQuoteIds = ExportTransactionRepository.Value.GetQuoteIdsForPurchaseOrderExports(transactionsFrom, transactionsTo);

                    if (purchaseOrderQuoteIds == null || !purchaseOrderQuoteIds.Any())
                        messaging.AppendLine($"No purchase order exports found");

                    var sellFromVaultQuoteIds = ExportTransactionRepository.Value.GetQuoteIdsForSellFromVaultExports(transactionsFrom, transactionsTo);

                    if (sellFromVaultQuoteIds == null || !sellFromVaultQuoteIds.Any())
                        messaging.AppendLine($"No sell from vault exports found");

                    var transactionQuoteIds = purchaseOrderQuoteIds.Union(sellFromVaultQuoteIds);

                    var quoteIdsNotFound = pampQuoteIds.Except(transactionQuoteIds);

                    if (quoteIdsNotFound != null && quoteIdsNotFound.Any())
                    {
                        messaging.AppendLine($"PAMP Quote Ids not accounted for:");
                        foreach (var q in quoteIdsNotFound)
                        {
                            messaging.AppendLine($"{q}");
                        }
                    }
                }

                var email = ContentLoader.Value.Get<TRMEmailPage>(startPage.PampQuotesCheckEmail);
                EmailHelper.Value.SendEmailForMissingPampQuoteIds(
                    email, startPage.PampQuotesCheckEmailAddress, messaging.ToString());

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                return "State of Pamp quotes and relevant missing transactions have been sent.";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        private IEnumerable<string> GetPampQuoteIds(DateTime @from, DateTime @to)
        {
            using (var db = new PremiumRequestDbContext())
            {
                return (
                    from r in db.PremiumRequestHistoricalData
                    where r.Command == "FinishQuoteRequest" && r.CreatedDate >= @from && r.CreatedDate <= @to
                    select r.SerializedInputDto
                )
                .ToList()
                .Select(x => (string)JObject.Parse(x).SelectToken("$.quoteId"));
            }
        }
    }
}