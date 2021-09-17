using System;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Blobs;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Helpers;
using TRM.Web.Models.Pages;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.ClearDownPersonalisedData
{
    [ScheduledPlugIn(
        DisplayName = "Clear Personalised Data", 
        Description = "Clear down 'personalised' data stored for abandoned baskets that is older than 30 days")]
    public class ClearDownPersonalisedData : ScheduledJobBase
    {
        private bool _stopSignaled;
        private readonly IContentLoader _contentLoader;
        private readonly IBlobFactory _blobFactory;
        private readonly IAmPersonalisedDataHelper _personalisedDataHelper;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public ClearDownPersonalisedData(
            IContentLoader contentLoader,
            IBlobFactory blobFactory, 
            IAmPersonalisedDataHelper personalisedDataHelper)
        {
            IsStoppable = true;
            _contentLoader = contentLoader;
            _blobFactory = blobFactory;
            _personalisedDataHelper = personalisedDataHelper;
        }

        public override string Execute()
        {
            try
            {
                //Call OnStatusChanged to periodically notify progress of job for manually started jobs
                OnStatusChanged($"Starting execution of {this.GetType()}");

                //Add implementation
                var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);

                if (startPage == null) return "Please set up Start Page";
                var personalisationContainerId = startPage.PersonalisationContainerId;

                Guid containerGuid;
                if (!Guid.TryParse(personalisationContainerId, out containerGuid)) return "Please set up Personalisation Container ID in Start Page";

                //Writing custom data to a blob
                var morethanDay = startPage.PersonalisationClearDataMoreThanDays <= 0 ? 30 : startPage.PersonalisationClearDataMoreThanDays;
                var personalisedDatas = _personalisedDataHelper.GetPersonalisedDatasByCreated(-morethanDay);
                if (personalisedDatas.Any())
                {
                    foreach (var personalised in personalisedDatas)
                    {
                        try
                        {
                            var uriImage = new Uri(personalised.ImageGuid);
                            _blobFactory.Delete(uriImage);
                            _personalisedDataHelper.Delete(personalised);
                        }
                        catch (Exception)
                        {
                            _personalisedDataHelper.UpdateStatus(personalised, false);
                        }
                    }
                }
                else
                {
                    return "There is not any personalised images found";
                }

                return _stopSignaled ? "Stop of job was called" : $"Deleted {personalisedDatas.Count} personalised images successfully";
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