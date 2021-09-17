using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Web.Constants;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.GDPR;

namespace TRM.Web.Business.DataAccess
{
    public class GdprRepository : DbContextDisposable<GdprDbContext>, IGdprRepository
    {
        public GdprCustomer GetCustomerByTypeAndId(string id, string type = null)
        {
            return context.GdprCustomers.FirstOrDefault(x => x.Id.Equals(id) &&
                (string.IsNullOrEmpty(type) || x.CustomerType.Equals(type, StringComparison.InvariantCultureIgnoreCase)));
        }

        public Guid AddGdprConsent(GdprConsent consent)
        {
            context.GdprConsents.Add(consent);
            context.SaveChanges();
            return consent.Id;
        }

        public Guid? UpdateStatusGdprConsent(GdprConsent consent, bool needToUpdateConfirmEmail = false)
        {
            var gdprConsent = context.GdprConsents.FirstOrDefault(x => x.Id == consent.Id);
            if (gdprConsent != null)
            {
                gdprConsent.Status = consent.Status;
                if (needToUpdateConfirmEmail)
                {
                    gdprConsent.ConfirmEmailAddressEmailSent = DateTime.Now;
                    gdprConsent.ConfirmEmailGuid = consent.ConfirmEmailGuid;
                }
                context.SaveChanges();
                return gdprConsent.Id;
            }
            return null;
        }

        public Guid? UpdateBasedOnConfirmEmailParameter(Guid confirmEmailGuid)
        {
            var gdprConsent = context.GdprConsents.FirstOrDefault(x => x.ConfirmEmailGuid == confirmEmailGuid);
            if (gdprConsent != null)
            {
                var currentDate = DateTime.Now;
                gdprConsent.Status = (int)Enums.GdprStatus.NotProcessed;
                gdprConsent.EmailPreference = currentDate;
                gdprConsent.Created = currentDate;
                context.SaveChanges();
                return gdprConsent.Id;
            }
            return null;
        }

        public IEnumerable<GdprConsent> GetAllGdprConsentsNeedToUpdateStatus(int numberOfItem)
        {
            return context.GdprConsents.Where(c =>
                ((c.CustomerType.Equals(StringConstants.GdprCustomerType.C) ||
                  c.CustomerType.Equals(StringConstants.GdprCustomerType.B) ||
                  c.CustomerType.Equals(StringConstants.GdprCustomerType.V)) &&
                 c.Status == (int) Enums.GdprStatus.NotProcessed) ||
                (c.CustomerType.Equals(StringConstants.GdprCustomerType.C) &&
                 c.Status == (int) Enums.GdprStatus.EmailSent) ||
                (c.CustomerType.Equals(StringConstants.GdprCustomerType.O) &&
                 c.Status == (int) Enums.GdprStatus.EmailSent) ||
                (c.Status == (int) Enums.GdprStatus.ConfirmEmailAddressRequired)).Take(numberOfItem);
        }

    }
}