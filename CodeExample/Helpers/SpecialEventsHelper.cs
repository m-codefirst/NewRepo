using System;
using System.Linq;
using System.Collections.Generic;
using TRM.Web.Business.DataAccess;
using TRM.Web.Models.EntityFramework.SpecialEvents;
using TRM.Web.Models.SpecialEvents;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using EPiServer;
using EPiServer.Core;
using TRM.Web.Models.Pages;
using TRM.Web.Extentions;
using TRM.Web.Business.Email;
using Mediachase.Commerce.Customers;
using System.Linq.Expressions;

namespace TRM.Web.Helpers
{
    public class SpecialEventsHelper : IAmSpecialEventsHelper
    {
        private readonly ISpecialEventsRepository _specialEventsRepository;
        private readonly IContentLoader _contentLoader;
        private readonly IEmailHelper _emailHelper;
        private readonly CustomerContext _customerContext;
        public SpecialEventsHelper(ISpecialEventsRepository specialEventsRepository, IContentLoader contentLoader, IEmailHelper emailHelper, CustomerContext customerContext)
        {
            _customerContext = customerContext;
            _contentLoader = contentLoader;
            _specialEventsRepository = specialEventsRepository;
            _emailHelper = emailHelper;
        }
        public Guid AddAppointment(DateTime date, string name, string typeName, string notes, bool isRepeatsAnnually)
        {
            return _specialEventsRepository.AddAppointment(date, name, typeName, notes, isRepeatsAnnually);
        }

        public OccasionsSettingsPage GetSettingPage()
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            if (startPage.OccasionSettingsPage == null || startPage.OccasionSettingsPage == ContentReference.EmptyReference)
            {
                return null;
            }
            var occasionGlobalSetting = _contentLoader.Get<OccasionsSettingsPage>(startPage.OccasionSettingsPage);
            return occasionGlobalSetting;
        }

        public Guid AddAppointment(Appointment appointment)
        {
            return _specialEventsRepository.AddAppointment(appointment);
        }

        public bool DeleteAppointment(Guid appointmentId)
        {
            return _specialEventsRepository.DeleteAppointment(appointmentId);
        }

        public IEnumerable<AppointmentResult> GetAppointments(Guid contactId)
        {
            var customer = _customerContext.GetContactById(contactId);
            var contactName = customer?.FullName;
            return _specialEventsRepository.GetUserAppointments(contactId).Select(x => x.ToResult(contactName));
        }

        public AppointmentResult GetAppointment(Guid id)
        {
            return _specialEventsRepository.GetAppointment(id).ToResult();
        }

        public IEnumerable<SpecialEventType> GetSpecialEventTypes()
        {
            var cache = ServiceLocator.Current.GetInstance<ISynchronizedObjectInstanceCache>();
            var eventTypes = cache.Get<IEnumerable<SpecialEventType>>(Constants.StringConstants.EventTypeCacheKey, ReadStrategy.Immediate);
            if (null == eventTypes)
            {
                var occasionGlobalSetting = GetSettingPage();
                if (null != occasionGlobalSetting)
                {
                    eventTypes = occasionGlobalSetting.EventTypes;
                    cache.Insert(Constants.StringConstants.EventTypeCacheKey, eventTypes, new CacheEvictionPolicy(TimeSpan.FromMinutes(15), CacheTimeoutType.Absolute));
                }
                else
                {
                    eventTypes = new List<SpecialEventType>();
                }
            }
            return eventTypes;
        }

        public Guid? UpdateAppointment(Appointment data)
        {
            return _specialEventsRepository.UpdateAppointment(data);
        }

        public IEnumerable<AppointmentResult> GetAllAppointment()
        {
            return _specialEventsRepository.GetAll().Select(x => x.ToResult());
        }

        public IEnumerable<AppointmentResult> GetAppointments(CustomerContact contact)
        {
            return _specialEventsRepository.GetUserAppointments(contact.PrimaryKeyId.Value).Select(x => x.ToResult(contact.FullName));
        }

        public bool SendEmail(AppointmentResult appointmentResult)
        {
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            if (null == startPage) return false;
            var occasionsSettingsPage = _contentLoader.Get<OccasionsSettingsPage>(startPage.OccasionSettingsPage);
            if (null == occasionsSettingsPage) return false;
            var eventType = occasionsSettingsPage.EventTypes.FirstOrDefault(x => x.Code == appointmentResult.EventTypeCode);

            var emailTemplateRef = ContentReference.EmptyReference;
            if (eventType != null)
                emailTemplateRef = eventType.EmailTemplatePage;

            var emailTemplate = _contentLoader.Get<TRMEmailPage>((emailTemplateRef == ContentReference.EmptyReference || emailTemplateRef == null) ? occasionsSettingsPage.EmailTemplatePage : emailTemplateRef);
            var customerContact = _customerContext.GetContactById(appointmentResult.ContactId);
            if (customerContact == null)
                return false;

            return _emailHelper.SendSpecialEventEmail(emailTemplate, customerContact.Email, appointmentResult);
        }

        public IEnumerable<AppointmentResult> Find(Expression<Func<Appointment, bool>> where, bool includeContactName = false)
        {
            return _specialEventsRepository.Find(where).Select(x => x.ToResult(includeContactName ? CustomerContext.Current.GetContactById(x.ContactId)?.FullName : string.Empty));
        }
    }
}