using System;
using System.Web;
using System.Web.Mvc;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.Web;
using EPiServer.Web.Routing.Segments.Internal;
using Newtonsoft.Json.Linq;
using TRM.Web.Helpers;

namespace TRM.Web.Business.GoogleTagManager
{
    public class TrmGtmCommerceDataLayerBuilder : IBuildCommerceGtmDataLayer
    {
        protected static readonly ILogger Logger = LogManager.GetLogger(typeof(TrmGtmCommerceDataLayerBuilder));
        private readonly IAmGoogleTagManagerHelper _googleTagManagerHelper;
        private readonly IAmContactHelper _contactHelper;


        public TrmGtmCommerceDataLayerBuilder(IAmGoogleTagManagerHelper googleTagManagerHelper, IAmContactHelper contactHelper)
        {
            _googleTagManagerHelper = googleTagManagerHelper;
            _contactHelper = contactHelper;
        }

        public const string Loggedon = "loggedOn";
        public const string Loggedoff = "loggedOff";
        public const string SessionGtmVisitorId = "GTMVisitorId";

        public virtual void Push(string variableName, JToken value, HttpContextBase httpContext)
        {
            var jobject = httpContext.Items["GtmDataLayer"] as JObject;
            if (jobject == null)
            {
                Logger.Warning("An expected call to Push some additional data when no DataLayer has already been pushed; skipping tracking.");
            }
            else
            {
                jobject.Add(variableName, value);
            }
        }

        public virtual void Push(ContentData contentData, ActionExecutingContext filterContext)
        {
            Push(contentData, false, filterContext);
        }

        public virtual void Push(ContentData contentData, bool isPartial, ActionExecutingContext filterContext)
        {
            switch (RequestSegmentContext.CurrentContextMode)
            {
                case ContextMode.Edit:
                    break;
                case ContextMode.Preview:
                    break;
                default:
                    var entryData = contentData as EntryContentBase;
                    if (entryData != null)
                    {
                        if (isPartial)
                        {
                            PushEntryContent(entryData, true, filterContext);
                            break;
                        }
                        PushEntryContent(entryData, filterContext);
                        break;
                    }
                    var nodeData = contentData as NodeContentBase;
                    if (nodeData == null)
                    { 
                        break;
                    }
                    PushNodeContent(nodeData, filterContext);
                    break;
            }
        }

        protected virtual void PushEntryContent(EntryContentBase entryContent, ActionExecutingContext filterContext)
        {
            try
            {
                var httpContextBase = HttpContext.Current.ContextBaseOrNull();
                if (filterContext != null)
                {
                    httpContextBase = filterContext.HttpContext;
                }
                if (httpContextBase.Items["GtmDataLayer"] is JObject)
                {
                    Logger.Warning("An expected call to Push some EntryContent when EntryContent has already been pushed; overwriting existing data.");
                }

                var dataLayer = _googleTagManagerHelper.GetEntryObject(entryContent.ContentLink);
                AddCustomerDataToDataLayer(dataLayer);
                httpContextBase.Items["GtmDataLayer"] = dataLayer;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to Push EntryContent to GTM DataLayer. Name = {entryContent.DisplayName}, ContentLink.ID = {entryContent.ContentLink.ID}.", ex);
            }
        }

        protected virtual void PushEntryContent(EntryContentBase entryContent, bool isPartial, ActionExecutingContext filterContext)
        {
            if (!isPartial)
            {
                PushEntryContent(entryContent, filterContext);
            }
            else
            {
                try
                {
                    var jobject = filterContext.HttpContext.Items["GtmDataLayer"] as JObject;
                    if (jobject == null)
                    {
                        Logger.Warning("An expected call to Push some partial PageData when no PageData has already been pushed; skipping tracking.");
                    }
                    if (jobject != null)
                    {
                        var dataLayer = JObject.FromObject(new
                        {
                            sku = entryContent.Code,
                            name = entryContent.DisplayName,
                            price = 0,
                            contentLinkId = entryContent.ContentLink.ID,
                            contentType = entryContent.ContentType
                        });

                        AddCustomerDataToDataLayer(jobject);
                        (jobject["partialEntries"] as JArray)?.Add(dataLayer);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to Push Partial EntryData to GTM DataLayer. Name = {entryContent.DisplayName}, ContentLink.ID = {entryContent.ContentLink.ID}.", ex);
                }
            }
        }

        protected virtual void PushNodeContent(NodeContentBase nodeContent, ActionExecutingContext filterContext)
        {
            try
            {
                var httpContextBase = HttpContext.Current.ContextBaseOrNull();
                if (filterContext != null)
                {
                    httpContextBase = filterContext.HttpContext;
                }
                if (httpContextBase.Items["GtmDataLayer"] is JObject)
                {
                    Logger.Warning("An expected call to Push some EntryContent when EntryContent has already been pushed; overwriting existing data.");
                }

                var dataLayer = _googleTagManagerHelper.GetNodeObject(nodeContent.ContentLink);
                httpContextBase.Items["GtmDataLayer"] = dataLayer;

                AddCustomerDataToDataLayer(dataLayer);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to Push NodeContent to GTM DataLayer.", ex);
            }
        }

        private void AddCustomerDataToDataLayer(JObject dataLayer)
        {
            if (dataLayer.ContainsKey("ObsAccountNumber")) return;

            var customer = _contactHelper.GetCheckoutCustomerContact();
            if (customer != null &&
                (customer.Properties.Contains(Shared.Constants.StringConstants.CustomFields.ObsAccountNumber) ||
                 customer.Properties.Contains(Shared.Constants.StringConstants.CustomFields.BullionObsAccountNumber)))
            {
                dataLayer["ObsAccountNumber"] = customer
                    .Properties[Shared.Constants.StringConstants.CustomFields.ObsAccountNumber]?.Value?.ToString();
                dataLayer["BullionObsAccountNumber"] = customer
                    .Properties[Shared.Constants.StringConstants.CustomFields.BullionObsAccountNumber]?.Value
                    ?.ToString();
            }
        }
    }
}
