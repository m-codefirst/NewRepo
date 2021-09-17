using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TRM.Web.Models.EntityFramework.SpecialEvents;

namespace TRM.Web.Business.DataAccess
{
    public interface ISpecialEventsRepository
    {
        Guid AddAppointment(Appointment appointment);
        Guid AddAppointment(DateTime date, string name, string eventType, string notes, bool repeats);
        bool DeleteAppointment(Guid id);
        IEnumerable<Appointment> Find(Expression<Func<Appointment, bool>> where);
        Guid? UpdateAppointment(Appointment dataApp);
        Appointment GetAppointment(Guid id);
        IEnumerable<Appointment> GetUserAppointments(Guid contactId);
        IEnumerable<Appointment> GetAll();
    }
}
