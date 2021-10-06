using EPiServer;
using EPiServer.Web;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Models.EntityFramework.GDPR;
using TRM.Web.Models.Pages;

namespace TRM.Web.Services
{
    public class GdprService : IGdprService
    {
        private readonly IGdprRepository _gdprRepository;
        private readonly IContentLoader _contentLoader;
        private readonly IEmailHelper _emailHelper;
        public GdprService(IGdprRepository gdprRepository, IContentLoader contentLoader, IEmailHelper emailHelper)
        {
            _gdprRepository = gdprRepository;
            _contentLoader = contentLoader;
            _emailHelper = emailHelper;
        }

        public GdprCustomer GetCustomerByTypeAndId(string id, string type = null)
        {
            return _gdprRepository.GetCustomerByTypeAndId(id, type);
        }

        public Guid? CreateGdprConsent(GdprConsent consent)
        {
            if (consent == null) return null;

            consent.Id = Guid.NewGuid();
            consent.Created = DateTime.Now;
            return _gdprRepository.AddGdprConsent(consent);
        }

        public Guid? UpdateBasedOnConfirmEmailParameter(string confirmEmail)
        {
            Guid confirmEmailGuid;
            return Guid.TryParse(confirmEmail, out confirmEmailGuid) ?
                _gdprRepository.UpdateBasedOnConfirmEmailParameter(confirmEmailGuid) : null;
        }

        public IEnumerable<GdprConsent> GetAllGdprConsentsNeedToUpdateStatus(int numberOfItem = 50)
        {
            return _gdprRepository.GetAllGdprConsentsNeedToUpdateStatus(numberOfItem);
        }

        //Customer Type in (“C”,”B”,”V”) and Status = 0:
        public void UpdateForCustomerTypeCBVAndStatusZero(GdprConsent gdprConsent)
        {
            var gdprConsentPage = GetCurrentGdprConsentPage();
            if (gdprConsentPage != null && gdprConsentPage.ConsentConfirmedEmailTemplatePage != null)
            {
                var confirmEmailPage = _contentLoader.Get<TRMEmailPage>(gdprConsentPage.ConsentConfirmedEmailTemplatePage);
                if(confirmEmailPage != null) _emailHelper.SendGdprConfirmEmail(confirmEmailPage, gdprConsent.EmailAddress, $"{gdprConsent.Title} {gdprConsent.LastName}");
            }

            gdprConsent.Status = (int)Constants.Enums.GdprStatus.EmailSent;
            _gdprRepository.UpdateStatusGdprConsent(gdprConsent);
        }

        //Customer Type = “C” and Status = 1
        public void UpdateForCustomerTypeCAndStatusOne(GdprConsent gdprConsent)
        {
            var customerContact = string.IsNullOrEmpty(gdprConsent.ObsCustomerNumber) ? null : FindContactByObsCustomerNumberAndLastname(gdprConsent.ObsCustomerNumber);
            if (customerContact != null)
            {
                UpdateGdprFieldsForCustomerContact(customerContact, gdprConsent);
                gdprConsent.Status = (int)Constants.Enums.GdprStatus.EpiserverUpdated;
            }
            else
            {
                gdprConsent.Status = (int)Constants.Enums.GdprStatus.NotFoundInEpiserver;
            }
            _gdprRepository.UpdateStatusGdprConsent(gdprConsent);
        }

        //Customer Type = “O” and Status = 1
        public void UpdateForCustomerTypeOAndStatusOne(GdprConsent gdprConsent)
        {
            var customerContact = string.IsNullOrEmpty(gdprConsent.ObsCustomerNumber) ? null : FindContactByObsCustomerNumberAndLastname(gdprConsent.ObsCustomerNumber, gdprConsent.LastName);
            if (customerContact != null)
            {
                UpdateGdprFieldsForCustomerContact(customerContact, gdprConsent);
                gdprConsent.Status = (int)Constants.Enums.GdprStatus.EpiserverUpdated;
            }
            else
            {
                gdprConsent.Status = (int)Constants.Enums.GdprStatus.NotFoundInEpiserver;
            }
            _gdprRepository.UpdateStatusGdprConsent(gdprConsent);
        }

