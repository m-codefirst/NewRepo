using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.DataAccess;
using TRM.Web.Extentions;
using TRM.Web.Models.Catalog.DDS;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.QueueIt
{
    [ScheduledPlugIn(
        DisplayName = "Refresh Known User Configuration", 
        Description = "Refresh Known User configuration from Queue It")]
    public class RefreshKnownUserConfiguration : ScheduledJobBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(RefreshKnownUserConfiguration));

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public override string Execute()
        {
            try
            {
                var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
                if (startPage == null)
                {
                    return "Start Page was Null";
                }

                if (startPage.TurnKnownUserOff)
                {
                    return "Start Page > Performance > Turn Known User Off is true";
                }
                if (string.IsNullOrEmpty(startPage.KnownUserCustomerId))
                {
                    return "Start Page > Performance > Known User Customer Id is not set";
                }

                if (string.IsNullOrEmpty(startPage.KnownUserSecretKey))
                {
                    return "Start Page > Performance > Known User Secret Key is not set";
                }

                var result = GetIntegrationConfig(startPage.KnownUserCustomerId);

                if (string.IsNullOrEmpty(result))
                {
                    return "Response was null - check the logs for more info!";
                }

                using (var repository = ServiceLocator.Current.GetInstance<IRepository<QueueItKnownUserConfiguration>>())
                {
                    if (repository.Find(x => x.Value == result).Any())
                    {
                        return "Same value - no need to save";
                    }
                    repository.DeleteAll();

                    var saveDate = DateTime.Now.ToString("dd / MM / yyyy hh: mm tt");
                    var configToSave = new QueueItKnownUserConfiguration()
                    {
                        DisplayName = saveDate,
                        Value = result
                    };

                    repository.Save(configToSave);
                    return "Repository Updated - " + saveDate;
                }
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        public string GetIntegrationConfig(string customerId)
        {
            var tryCount = 0;
            const int downloadTimeoutMs = 4000;
            while (tryCount < 5)
            {
                var timeBaseQueryString = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString(CultureInfo.CurrentCulture);
                var configUrl = string.Format("https://{0}.queue-it.net/status/integrationconfig/{0}?qr={1}", customerId, timeBaseQueryString);
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(configUrl);
                    request.Timeout = downloadTimeoutMs;
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                            throw new Exception($"It was not sucessful retriving config file status code {response.StatusCode} from {configUrl}");
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ++tryCount;
                    if (tryCount >= 5)
                    {
                        Logger.Log(Level.Error, ex.Message + "|" + ex.StackTrace);
                    }

                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.200 * tryCount));
                }
            }

            return null;
        }
    }
}