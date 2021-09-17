using System;
using System.Linq;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.EntityFramework.SovereignCertificates;

namespace TRM.Web.Helpers
{
    public class SovereignCertificateHelper : IAmSovereignCertificateHelper
    {
        public Enums.eSovereignSignUpMessageStatus SaveSoverignSignup(SovereignCertificateSignUpDto signUp)
        {
            using (var dbCert = new SovereignCertificatesContext())
            {
                var certificate = dbCert.Certificates.FirstOrDefault(m => m.Code.ToLower() == signUp.CertificateCode.ToLower());

                if (certificate == null || certificate.Code == null)
                {
                    return Enums.eSovereignSignUpMessageStatus.SovereignCodeDoesntExist;
                }

                var certificateIsRegistered = dbCert.Certificates.FirstOrDefault(m => m.Code.ToLower() == signUp.CertificateCode.ToLower() && m.IsRegistered.Equals(true));

                if (certificateIsRegistered != null && certificateIsRegistered.IsRegistered.Equals(true))
                {
                    return Enums.eSovereignSignUpMessageStatus.SovereignCodeAlreadyBeenRegistered;
                }

                if (dbCert.SovereignCertificateRegistrations.Any(m => m.CertificateCode.ToLower() == signUp.CertificateCode.ToLower()))
                {
                    return Enums.eSovereignSignUpMessageStatus.SovereignCodeAlreadyBeenRegistered;
                }

                DateTime contactPreferenceDateTime = DateTime.Now;
                var certificateSignUp = new SovereignCertificateRegistration
                {
                    RegistrationId = Guid.NewGuid(),
                    EmailAddress = signUp.EmailAddress,
                    DateAdded = DateTime.UtcNow,
                    CertificateId = certificate.CertificateId,
                    CertificateCode = signUp.CertificateCode.ToUpper(),
                    CertificateNumber = signUp.CertificateNumber,
                    FirstName = signUp.FirstName,
                    Surname = signUp.Surname,
                    Telephone = signUp.Telephone,
                    ByEmail = signUp.ByEmail,
                    ByPhone = signUp.ByPhone,
                    EmailConsentDateTime = contactPreferenceDateTime,
                    TelephoneConsentDateTime = contactPreferenceDateTime
                };
                dbCert.SovereignCertificateRegistrations.Add(certificateSignUp);

                certificate.IsRegistered = true;

                dbCert.SaveChanges();

                return Enums.eSovereignSignUpMessageStatus.SuccessfulSignUp;
            }
        }

    }
}
