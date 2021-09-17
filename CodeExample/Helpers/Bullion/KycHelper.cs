using EPiServer;
using EPiServer.Logging.Compatibility;
using EPiServer.Web;
using kyc;
using kyc.Dtos;
using kyc.ID3GlobalService;
using Mediachase.Commerce.Customers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Shared.Services;
using TRM.Web.Business.Email;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Bullion;
using TRM.Web.Models.Pages;
using static TRM.Web.Constants.StringConstants;
using Enums = TRM.Web.Constants.Enums;

namespace TRM.Web.Helpers.Bullion
{
    public class KycHelper : IKycHelper
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(KycHelper));
        private readonly IKycPersonQuery _kycPersonQuery;
        private readonly IKycDocumentQuery _kycDocumentQuery;
        private readonly IContentLoader _contentLoader;
        private readonly IAmCountryHelper _countryHelper;
        private readonly IBaseUserService _userService;
        private readonly IEmailHelper _emailHelper;
        private readonly CustomerContext _customerContext;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;
        private readonly IAmOrderHelper _orderHelper;

        public enum KycDocumentType
        {
            Passport,
            DrivingLicense,
            IdentityCard
        }

        public KycHelper(
            IKycPersonQuery kycPersonQuery,
            IContentLoader contentLoader,
            IAmCountryHelper countryHelper,
            IKycDocumentQuery kycDocumentQuery,
            IBaseUserService userService,
            IEmailHelper emailHelper,
            CustomerContext customerContext,
            IAmTransactionHistoryHelper transactionHistoryHelper,
            IAmOrderHelper orderHelper)
        {
            _kycPersonQuery = kycPersonQuery;
            _contentLoader = contentLoader;
            _countryHelper = countryHelper;
            _kycDocumentQuery = kycDocumentQuery;
            _userService = userService;
            _emailHelper = emailHelper;
            _customerContext = customerContext;
            _transactionHistoryHelper = transactionHistoryHelper;
            _orderHelper = orderHelper;
        }
        public KycQueryResultDto PerformKycOnCustomer(CustomerKycApplication customer, AddressKycApplication address)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);

            if (!customer.BirthDate.HasValue || address == null || string.IsNullOrWhiteSpace(startPage?.KycApiUsername))
            {
                return new KycQueryResultDto
                {
                    Id3Response = string.Empty,
                    Status = AccountKycStatus.Submitted
                };
            }

            var kycQueryReference = $"TRM-{DateTime.Now:yyyyMMddhhmmssfff}";

            var queryData = _kycPersonQuery.RetrieveKycPersonData(new KycQueryDto
            {
                AuthenticationDetails = new AuthenticationDetailsDto
                {
                    ProfileId = GetKycProfileId(address.CountryCode),
                    Password = startPage.KycApiPassword,
                    Username = startPage.KycApiUsername,
                    QueryReference = kycQueryReference,
                    CountryCode = address.CountryCode
                },
                AddressDetails = new AddressDetailsDto
                {
                    AddressLine1 = address.Line1,
                    AddressLine2 = address.City,
                    AddressLine3 = address.PostalCode,
                    Country = address.CountryCode
                },
                PersonalDetails = new PersonalDetailsDto
                {
                    Forename = customer.FirstName,
                    Surname = customer.LastName,
                    Gender = customer.Gender,
                    MiddleName = customer.MiddleName,
                    DOB = customer.BirthDate.Value,
                    Title = customer.Title
                },
                OtherDetails = new OtherDetailsDto
                {
                    Email = customer.Email,
                    Mobile = customer.MobilePhone,
                    PhoneNumber = customer.PhoneNumber
                }
            });

            return GetKycStatusFromId3Response(queryData);
        }
        public KycQueryResultDto PerformKycOnUserDetails(ApplicationUser userDetails)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var address = userDetails.Addresses.FirstOrDefault();

            if (!userDetails.BirthDate.HasValue || address == null || string.IsNullOrWhiteSpace(startPage?.KycApiUsername))
            {
                return new KycQueryResultDto
                {
                    Id3Response = string.Empty,
                    Status = AccountKycStatus.Submitted
                };
            }

            var kycQueryReference = $"TRM-{DateTime.Now:yyyyMMddhhmmssfff}";

            var queryData = _kycPersonQuery.RetrieveKycPersonData(new KycQueryDto
            {
                AuthenticationDetails = new AuthenticationDetailsDto
                {
                    ProfileId = GetKycProfileId(address.CountryCode),
                    Password = startPage.KycApiPassword,
                    Username = startPage.KycApiUsername,
                    QueryReference = kycQueryReference,
                    CountryCode = address.CountryCode
                },
                AddressDetails = new AddressDetailsDto
                {
                    AddressLine1 = address.Line1,
                    AddressLine2 = address.City,
                    AddressLine3 = address.PostalCode,
                    Country = address.CountryCode
                },
                PersonalDetails = new PersonalDetailsDto
                {
                    Forename = userDetails.FirstName,
                    Surname = userDetails.LastName,
                    Gender = userDetails.Gender,
                    MiddleName = userDetails.MiddleName,
                    DOB = userDetails.BirthDate.Value,
                    Title = userDetails.Title
                },
                OtherDetails = new OtherDetailsDto
                {
                    Email = userDetails.Email,
                    Mobile = userDetails.MobilePhone,
                    PhoneNumber = userDetails.PhoneNumber
                }
            });

            return GetKycStatusFromId3Response(queryData);
        }

        public KycQueryResultDto PerformKycOnDocumentImage(byte[] documentImage, string countryCode, string kycQueryReference, Guid authenticationId)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);

            if (string.IsNullOrWhiteSpace(startPage?.KycApiUsername))
            {
                return new KycQueryResultDto
                {
                    Id3Response = string.Empty,
                    Status = AccountKycStatus.Submitted
                };
            }

            var kycDocumentDto = new KycDocumentDto
            {
                AuthenticationDetails = new AuthenticationDetailsDto
                {
                    ProfileId = GetKycProfileId(countryCode),
                    Password = startPage.KycApiPassword,
                    Username = startPage.KycApiUsername,
                    QueryReference = kycQueryReference,
                    CountryCode = countryCode,
                    AuthenticationId = authenticationId
                },
                DocumentDetails = new IdentityDocument
                {
                    DocumentImageToUpload = documentImage
                }
            };

            var documentImageData = _kycDocumentQuery.CheckDocumentFromImage(kycDocumentDto);
            return GetKycStatusFromId3Response(documentImageData);
        }

        public KycQueryResultDto PerformKycOnDocument(string documentNumber, DateTime? expiryDate, string countryCode, string kycQueryReference, Guid authenticationId, KycDocumentType documentType)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);

            if (string.IsNullOrWhiteSpace(startPage?.KycApiUsername))
            {
                return new KycQueryResultDto
                {
                    Id3Response = string.Empty,
                    Status = AccountKycStatus.Submitted
                };
            }

            var document = new DocumentDetail
            {
                DocumentNumber = documentNumber,
                DocumentExpiryDate = expiryDate,
                Country = countryCode
            };

            var idDocument = new IdentityDocument();

            switch (documentType)
            {
                case KycDocumentType.Passport:
                    idDocument.Passport = document;
                    break;
                case KycDocumentType.DrivingLicense:
                    idDocument.DrivingLicense = document;
                    break;
                case KycDocumentType.IdentityCard:
                    idDocument.IdCard = document;
                    break;
            }

            var kycDocumentDto = new KycDocumentDto
            {
                AuthenticationDetails = new AuthenticationDetailsDto
                {
                    AuthenticationId = authenticationId,
                    ProfileId = GetKycProfileId(countryCode),
                    Password = startPage.KycApiPassword,
                    Username = startPage.KycApiUsername,
                    QueryReference = kycQueryReference,
                    CountryCode = countryCode
                },
                DocumentDetails = idDocument
            };

            var queryData = _kycDocumentQuery.CheckDocument(kycDocumentDto);
            return GetKycStatusFromId3Response(queryData, true);
        }

        /// <summary>
        /// Get kyc status from KycId3Response
        /// </summary>
        /// <param name="queryData"></param>
        /// <param name="initialCheckStep1"></param>
        /// <returns></returns>
        private KycQueryResultDto GetKycStatusFromId3Response(GlobalResultData queryData, bool initialCheckStep1 = false)
        {
            //If we make a KYC request to GBG's API and don't receive a response, or timeout, 
            // or receive an error we should set the status to REJECTED. BULL-1058

            var status = AccountKycStatus.Rejected;
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);

            if (startPage != null && queryData != null)
            {
                if (!string.IsNullOrWhiteSpace(startPage.KycBandTextPass) &&
                    queryData.BandText.IndexOf(startPage.KycBandTextPass, StringComparison.InvariantCultureIgnoreCase) > -1)
                {
                    status = AccountKycStatus.Approved;
                }
                else if (!string.IsNullOrWhiteSpace(startPage.KycBandTextAlert) &&
                         queryData.BandText.IndexOf(startPage.KycBandTextAlert, StringComparison.InvariantCultureIgnoreCase) > -1)
                {
                    if (initialCheckStep1)
                    {
                        status = startPage.TreatInitialAlertAsRefer ? AccountKycStatus.PendingAdditionalInformation : AccountKycStatus.ReadyForReview;
                    }
                    else { status = AccountKycStatus.ReadyForReview; }
                }
                else if (!string.IsNullOrWhiteSpace(startPage.KycBandTextRefer) &&
                        queryData.BandText.IndexOf(startPage.KycBandTextRefer, StringComparison.InvariantCultureIgnoreCase) > -1)
                {
                    status = initialCheckStep1 ? AccountKycStatus.PendingAdditionalInformation : AccountKycStatus.ReadyForReview;
                }
            }

            return new KycQueryResultDto
            {
                Status = status,
                Id3Response = JsonConvert.SerializeObject(queryData)
            };
        }

        private Guid GetKycProfileId(string countryCode)
        {
            var country = _countryHelper.GetCountries().FirstOrDefault(x => x.CountryCode == countryCode);

            if (country != null && country.KYCProfileId != Guid.Empty) return country.KYCProfileId;

            return new Guid("e6cd5db0-317a-4336-ae0d-4ef1086a6115");
        }

        public bool KycCanProceed(CustomerContact customer)//, AccountKycStatus miniumumKycLevelForThisOperation = AccountKycStatus.Approved)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null) return false;

            if (startPage.OverrideKycCheckForBullionAccountActivity) return true;

            var customerKycStatus = GetCustomerKycStatus(customer);
            if (customerKycStatus == AccountKycStatus.Approved) return true;

            if (customerKycStatus != AccountKycStatus.NeedReCheck)
            {
                return false;
            }

            ResetCustomerKycStatus(customer);
            customerKycStatus = GetCustomerKycStatus(customer);

            return customerKycStatus == AccountKycStatus.Approved;
        }

        public AccountKycStatus GetCustomerKycStatus(CustomerContact customer)
        {
            AccountKycStatus customerKycStatus;
            Enum.TryParse(customer.GetStringProperty(StringConstants.CustomFields.KycStatus), out customerKycStatus);
            return customerKycStatus == 0 ? AccountKycStatus.Approved : customerKycStatus;
        }

        public DateTime? GetCustomerKycDate(CustomerContact customer)
        {
            return (DateTime?)customer[StringConstants.CustomFields.KycDate];
        }
        
        private void ResetCustomerKycStatus(CustomerContact customer)
        {
            var applicationUser = _userService.GetUser(customer.Email);
            if (string.IsNullOrWhiteSpace(applicationUser.Email)) return;

            var newStatus = PerformKycOnUserDetails(applicationUser);

            customer.Properties[StringConstants.CustomFields.BullionKycApiResponse].Value = newStatus.Id3Response;
            customer.Properties[StringConstants.CustomFields.KycStatus].Value = (int)newStatus.Status;
            customer.Properties[StringConstants.CustomFields.KycDate].Value = DateTime.Now.Date;
            customer.SaveChanges();
        }

        public bool SendKycResultCheckingEmail(MailedUserInformationDto bullionUser, AccountKycStatus kycCheckStatus)
        {
            if (string.IsNullOrEmpty(bullionUser?.Email))
            {
                _logger.Error("The [ToAddress] cannot be null.");
                return false;
            }
            var bullionEmailCategory = GetEmailCategory(kycCheckStatus);
            var emailParams = GetEmailParamsByKycCheckingStatus(kycCheckStatus, bullionUser);
            string sendingEmailErrorMessage;
            var toMailAddress = new MailAddress(bullionUser.Email, bullionUser.FullName);

            if (_emailHelper.SendBullionEmail(bullionEmailCategory, new List<MailAddress>() { toMailAddress }, out sendingEmailErrorMessage, emailParams))
            {
                return true;
            }
            _logger.ErrorFormat("Error when trying to send the email: {0}", sendingEmailErrorMessage);
            return false;
        }

        public void ValidateKycResponse(out string kycQueryReference, out Guid authenticationId)
        {
            var customer = _customerContext.CurrentContact;

            authenticationId = Guid.NewGuid();
            var kycResponse = customer.GetStringProperty(StringConstants.CustomFields.BullionKycApiResponse);
            kycQueryReference = $"TRM-{DateTime.Now:yyyyMMddhhmmssfff}";

            if (string.IsNullOrEmpty(kycResponse)) return;

            var globalResultData = JsonConvert.DeserializeObject<GlobalResultData>(kycResponse);
            if (globalResultData == null) return;
            kycQueryReference = globalResultData.CustomerRef;
            authenticationId = globalResultData.AuthenticationID;
        }

        private Dictionary<string, object> GetEmailParamsByKycCheckingStatus(AccountKycStatus kycCheckStatus, MailedUserInformationDto bullionUser)
        {
            var result = new Dictionary<string, object>
            {
                { EmailParameters.Title, bullionUser.Title },
                { EmailParameters.FirstName, bullionUser.FirstName },
                { EmailParameters.LastName, bullionUser.LastName }
            };
            return result;
        }

        private string GetEmailCategory(AccountKycStatus kycCheckStatus)
        {
            if (kycCheckStatus == AccountKycStatus.Approved)
            {
                return BullionEmailCategories.ApplicationApprovedEmail;
            }
            if (kycCheckStatus == AccountKycStatus.Rejected || kycCheckStatus == AccountKycStatus.ReadyForReview)
            {
                return BullionEmailCategories.ApplicationDeclinedEmail;
            }
            if (kycCheckStatus == AccountKycStatus.PendingAdditionalInformation)
            {
                return BullionEmailCategories.DocumentsRequiredEmail;
            }
            return string.Empty;
        }

        public decimal GetCustomerTotalSpend(int recheckMonths)
        {
            return GetCustomerTotalSpend(recheckMonths, _customerContext.CurrentContact);
        }
        public decimal GetCustomerTotalSpend(int recheckMonths, CustomerContact contact)
        {
            decimal totalSpend = 0;
            var dateCutoff = DateTime.Now.AddMonths(-recheckMonths);

            //ToDo: Optimise this query
            totalSpend += _transactionHistoryHelper.GetTransactionHistoricalSpend(contact.PrimaryKeyId.ToString(), dateCutoff);

            //ToDo: Optimise this query
            totalSpend += _orderHelper.GetPurchaseOrderHistoricalSpend(contact.UserId, dateCutoff);

            return totalSpend;
        }
    }
}