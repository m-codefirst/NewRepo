using System;
using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.GDPR;

namespace TRM.Web.Services
{
    public interface IGdprService
    {
        GdprCustomer GetCustomerByTypeAndId(string id, string type = null);
        Guid? CreateGdprConsent(GdprConsent consent);
        Guid? UpdateBasedOnConfirmEmailParameter(string confirmEmail);
        IEnumerable<GdprConsent> GetAllGdprConsentsNeedToUpdateStatus(int numberOfItem = 50);
        void UpdateForCustomerTypeCBVAndStatusZero(GdprConsent gdprConsent);
        void UpdateForCustomerTypeCAndStatusOne(GdprConsent gdprConsent);
        void UpdateForCustomerTypeOAndStatusOne(GdprConsent gdprConsent);
        void UpdateForCustomerHasStatusFour(GdprConsent gdprConsent);
    }
}
