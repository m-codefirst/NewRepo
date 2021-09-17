using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EPiServer;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing.Segments.Internal;
using Hephaestus.CMS.DataAccess;
using QueueIT.KnownUserV3.SDK;
using QueueIT.KnownUserV3.SDK.IntegrationConfig;
using TRM.Web.Models.Catalog.DDS;
using TRM.Web.Models.Pages;

namespace TRM.Web.Business
{
    public class QueueItKnownUserFilterAttribute : FilterAttribute, IActionFilter
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(QueueItKnownUserFilterAttribute));
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
                return;
            if (filterContext.IsChildAction)
                return;
            if (filterContext.HttpContext.Request.Url == null)
                return;

            switch (RequestSegmentContext.CurrentContextMode)
            {
                case ContextMode.Edit:
                    break;
                case ContextMode.Preview:
                    break;
                default:
                    HandleRequest(filterContext);
                    break;
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        protected virtual void HandleRequest(ActionExecutingContext filterContext)
        {
            try
            {
                var startPageContentLink = SiteDefinition.Current.StartPage;
                if (startPageContentLink == null || startPageContentLink.ID < 1)
                {
                    Logger.Log(Level.Debug, "StartPageContent Link is null/0");
                    return;
                }
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
                var actualStartPage = contentLoader?.Get<StartPage>(startPageContentLink);
                if (actualStartPage == null || actualStartPage.TurnKnownUserOff)
                {
                    Logger.Log(Level.Debug, "Actual Start Page is null or Turn Known User is Off");
                    return;
                }

                var userAgentString = filterContext.HttpContext.Request.UserAgent ?? string.Empty;
                var userAgents = actualStartPage.UserAgentsToBypassQueue?.Split(',');
                if (userAgents != null && userAgents.Any(userAgentString.Contains))
                {
                    Logger.Log(Level.Debug, "Queue bypassed based on user agent");
                    return;
                }

                var urlParts = actualStartPage.UrlPartsToBypassQueue?.Split(',');
                if (urlParts != null && urlParts.Any(filterContext.HttpContext.Request.Url.ToString().ToLower().Contains))
                {
                    Logger.Log(Level.Debug, "Queue bypassed based on Url part");
                    return;
                }

                var customerId = actualStartPage.KnownUserCustomerId;//"maginus";
                var secretKey = actualStartPage.KnownUserSecretKey;//"c2d67223-11c8-4038-af7d-ca4398a3835a0ae5c8a9-0789-408f-92aa-80fb320ada65";
                if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(secretKey))
                {
                    Logger.Log(Level.Debug, "Customer Id or Secret Key is null/Empty");
                    return;
                }

                var request = filterContext.HttpContext.Request;
                if (request.Url == null)
                {
                    Logger.Log(Level.Debug, "Request URL is null?");
                    return;
                }
                var response = filterContext.HttpContext.Response;
                
                var queueitToken = request.QueryString[KnownUser.QueueITTokenKey];
                var pureUrl = Regex.Replace(request.Url.ToString(), @"([\?&])(" + KnownUser.QueueITTokenKey + "=[^&]*)", string.Empty, RegexOptions.IgnoreCase);
                // The pureUrl is used to match Triggers and as the Target url (where to return the users to)
                // It is therefor important that the pureUrl is exactly the url of the users browsers. So if your webserver is 
                // e.g. behind a load balancer that modifies the host name or port, reformat the pureUrl before proceeding

                CustomerIntegration integrationConfig;
                using (var repository = ServiceLocator.Current.GetInstance<IRepository<QueueItKnownUserConfiguration>>())
                {
                    var ddsConfig = repository.FindAll().FirstOrDefault();
                    if (ddsConfig == null || string.IsNullOrEmpty(ddsConfig.Value))
                    {
                        Logger.Log(Level.Debug, "Dds Config/Dds Config Value is null/Empty");
                        return;
                    }

                    var deserializer = new JavaScriptSerializer();
                    var deserialized = deserializer.Deserialize<CustomerIntegration>(ddsConfig.Value);
                    if (deserialized == null)
                    {
                        Logger.Log(Level.Debug, "Couldn't deserialize Dds Config Value");
                        return;
                    }
                    integrationConfig = deserialized;
                }

                // Don't use the static one - we have a scheduled job.
                //var integrationConfig = IntegrationConfigProvider.GetCachedIntegrationConfig(customerId);

                //Verify if the user has been through the queue
                var validationResult = KnownUser.ValidateRequestByIntegrationConfig(pureUrl, queueitToken, integrationConfig, customerId, secretKey);

                if (validationResult.DoRedirect)
                {
                    //Adding no cache headers to prevent browsers to cache requests
                    response.Cache.SetCacheability(HttpCacheability.NoCache);
                    response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
                    response.Cache.SetNoStore();
                    //end
                    //Send the user to the queue - either because hash was missing or because is was invalid
                    response.Redirect(validationResult.RedirectUrl, false);
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    //Request can continue - we remove queueittoken form querystring parameter to avoid sharing of user specific token
                    if (HttpContext.Current.Request.Url.ToString().Contains(KnownUser.QueueITTokenKey) && !string.IsNullOrEmpty(validationResult.ActionType))
                    {
                        response.Redirect(pureUrl, false);
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                    }
                }
            }
            catch (Exception)
            {
                //There was an error validationg the request
                //Use your own logging framework to log the Exception
                //This was a configuration exception, so we let the user continue
            };
        }
    }
}