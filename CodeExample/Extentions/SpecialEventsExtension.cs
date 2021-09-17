using EPiServer;
using System.Linq;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.SpecialEvents;
using TRM.Web.Models.SpecialEvents;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels;
using System;

namespace TRM.Web.Extentions
{
    public static class SpecialEventsExtension
    {
        public static T GetAttributeFrom<T>(this object instance, string propertyName) where T : Attribute
        {
            var attrType = typeof(T);
            var property = instance.GetType().GetProperty(propertyName);
            return (T)property.GetCustomAttributes(attrType, false).First();
        }

        public static IEnumerable<AppointmentViewModel> ToAppointmentViewModel(this IEnumerable<Appointment> appointments)
        {
            foreach (var app in appointments)
            {
                yield return new AppointmentViewModel
                {
                    Id = app.Id,
                    Date = app.Date,
                    EventTypeCode = app.EventTypeCode,
                    EventTypeName = app.GetEventType().Key,
                    Name = app.Name,
                    Notes = app.Notes,
                    IsExpired = System.DateTime.Compare(app.Date, System.DateTime.Now) < 0
                };
            }
        }

        public static AppointmentResult ToResult(this Appointment app, string contactName = null)
        {
            var eventType = app.GetEventType();            
            return new AppointmentResult
            {
                Id = app.Id.ToString(),
                Name = app.Name,
                Date = app.Date.ToString("dd MMMM yyyy"),
                Notes = app.Notes,
                EventTypeCode = app.EventTypeCode,
                EventTypeName = eventType.Key,
                ContactName = contactName,
                ContactId = app.ContactId,
                RepeatsAnnually = app.RepeatsAnnually,
                IsExpired = eventType.Value ? false : System.DateTime.Compare(app.Date, System.DateTime.Now) < 0
            };
        }
        public static KeyValuePair<string, bool> GetEventType(this Appointment app)
        {
            var cache = ServiceLocator.Current.GetInstance<ISynchronizedObjectInstanceCache>();
            var eventTypes = cache.Get<IEnumerable<SpecialEventType>>(Constants.StringConstants.EventTypeCacheKey, ReadStrategy.Immediate);
            if (null == eventTypes)
            {
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
                var startPage = contentLoader.Get<StartPage>(ContentReference.StartPage);

                if (null != startPage.OccasionSettingsPage && startPage.OccasionSettingsPage != ContentReference.EmptyReference)
                {

                    var occasionGlobalSetting = contentLoader.Get<OccasionsSettingsPage>(startPage.OccasionSettingsPage);
                    eventTypes = occasionGlobalSetting.EventTypes;
                    cache.Insert(Constants.StringConstants.EventTypeCacheKey, eventTypes, new CacheEvictionPolicy(System.TimeSpan.FromMinutes(15), CacheTimeoutType.Absolute));
                }
            }
            if (eventTypes == null) return new KeyValuePair<string, bool>();
            var eventType = eventTypes.FirstOrDefault(x => x.Code == app.EventTypeCode);
            if (eventType == null) return new KeyValuePair<string, bool>();

            return new KeyValuePair<string, bool>(eventType.EventTypeName, eventType.RepeatsAnnually);
        }
    }

}