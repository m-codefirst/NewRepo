using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.Shared.Facades;
using Mediachase.Commerce.Customers;
using TRM.IntegrationServices.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Services.CustomersExportTransaction;
using static TRM.IntegrationServices.Constants.StringConstants;
using static TRM.Web.Models.DTOs.Bullion.AxImportData;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Services.DataImportCustomerService
{
    [ServiceConfiguration(typeof(IDataImportCustomerService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DataImportCustomerService : IDataImportCustomerService
    {
        private readonly IUserService _userService;
        private readonly IBullionUserService _bullionUserService;
        private readonly ICustomersExportTransactionService _bullionCustomerExport;
        private readonly CountryManagerFacade _countryManager;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IAmBullionContactHelper _iAmBullionContactHelper;

        public DataImportCustomerService(
            IUserService userService,
            IBullionUserService bullionUserService,
            ICustomersExportTransactionService bullionCustomerExport,
            CountryManagerFacade countryManager, IAmBullionContactHelper bullionContactHelper, IAmBullionContactHelper iAmBullionContactHelper)
        {
            _userService = userService;
            _bullionUserService = bullionUserService;
            _bullionCustomerExport = bullionCustomerExport;
            _countryManager = countryManager;
            _bullionContactHelper = bullionContactHelper;
            _iAmBullionContactHelper = iAmBullionContactHelper;
        }
        public bool CreateCustomerFromAx(CustomerRequest customer)
        {
            var user = GenerateApplicationUser(customer, null);
            var createdUser = _userService.RegisterAccount(user, false);
            return createdUser?.Contact != null;
        }

        public bool UpdateCustomerFromAx(CustomerRequest axCustomer, CustomerContact customerToUpdate)
        {
            var isBullionUser = axCustomer.AccountNum ==
                                customerToUpdate.GetStringProperty(StringConstants.CustomFields
                                    .BullionObsAccountNumber);
            var user = GenerateApplicationUser(axCustomer, customerToUpdate);

            return UpdateCustomerContactIfRequired(customerToUpdate, user, isBullionUser);
        }
        public bool UpdateContactFromAx(ObsCustomerContactRequest contactRequest, CustomerContact customerToUpdate)
        {
            var isBullionUser = contactRequest.ObsAccountNumber ==
                                customerToUpdate.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber);
            var user = GenerateApplicationUser(contactRequest);
            return UpdateCustomerContactIfRequired(customerToUpdate, user, isBullionUser);
        }

        private bool UpdateCustomerContactIfRequired(CustomerContact customerToUpdate, ApplicationUser user, bool isBullionUser)
        {
            var needToUpdate = false;

            if (customerToUpdate.FirstName != user.FirstName && user.FirstName != null)
            {
                customerToUpdate.FirstName = user.FirstName;
                needToUpdate = true;
            }
            if (customerToUpdate.MiddleName != user.MiddleName && user.MiddleName != null)
            {
                needToUpdate = true;
                customerToUpdate.MiddleName = user.MiddleName;
            }
            if (customerToUpdate.LastName != user.LastName && user.LastName != null)
            {
                needToUpdate = true;
                customerToUpdate.LastName = user.LastName;
            }
            if (customerToUpdate.Email != user.Email && user.Email != null)
            {
                needToUpdate = true;
                customerToUpdate.Email = user.Email;
            }

            if (customerToUpdate.PreferredCurrency != user.Currency && user.Currency != null)
            {
                needToUpdate = true;
                customerToUpdate.PreferredCurrency = user.Currency;
            }
            if (customerToUpdate.BirthDate != user.BirthDate && user.BirthDate != null)
            {
                needToUpdate = true;
                customerToUpdate.BirthDate = user.BirthDate;
            }

            //update for consumer
            needToUpdate = _userService.UpdateCustomerCustomProperties(user, customerToUpdate) || needToUpdate;
            //update for bullion
            if (isBullionUser)
            {
                needToUpdate = _bullionUserService.UpdateBullionCustomerCustomProperties(user, customerToUpdate) || needToUpdate;
            }

            if (needToUpdate) customerToUpdate.SaveChanges();

            if (!needToUpdate) return true;

            //only create Update user export transaction in case account number matches an OBSAccountNumber
            if (isBullionUser == false && _bullionContactHelper.IsBullionAccount(customerToUpdate))
            {
                _bullionCustomerExport.SaveBullionCustomerExportTransaction(customerToUpdate,
                    ExportTransactionType.UpdateBullion);
            }

            return true;
        }

        public bool UpdateCustomerAddressesFromAx(List<Address> axAddresses, CustomerContact contact, bool isBullionUser)
        {
            if (axAddresses == null || !axAddresses.Any()) return false;

            foreach (var axAddress in axAddresses)
            {
                if (axAddress.AccountNum != contact.GetStringProperty(StringConstants.CustomFields.ObsAccountNumber)
                    && axAddress.AccountNum != contact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber)) continue;

                var customerAddress = CustomerAddress.CreateInstance();
                customerAddress.Name = isBullionUser
                    ? StringConstants.PreciousMetalsRegisteredAddressName
                    : StringConstants.DefaultRegisteredAddressName;
                customerAddress.FirstName = contact.FirstName;
                customerAddress.LastName = contact.LastName;
                customerAddress.Email = contact.Email;
                customerAddress.Line1 = axAddress.Street;
                //customerAddress.Line2 = axAddress.Line2;
                customerAddress.City = axAddress.City;
                if (!string.IsNullOrWhiteSpace(axAddress.ZipCode))
                {
                    customerAddress.PostalCode = axAddress.ZipCode;
                }
                if (!string.IsNullOrWhiteSpace(axAddress.CountryRegionId))
                {
                    customerAddress.CountryCode = axAddress.CountryRegionId;
                }
                customerAddress.CountryName = _countryManager.GetCountryByCountryCode(axAddress.CountryRegionId)?.Name;
                customerAddress.RegionCode = axAddress.State;
                customerAddress.State = axAddress.State;
                customerAddress.AddressType = CustomerAddressTypeEnum.Public;

                customerAddress[AddressFieldNames.County] = axAddress.County;
                customerAddress[AddressFieldNames.LocationId] = axAddress.LocationId;

                if (AddOrUpdateCustomerAddress(contact, customerAddress, isBullionUser))
                {
                    contact.SaveChanges();
                }
            }

            return true;
        }

        private ApplicationUser GenerateApplicationUser(CustomerRequest axCustomer, CustomerContact existingCustomer)
        {
            DateTime outDatetime;

            return new ApplicationUser
            {
                //Email = axCustomer.Email,
                //UserName = axCustomer.Email,
                //PhoneNumber = axCustomer.Phone,
                LastName = axCustomer.Name,

                BirthDate = axCustomer.Birthday.TryParseSqlDatetimeExact(out outDatetime) ? outDatetime : (DateTime?)null,
                Password = Guid.NewGuid().ToString(),
                RegistrationSource = StringConstants.RegistrationSource.ImportedFromAx,
                ObsAccountNumber = existingCustomer == null ? (axCustomer.AccountNum) :
                    existingCustomer.GetStringProperty(StringConstants.CustomFields.ObsAccountNumber),
                D2CCustomerType = axCustomer.D2CCustomerType,
                TaxGroup = axCustomer.TaxGroup,
                CreditLimit = axCustomer.CreditMax.ToDecimalExactCulture(),
                InclTax = axCustomer.InclTax.YesNoToBool(),
                StatementPreference = axCustomer.StatementPreference,
                DlvMode = axCustomer.DlvMode,
                Department = axCustomer.Department,
                ClassificationId = axCustomer.CustClassificationId,
                Currency = axCustomer.Currency,

                ByEmail = axCustomer.Email.YesNoToBool(),
                EmailConsentDateTime = axCustomer.EmailConsentDateTime.ToSqlDatetime(),
                EmailConsentSource = axCustomer.EmailConsentSource,

                ByPost = axCustomer.Post.YesNoToBool(),
                PostConsentDateTime = axCustomer.PostConsentDateTime.ToSqlDatetime(),
                PostConsentSource = axCustomer.PostConsentSource,

                ByTelephone = axCustomer.Phone.YesNoToBool(),
                TelephoneConsentDateTime = axCustomer.TelephoneConsentDateTime.ToSqlDatetime(),
                TelephoneConsentSource = axCustomer.TelephoneConsentSource,

                TwoStepAuthenticationAnswer = existingCustomer?.GetStringProperty(StringConstants.CustomFields.TwoStepAuthenticationAnswer),
                TwoStepAuthenticationQuestion = existingCustomer?.GetStringProperty(StringConstants.CustomFields.TwoStepAuthenticationQuestion),

                CustomerType = existingCustomer == null ? (StringConstants.CustomerType.Consumer) :
                    (existingCustomer.GetStringProperty(StringConstants.CustomFields.CustomerType)),

                BullionAccountNumber = existingCustomer == null ? (axCustomer.AccountNum) :
                    existingCustomer.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber),

                BullionUnableToPurchaseBullion = existingCustomer?.GetBooleanProperty(StringConstants.CustomFields.BullionUnableToPurchaseBullion) ?? false,
                BullionUnableToSellorDeliverFromVault = existingCustomer?.GetBooleanProperty(StringConstants.CustomFields.BullionUnableToSellorDeliverFromVault) ?? false,
                BullionUnableToDepositViaCard = existingCustomer?.GetBooleanProperty(StringConstants.CustomFields.BullionUnableToDepositViaCard) ?? false,
                BullionUnableToWithdraw = existingCustomer?.GetBooleanProperty(StringConstants.CustomFields.BullionUnableToWithdraw) ?? false,
                BullionUnableToLogin = existingCustomer?.GetBooleanProperty(StringConstants.CustomFields.BullionUnableToLogin) ?? false
            };
        }

        private ApplicationUser GenerateApplicationUser(ObsCustomerContactRequest obsCustomerContact)
        {
            return new ApplicationUser
            {
                Email = obsCustomerContact.Email,
                UserName = obsCustomerContact.Email,
                PhoneNumber = string.IsNullOrEmpty(obsCustomerContact.Telephone) ? obsCustomerContact.SecondTelephone : obsCustomerContact.Telephone,
                Title = obsCustomerContact.Affix,
                FirstName = obsCustomerContact.FirstName,
                LastName = obsCustomerContact.LastName,
                MiddleName = obsCustomerContact.MiddleName,
                Password = Guid.NewGuid().ToString(),
                RegistrationSource = StringConstants.RegistrationSource.ImportedFromAx,
                Affix = obsCustomerContact.Affix,
                MobilePhone = obsCustomerContact.Mobile
            };
        }

        private bool AddOrUpdateCustomerAddress(CustomerContact contact, CustomerAddress axAddress, bool isBullionUser)
        {
            var existedContactAddress =
                contact.ContactAddresses != null ? isBullionUser
                ? _iAmBullionContactHelper.GetBullionAddress(contact)
                : contact.ContactAddresses.FirstOrDefault(x =>
                    x.Name.Equals(StringConstants.DefaultRegisteredAddressName)) : null;

            if (existedContactAddress != null)
            {
                var needToUpdate = existedContactAddress.FirstName != axAddress.FirstName;
                existedContactAddress.FirstName = axAddress.FirstName;

                needToUpdate = existedContactAddress.LastName != axAddress.LastName || needToUpdate;
                existedContactAddress.LastName = axAddress.LastName;

                needToUpdate = existedContactAddress.Email != axAddress.Email || needToUpdate;
                existedContactAddress.Email = axAddress.Email;

                needToUpdate = existedContactAddress.Line1 != axAddress.Line1 || needToUpdate;
                existedContactAddress.Line1 = axAddress.Line1;

                needToUpdate = existedContactAddress.Line2 != axAddress.Line2 || needToUpdate;
                existedContactAddress.Line2 = axAddress.Line2;

                needToUpdate = existedContactAddress.City != axAddress.City || needToUpdate;
                existedContactAddress.City = axAddress.City;

                if (!string.IsNullOrWhiteSpace(axAddress.PostalCode))
                {
                    needToUpdate = existedContactAddress.PostalCode != axAddress.PostalCode || needToUpdate;
                    existedContactAddress.PostalCode = axAddress.PostalCode;
                }

                if (!string.IsNullOrWhiteSpace(axAddress.CountryCode))
                {
                    needToUpdate = existedContactAddress.CountryCode != axAddress.CountryCode || needToUpdate;
                    existedContactAddress.CountryCode = axAddress.CountryCode;
                }

                needToUpdate = existedContactAddress.CountryName != axAddress.CountryName || needToUpdate;
                existedContactAddress.CountryName = axAddress.CountryName;

                needToUpdate = existedContactAddress.RegionCode != axAddress.RegionCode || needToUpdate;
                existedContactAddress.RegionCode = axAddress.RegionCode;

                needToUpdate = existedContactAddress.RegionName != axAddress.RegionName || needToUpdate;
                existedContactAddress.RegionName = axAddress.RegionName;

                needToUpdate = existedContactAddress.State != axAddress.State || needToUpdate;
                existedContactAddress.State = axAddress.State;

                needToUpdate = existedContactAddress.UpdateProperty(AddressFieldNames.County, axAddress[AddressFieldNames.County].ToString()) || needToUpdate;
                needToUpdate = existedContactAddress.UpdateProperty(AddressFieldNames.LocationId, axAddress[AddressFieldNames.LocationId].ToString()) || needToUpdate;

                //update Address
                contact.UpdateContactAddress(existedContactAddress);
                return needToUpdate;
            }

            contact.AddContactAddress(axAddress);
            return true;
        }
    }
}