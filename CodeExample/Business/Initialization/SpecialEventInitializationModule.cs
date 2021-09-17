using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using TRM.Web.Helpers;
using TRM.Web.Models.Pages;
using TRM.Web.Models.SpecialEvents;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class SpecialEventInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.PublishingContent += contentEvents_PublishingContent;
            contentEvents.SavingContent += ContentEvents_SavingContent;
        }

        private void ContentEvents_SavingContent(object sender, ContentEventArgs e)
        {
            ValidateEventTypes(e);
        }

        public void Preload(string[] parameters) { }

        public void Uninitialize(InitializationEngine context)
        {
            var contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            contentEvents.PublishingContent -= contentEvents_PublishingContent;
            contentEvents.SavingContent -= ContentEvents_SavingContent;
        }
        private void ValidateEventTypes(ContentEventArgs e, bool isContentLoaded = false)
        {
            if (e.Content is OccasionsSettingsPage)
            {
                var specialEventHelper = ServiceLocator.Current.GetInstance<IAmSpecialEventsHelper>();

                var osp = (OccasionsSettingsPage)e.Content;
                var currentValue = specialEventHelper.GetSettingPage();
                if (null == currentValue || currentValue.EventTypes == null)
                    return;

                var currEventTypes = currentValue.EventTypes.ToList();
                var eventTypesUpdated = new List<SpecialEventType>();
                if (null != osp.EventTypes)
                {
                    foreach (var item in osp.EventTypes)
                    {
                        var oIndex = currEventTypes.FindIndex(et => et.Code == item.Code);
                        if (oIndex > -1)
                            currEventTypes.RemoveAt(oIndex);
                        else
                        {
                            eventTypesUpdated.Add(item);
                        }
                    }
                }

                foreach (var evT in eventTypesUpdated)
                {
                    var existingApps = specialEventHelper.Find(x => x.EventTypeCode == evT.Code).Count();
                    if (existingApps > 0)
                    {
                        e.CancelAction = true;
                        e.CancelReason = $"You're not allow to amend in use event type code.";
                        return;
                    }
                }
                foreach (var item in currEventTypes)
                {
                    var existingApps = specialEventHelper.Find(x => x.EventTypeCode == item.Code).Count();
                    if (existingApps > 0)
                    {
                        e.CancelAction = true;
                        e.CancelReason = $"Cannot amend or remove '{item.EventTypeName}'. Found {existingApps} related item(s).";
                        break;
                    }
                }
            }
        }
        private void contentEvents_PublishingContent(object sender, ContentEventArgs e)
        {
            ValidateEventTypes(e);
        }
    }
}