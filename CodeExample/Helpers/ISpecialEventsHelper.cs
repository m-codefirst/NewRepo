using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TRM.Web.Models.EntityFramework.SpecialEvents;
using TRM.Web.Models.Pages;
using TRM.Web.Models.SpecialEvents;

namespace TRM.Web.Helpers
{
    public interface IAmSpecialEventsHelper
    {
        Guid AddAppointment(DateTime date, string name, string typeName, string notes, bool isRepeatsAnnually);
        Guid AddAppointment(Appointment appointment);
        IEnumerable<AppointmentResult> GetAppointments(CustomerContact contact);
        IEnumerable<AppointmentResult> GetAppointments(Guid contactId);
        IEnumerable<AppointmentResult> GetAllAppointment();
        OccasionsSettingsPage GetSettingPage();
        AppointmentResult GetAppointment(Guid id);
        IEnumerable<AppointmentResult> Find(Expression<Func<Appointment, bool>> where, bool includeContactName = false);
        bool DeleteAppointment(Guid appointmentId);
        Guid? UpdateAppointment(Appointment data);
        IEnumerable<SpecialEventType> GetSpecialEventTypes();
        bool SendEmail(AppointmentResult appointmentResult);
    }
}