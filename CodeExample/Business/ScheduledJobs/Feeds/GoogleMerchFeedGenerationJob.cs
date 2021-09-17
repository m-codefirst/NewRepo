using System;
using EPiServer;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.Web;
using TRM.Web.Models.Pages;
using TRM.Web.Services;
using TRM.Web.Services.MerchandiseFeed;
using TRM.Web.Services.MerchandiseFeed.Pages;

namespace TRM.Web.Business.ScheduledJobs.Feeds
{
    [ScheduledPlugIn(DisplayName = "[Feeds] Google Merch Feed Generation")]
    public class GoogleMerchFeedGenerationJob : ScheduledJobBase
    {
        private bool _stopSignaled;
        private readonly IContentLoader _contentLoader;
        private readonly IGenerateFeed _feedGeneration;
        private readonly IJobFailedHandler _failedHandler;

        public GoogleMerchFeedGenerationJob(IContentLoader contentLoader, IGenerateFeed feedGeneration, IJobFailedHandler failedHandler)
        {
            IsStoppable = true;
            _contentLoader = contentLoader;
            _feedGeneration = feedGeneration;
            _failedHandler = failedHandler;
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

                var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);

                if (startPage == null)
                    return "Could not find Start Page";

                var feedPage = _contentLoader.Get<FeedPage>(startPage.GoogleMerchFeed);

                if (feedPage == null)
                    return "Could not find Google Merchandise Feed Page";

                _feedGeneration.Generate(feedPage);

                //For long running jobs periodically check if stop is signaled and if so stop execution
                if (_stopSignaled)
                {
                    return "Stop of job was called";
                }

                return "Feed successfully generated";
            }
            catch (Exception ex)
            {
                _failedHandler.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
