using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;

namespace TRM.Shared.Services
{
    [ServiceConfiguration(typeof(IBaseUserService), Lifecycle = ServiceInstanceScope.Transient)]
    public class BaseUserService : IBaseUserService
    {
        private readonly CustomerContext _customerContext;
        private readonly IUserImpersonation _userImpersonation;

        private readonly string[] _defaultUserRoles =
        {
            AppRoles.RegisteredRole, AppRoles.EveryoneRole
        };

        public BaseUserService(
            CustomerContext customerContext,
            IUserImpersonation userImpersonation)
        {
            _customerContext = customerContext;
            _userImpersonation = userImpersonation;
        }

        public virtual bool IsEmailAvailable(string email)
        {
            var userName = Membership.GetUserNameByEmail(email.ToLower());

            return string.IsNullOrEmpty(userName);
        }

        public virtual ContactIdentityResult RegisterAccount(ApplicationUser model, bool isBullionAccount = false)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.ObsAccountNumber))
            {
                return new ContactIdentityResult(new IdentityResult
                {
                    Status = Enums.eCustomerStatus.Error,
                    Messages = new List<string>
                        {
                            "No Email and no Account Number provided. At least one is required for account creation"
                        }
                }, null);
            }

            var username = model.UserName;

            model.Email = model.Email?.ToLower();

            if (string.IsNullOrEmpty(model.UserName) && !isBullionAccount)
            {
                username = GenerateUsername(model);
            }

            //if a password wasn't specified we still need one to register an account
            //we'll set it to a random guid so that it can be recovered later by the user
            if (string.IsNullOrEmpty(model.Password))
            {
                model.Password = Guid.NewGuid().ToString();
            }

            MembershipCreateStatus createStatus;

            var user = Membership.CreateUser(username, model.Password, model.Email,
                null, null, true, out createStatus);

            if (createStatus == MembershipCreateStatus.DuplicateEmail)
            {
                return new ContactIdentityResult(new IdentityResult
                {
                    Status = Enums.eCustomerStatus.Error,
                    Messages = new List<string>
                    {
                        "Email already registered"
                    }
                }, null);
            }

            if (createStatus != MembershipCreateStatus.Success)
            {
                return new ContactIdentityResult(new IdentityResult
                {
                    Status = Enums.eCustomerStatus.Error,
                    Messages = new List<string>
                        {
                            $"Failed to create a user: {createStatus}"
                        }
                }, null);
            }

            if (user == null)
            {
                return new ContactIdentityResult(new IdentityResult
                {
                    Status = Enums.eCustomerStatus.Error,
                    Messages = new List<string>()
                }, null);
            }

            AssignDefaultRolesToUser(user);

            //We have to reload the contact after save in order to be able to interact with the custom properties
            var customer = CreateCustomerContact(model, user);

            UpdateCustomerPreferredAddress(model, customer);

            UpdateCustomerCustomProperties(model, customer);

            if (isBullionAccount)
            {
                UpdateBullionCustomerCustomProperties(model, customer);
            }

            customer.SaveChanges();

            return new ContactIdentityResult(new IdentityResult
            {
                Status = Enums.eCustomerStatus.Created,
                Messages = new List<string>()
            }, customer);
        }

        public bool UpdateBullionCustomerCustomProperties(ApplicationUser model, CustomerContact customer)
        {
            var needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.Gender, model.Gender);

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.SecondSurname, model.SecondSurname) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.TwoStepAuthenticationAnswer, model.TwoStepAuthenticationAnswer) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.BullionObsAccountNumber, model.BullionAccountNumber) || needToUpdate;

            if (model.CustomerKycData != null)
            {
                //KYC
                needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.BullionKycApiResponse, model.CustomerKycData.Id3Response) || needToUpdate;

                needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.KycStatus, (int)model.CustomerKycData.Status) || needToUpdate;
            }
            
            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.BullionUnableToPurchaseBullion, model.BullionUnableToPurchaseBullion) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.BullionUnableToSellorDeliverFromVault, model.BullionUnableToSellorDeliverFromVault) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.BullionUnableToDepositViaCard, model.BullionUnableToDepositViaCard) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.BullionUnableToWithdraw, model.BullionUnableToWithdraw) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.BullionUnableToLogin, model.BullionUnableToLogin) || needToUpdate;

            if (!string.IsNullOrWhiteSpace(model.TwoStepAuthenticationQuestion))
            {
                needToUpdate = customer.UpdateIntegerPropertyIfPossible(StringConstants.CustomFields.TwoStepAuthenticationQuestion, model.TwoStepAuthenticationQuestion) || needToUpdate;
            }

            if (!string.IsNullOrWhiteSpace(model.BullionCustomerType))
            {
                needToUpdate = customer.UpdateIntegerPropertyIfPossible(StringConstants.CustomFields.BullionCustomerType, model.BullionCustomerType) || needToUpdate;
            }

            if (model.BullionPremiumGroupInt > 0)
            {
                needToUpdate = customer.UpdateIntegerPropertyIfPossible(StringConstants.CustomFields.BullionPremiumGroupInt, model.BullionPremiumGroupInt.ToString()) || needToUpdate;
            }

            if (needToUpdate) customer.Properties[StringConstants.CustomFields.BullionObsAccountLastUpdatedDate].Value = DateTime.Now;
            return needToUpdate;
        }

        private string GenerateUsername(ApplicationUser model)
        {
            if (!model.RegistrationSource.Equals(StringConstants.RegistrationSource.ConsumerRegistrationPage)) return string.Empty;

            if (!string.IsNullOrEmpty(model.Email)) return model.Email;

            //if we don't have an email the membership requires one
            //To get to this point we know we have an obs account number
            //So we'll set the email to that
            model.Email = $"{model.ObsAccountNumber}@example.com";
            return model.ObsAccountNumber;
        }

        public CustomerContact CreateCustomerContact(ApplicationUser model, MembershipUser user)
        {
            var customer = CustomerContact.CreateInstance(user);

            customer.FirstName = model.FirstName;
            customer.MiddleName = model.MiddleName;
            customer.LastName = model.LastName;
            customer.FullName = !string.IsNullOrEmpty(model.FullName) ? model.FullName : $"{model.FirstName} {model.LastName}";
            customer.BirthDate = model.BirthDate;
            customer.RegistrationSource = model.RegistrationSource;
            customer.Email = user.Email;

            var isFirst = !customer.ContactAddresses.Any();
            foreach (var address in model.Addresses)
            {
                address.AddressType = CustomerAddressTypeEnum.Public;
                address.IsDefault = isFirst;

                customer.AddContactAddress(address);
                isFirst = false;
            }

            if (!string.IsNullOrEmpty(model.Currency))
            {
                customer.PreferredCurrency = model.Currency;
            }

            customer.SaveChanges();
            return _customerContext.GetContactByUserId(customer.UserId);
        }

        public void UpdateCustomerPreferredAddress(ApplicationUser model, CustomerContact customer)
        {
            if (model.Addresses.Count <= 0) return;
            var defaultAddress = customer.ContactAddresses.FirstOrDefault();

            if (defaultAddress == null) return;
            customer.PreferredBillingAddress = defaultAddress;
            customer.PreferredShippingAddress = defaultAddress;
        }

        /// <summary>
        /// This will set all the custom properties of a customer EXCEPT the OBS Account number.
        /// The only way we set the OBS Account Number is in the Customer Update Service which will go to Royal Mint and request an OBS Number if we're missing one
        /// Once a customer has an OBS Account Number in our system it should never change
        /// </summary>
        /// <param name="model">The Data we've been passed to update</param>
        /// <param name="customer">The customer to apply the update to</param>
        public bool UpdateCustomerCustomProperties(ApplicationUser model, CustomerContact customer)
        {
            var needToUpdate = false;
            if (!string.IsNullOrEmpty(model.ObsAccountNumber))
            {
                //customer.Properties[StringConstants.CustomFields.ObsAccountNumber].Value = model.ObsAccountNumber;
                needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.ObsAccountNumber, model.ObsAccountNumber) || needToUpdate;
            }

            //Title
            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.ContactTitleField, model.Title) || needToUpdate;

            //Contact Preferences
            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.ContactByEmail, model.ByEmail) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.EmailConsentDateTime, model.EmailConsentDateTime) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.EmailConsentSource, model.EmailConsentSource) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.ContactByPhone, model.ByTelephone) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.TelephoneConsentDateTime, model.TelephoneConsentDateTime) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.TelephoneConsentSource, model.TelephoneConsentSource) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.ContactByPost, model.ByPost) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.PostConsentDateTime, model.PostConsentDateTime) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.PostConsentSource, model.PostConsentSource) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.D2CCustomerType, model.D2CCustomerType) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.TaxGroup, model.TaxGroup) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.CreditLimitFieldName, model.CreditLimit) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.InclTax, model.InclTax) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.StatementPreference, model.StatementPreference) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.DlvMode, model.DlvMode) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.Department, model.Department) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.CustClassificationId, model.ClassificationId) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.Activated, model.IsActivated) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.Telephone, model.PhoneNumber) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.Mobile, model.MobilePhone) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.Affix, model.Affix) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.IsSiteCoreUser, model.IsSiteCoreUser) || needToUpdate;

            needToUpdate = customer.UpdateProperty(StringConstants.CustomFields.CustomerType, model.CustomerType) || needToUpdate;

            customer.CustomerGroup = string.Empty;
            return needToUpdate;
        }

        public void AssignDefaultRolesToUser(MembershipUser user)
        {
            var roles = from roleName in _defaultUserRoles
                        where !_userImpersonation.CreatePrincipal(user.UserName).IsInRole(roleName)
                        select roleName;

            //Assign default roles to new user
            foreach (var roleName in roles)
            {

                if (!Roles.RoleExists(roleName))
                {
                    Roles.CreateRole(roleName);
                }

                Roles.AddUserToRole(user.UserName, roleName);
            }
        }

        public virtual ApplicationUser GetUser(string email)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            email = email.ToLower();

            var username = Membership.GetUserNameByEmail(email);

            if (!string.IsNullOrEmpty(username))
            {
                var user = Membership.GetUser(username);

                var mapUserKey = new MapUserKey();

                var customer = _customerContext.GetContactByUserId(mapUserKey.ToTypedString(username));

                if (user != null && customer != null)
                {
                    return GetApplicationUser(user, customer);
                }
            }
            return new ApplicationUser();
        }

        public CustomerContact GetExistingContactByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return null;

            var user = Membership.GetUser(userName);
            if (user?.ProviderUserKey == null) return null;
            var contactUserId = (new MapUserKey()).ToTypedString(user.UserName);
            return CustomerContext.Current.GetContactByUserId(contactUserId);
        }

        protected ApplicationUser GetApplicationUser(MembershipUser membershipUser, CustomerContact customer)
        {
            return new ApplicationUser
            {
                UserId = membershipUser.UserName,
                Email = membershipUser.Email,
                Title = customer.GetStringProperty(StringConstants.CustomFields.ContactTitleField),
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Addresses = customer.ContactAddresses.ToList(),
                BirthDate = customer.BirthDate,
                ByPost = customer.GetBooleanProperty(StringConstants.CustomFields.ContactByPost),
                ByEmail = customer.GetBooleanProperty(StringConstants.CustomFields.ContactByEmail),
                ByTelephone = customer.GetBooleanProperty(StringConstants.CustomFields.ContactByPhone),
                UserName = membershipUser.UserName,
                PhoneNumber = customer.GetStringProperty(StringConstants.CustomFields.Telephone),
                IsLockedOut = membershipUser.IsLockedOut || customer.GetBooleanProperty(StringConstants.CustomFields.CustomerLockedOut),
                IsActivated = customer.GetBooleanProperty(StringConstants.CustomFields.Activated),
                IsMigrated = customer.GetBooleanProperty(StringConstants.CustomFields.MigratedToEpiUser),
            };
        }
    }
}