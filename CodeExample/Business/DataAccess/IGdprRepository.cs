using System;
using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.GDPR;

namespace TRM.Web.Business.DataAccess
{
    public interface IGdprRepository
    {
        GdprCustomer GetCustomerByTypeAndId(string id, string type = null);
        Guid AddGdprConsent(GdprConsent consent);
        Guid? UpdateBasedOnConfirmEmailParameter(Guid confirmEmailGuid);
        IEnumerable<GdprConsent> GetAllGdprConsentsNeedToUpdateStatus(int numberOfItem);
        Guid? UpdateStatusGdprConsent(GdprConsent consent, bool needToUpdateConfirmEmail = false);
    }
}