        //Status = 4
        public void UpdateForCustomerHasStatusFour(GdprConsent gdprConsent)
        {
            var confirmConsentGuid = Guid.NewGuid();
            var gdprConsentPage = GetCurrentGdprConsentPage();
            if (gdprConsentPage != null && gdprConsentPage.IdentityConfirmationEmailTemplatePage != null)
            {
                var identityEmailPage = _contentLoader.Get<TRMEmailPage>(gdprConsentPage.IdentityConfirmationEmailTemplatePage);
                if (identityEmailPage != null)
                {
                    var absoluteGdprPageUrl = $"{gdprConsentPage.ContentLink.GetExternalUrl_V2()}";
                    var pageUrl = $"{absoluteGdprPageUrl}?ConfirmConsent={confirmConsentGuid}";

                    _emailHelper.SendGdprIdentityEmail(identityEmailPage, gdprConsent.EmailAddress, $"{gdprConsent.Title} {gdprConsent.LastName}", pageUrl);
                }
            }

            gdprConsent.Status = (int)Constants.Enums.GdprStatus.ConfirmEmailAddressEmailSent;
            gdprConsent.ConfirmEmailGuid = confirmConsentGuid;
            _gdprRepository.UpdateStatusGdprConsent(gdprConsent, true);
        }

        private GdprConsentPage GetCurrentGdprConsentPage()
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if(startPage == null || startPage.GdprConsentPage == null) 
                throw new ArgumentException("Must setting GdprConsent page in site settings");
            return _contentLoader.Get<GdprConsentPage>(startPage.GdprConsentPage);
        }

        private CustomerContact FindContactByObsCustomerNumberAndLastname(string obsCustomerNumber, string lastname = null)
        {
            var filters = new List<FilterElement>();
            var obsAccountNumberFilter = new FilterElement(StringConstants.CustomFields.ObsAccountNumber, FilterElementType.Equal, obsCustomerNumber);
            filters.Add(obsAccountNumberFilter);

            var customerContacts = BusinessManager.List(StringConstants.CustomFields.ContactClassName, filters.ToArray()).Cast<CustomerContact>();

            if (string.IsNullOrEmpty(lastname)) return customerContacts.FirstOrDefault();

            return customerContacts.FirstOrDefault(customerContact => customerContact.LastName != null &&
            customerContact.LastName.Equals(lastname, StringComparison.InvariantCultureIgnoreCase));
        }

        private void UpdateGdprFieldsForCustomerContact(CustomerContact matchCustomerContact, GdprConsent gdprConsent)
        {
            matchCustomerContact.Properties[StringConstants.CustomFields.ContactByPhone].Value = gdprConsent.CanPhone ?? false;
            matchCustomerContact.Properties[StringConstants.CustomFields.ContactByEmail].Value = gdprConsent.CanEmail;
            matchCustomerContact.Properties[StringConstants.CustomFields.ContactByPost].Value = gdprConsent.CanPost ?? false;

            matchCustomerContact.Properties[StringConstants.CustomFields.TelephoneConsentDateTime].Value = gdprConsent.PhonePreference;
            matchCustomerContact.Properties[StringConstants.CustomFields.EmailConsentDateTime].Value = gdprConsent.EmailPreference;
            matchCustomerContact.Properties[StringConstants.CustomFields.PostConsentDateTime].Value = gdprConsent.PostPreference;

            matchCustomerContact.Properties[StringConstants.CustomFields.TelephoneConsentSource].Value = gdprConsent.Purpose;
            matchCustomerContact.Properties[StringConstants.CustomFields.EmailConsentSource].Value = gdprConsent.Purpose;
            matchCustomerContact.Properties[StringConstants.CustomFields.PostConsentSource].Value = gdprConsent.Purpose;

            matchCustomerContact.SaveChanges();
        }
    }
}