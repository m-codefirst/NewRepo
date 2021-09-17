using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.SpecialEvents;

namespace TRM.Web.Business.DataAccess
{
    public class SpecialEventsRepository : DbContextDisposable<SpecialEventsDbContext>, ISpecialEventsRepository
    {
        public Guid AddAppointment(Appointment appointment)
        {
            appointment.Id = Guid.NewGuid();
            context.Appointments.Add(appointment);
            context.SaveChanges();
            return appointment.Id;
        }

        public Guid? UpdateAppointment(Appointment dataApp)
        {
            var app = context.Appointments.FirstOrDefault(x => x.Id == dataApp.Id);
            if (app != null)
            {
                context.Entry(app).CurrentValues.SetValues(dataApp);
                context.SaveChanges();
                return app.Id;
            }
            return null;
        }


        public Guid AddAppointment(DateTime date, string name, string notes, string eventType, bool repeats)
        {
            var appointment = context.Appointments.Create();
            appointment.Date = date;
            appointment.Id = Guid.NewGuid();
            appointment.Name = name;
            appointment.Notes = notes;
            appointment.RepeatsAnnually = repeats;
            appointment.EventTypeCode = eventType;
            context.Appointments.Attach(appointment);
            context.SaveChanges();
            return appointment.Id;
        }

        public IEnumerable<Appointment> Find(Expression<Func<Appointment, bool>> expression)
        {
            return context.Appointments.Where(expression).OrderBy(x=>x.Date);
        }

        public bool DeleteAppointment(Guid id)
        {
            try
            {
                var existing = context.Appointments.FirstOrDefault(x => x.Id == id);
                if (null == existing) return false;
                context.Appointments.Remove(existing);
                return context.SaveChanges() > 0;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<Appointment> GetUserAppointments(Guid contactId)
        {
            var result = context.Appointments.Where(x => x.ContactId == contactId).OrderBy(x => x.Date);
            return result;
        }

        public Appointment GetAppointment(Guid id)
        {
            var app = context.Appointments.SingleOrDefault(x => x.Id == id);
            return app;
        }      
        public IEnumerable<Appointment> GetAll()
        {
            var result = context.Appointments.Where(x => (!x.RepeatsAnnually && x.Date.CompareTo(DateTime.Now) < 0) || x.RepeatsAnnually).OrderBy(x => x.Date);
            return result;
        }
    }
}