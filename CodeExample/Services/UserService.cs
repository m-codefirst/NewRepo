using EPiServer;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Hephaestus.XmlSitemaps2.Extensions;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Shared.Services;
using TRM.Web.Business.Email;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Pages;
using static TRM.Web.Constants.StringConstants;
using StringResources = TRM.Web.Constants.StringResources;

namespace TRM.Web.Services
{
    [ServiceConfiguration(typeof(IUserService), Lifecycle = ServiceInstanceScope.Transient)]
    public class UserService : BaseUserService, IUserService
    {
        private readonly CustomerContext _customerContext;
        private readonly LocalizationService _localizationService;

        protected readonly IEmailHelper _emailHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IAmResetTokenHelper _resetTokenHelper;
        private readonly IAmMarketHelper _marketHelper;
        private readonly IAmVatHelper _vatHelper;
        private readonly ICurrentMarket _currentMarket;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IAmSecurityQuestionHelper _securityQuestionHelper;

        public UserService(
            CustomerContext customerContext,
            LocalizationService localizationService,
            IEmailHelper emailHelper,
            IContentLoader contentLoader,
            IAmResetTokenHelper resetTokenHelper,
            IUserImpersonation userImpersonation,
            IAmMarketHelper marketHelper,
            IAmVatHelper vatHelper,
            ICurrentMarket currentMarket,
            IAmBullionContactHelper bullionContactHelper,
            IAmSecurityQuestionHelper securityQuestionHelper) : base(customerContext, userImpersonation)
        {
            _customerContext = customerContext;
            _emailHelper = emailHelper;
            _contentLoader = contentLoader;
            _resetTokenHelper = resetTokenHelper;
            _localizationService = localizationService;
            _marketHelper = marketHelper;
            _vatHelper = vatHelper;
            _currentMarket = currentMarket;
            _bullionContactHelper = bullionContactHelper;
            _securityQuestionHelper = securityQuestionHelper;
        }

        public bool ValidateUser(string username, string password)
        {
            var customer = _customerContext.GetContactByUserId(new MapUserKey().ToTypedString(username));
            return customer != null && Membership.ValidateUser(username, password);
        }

        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var user = Membership.GetUser(username);
            return user != null && user.ChangePassword(oldPassword, newPassword);
        }

        public SignInStatus SignIn(string email, string password)
        {
            var username = Membership.GetUserNameByEmail(email.ToLower());

            if (!username.IsNullOrEmpty())
            {
                var validSignIn = Membership.ValidateUser(username, password);

                if (validSignIn)
                {
                    LoginNewUser(username);
                    UpdateCustomerGroupAndCurrentMarket(username);
                    return SignInStatus.Success;
                }
            }

            return SignInStatus.Failure;
        }

        public bool Login(string email, string password)
        {
            LoginNewUser(email);

            UpdateCustomerGroupAndCurrentMarket(email);

            CheckForNewDeviceLogin(email);

            return true;
        }

        private void CheckForNewDeviceLogin(string email)
        {
            var customer = _customerContext.GetContactByUserId((new MapUserKey()).ToTypedString(email));
            if (customer == null) return;

            if (!_bullionContactHelper.IsBullionAccount(customer)) return;
            if (!customer.PrimaryKeyId.HasValue) return;

            var previousLogins = CookieHelper.GetBasicCookie(Cookies.DeviceLogin)?.Value;

            var newDevice = false;
            var timeoutDays = 180;

            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var deviceTrackingTimeoutDays = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).DeviceTrackingTimeoutDays :
                startPage?.DeviceTrackingTimeoutDays;
            if (deviceTrackingTimeoutDays != null && deviceTrackingTimeoutDays > 0)
            {
                timeoutDays = deviceTrackingTimeoutDays.Value;
            }

            var timeSpan = new TimeSpan(timeoutDays, 0, 0, 0);

            if (string.IsNullOrWhiteSpace(previousLogins))
            {
                newDevice = true;
                CookieHelper.CreateBasicCookie(Cookies.DeviceLogin, customer.PrimaryKeyId.Value.GetHashCode().ToString(), timeSpan);
            }
            else
            {

                //If we don't have the value 
                if (!previousLogins.Contains(customer.PrimaryKeyId.Value.GetHashCode().ToString()))
                {
                    newDevice = true;
                    CookieHelper.CreateBasicCookie(Cookies.DeviceLogin, $"{previousLogins},{customer.PrimaryKeyId.Value.GetHashCode()}", timeSpan);
                }
                else
                {
                    //re create to the extend the expiry date
                    CookieHelper.CreateBasicCookie(Cookies.DeviceLogin, previousLogins, timeSpan);
                }

            }

            if (newDevice)
            {
                SendDeviceLoginEmail(customer);
            }
        }

        public bool CheckTwoStepAuthentication(CustomerContact customerContact, string securityAnswer)
        {
            var securityQuestion = GetSecurityQuestion(customerContact);
            if (string.IsNullOrEmpty(securityQuestion)) return false;

            var twoStepAuthenticationAnswer = customerContact.Properties[StringConstants.CustomFields.TwoStepAuthenticationAnswer]?.Value?.ToString();
            if (string.IsNullOrEmpty(twoStepAuthenticationAnswer) || string.IsNullOrEmpty(securityAnswer)) return false;

            return string.Equals(twoStepAuthenticationAnswer, securityAnswer, StringComparison.OrdinalIgnoreCase);
        }

        public string GetSecurityQuestion(CustomerContact customerContact)
        {
            var securityQuestionId = customerContact.Properties[StringConstants.CustomFields.TwoStepAuthenticationQuestion]?.Value?.ToString();
            return string.IsNullOrEmpty(securityQuestionId) ? null : _securityQuestionHelper.GetQuestionById(securityQuestionId);
        }

        private bool SendMigratedCustomerSetPasswordEmail(string email)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var resetPasswordPage = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ResetPasswordPage :
                startPage?.ResetPasswordPage;

            var migratedCustomerSetPasswordEmailRef = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).MigratedCustomerSetPasswordEmail :
                startPage?.MigratedCustomerSetPasswordEmail;

            if (migratedCustomerSetPasswordEmailRef == null || resetPasswordPage == null) return false;

            var migratedCustomerSetPasswordEmail = _contentLoader.Get<TRMEmailPage>(migratedCustomerSetPasswordEmailRef);

            return SendForgottenUsernameOrPassword(email, migratedCustomerSetPasswordEmail, startPage);
        }

        public bool SendResetPasswordEmail(string email)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var resetPasswordPage = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ResetPasswordPage :
                startPage?.ResetPasswordPage;

            var forgottenUsername = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ForgottenUsername :
                startPage?.ForgottenUsername;
            if (forgottenUsername == null || resetPasswordPage == null) return false;

            var forgottenUsernameOrPasswordEmailTemplate = _contentLoader.Get<TRMEmailPage>(forgottenUsername);

            return SendForgottenUsernameOrPassword(email, forgottenUsernameOrPasswordEmailTemplate, startPage);
        }

        public bool TryToMigrateSiteCoreUserToEpiserver(string email)
        {
            var mapUserKey = new MapUserKey();

            var userFilters = new OrBlockFilterElement(
                new FilterElement(ContactEntity.FieldUserId, FilterElementType.Equal, mapUserKey.ToTypedString(email)),
                new FilterElement(ContactEntity.FieldEmail, FilterElementType.Equal, email)
            );

            FilterElement[] filters = { userFilters };
            var customerContact = BusinessManager.List(StringConstants.CustomFields.ContactClassName, filters).Cast<CustomerContact>().FirstOrDefault();
            var isSiteCoreUser = customerContact.GetBooleanProperty(StringConstants.CustomFields.IsSiteCoreUser);
            if (customerContact == null) return false;

            //for bullion account
            if (_bullionContactHelper.IsBullionAccount(customerContact))
            {
                return !customerContact.GetBooleanProperty(StringConstants.CustomFields.Activated) && isSiteCoreUser && SendMigratedCustomerSetPasswordEmail(customerContact.Email);
            }

            //for consumer account
            return isSiteCoreUser && CreateUserForExistingContact(customerContact) && SendMigratedCustomerSetPasswordEmail(email);
        }

        private bool UpdateMigratedCustomer(CustomerContact customerContact)
        {
            customerContact.Properties[StringConstants.CustomFields.Activated].Value = true;
            customerContact.SaveChanges();
            return true;
        }

        public CustomerContact GetCustomerContactByActivationCode(string obsAccountNumber, string postCode)
        {
            var obsAccountNumberFilter = new FilterElement(StringConstants.CustomFields.ObsAccountNumber, FilterElementType.Equal, obsAccountNumber);
            var activatedFilter = new FilterElement(StringConstants.CustomFields.Activated, FilterElementType.Equal, false);
            var filters = new[] { obsAccountNumberFilter, activatedFilter };
            var customerContacts = BusinessManager.List(StringConstants.CustomFields.ContactClassName, filters).Cast<CustomerContact>();
            postCode = postCode.ToLower().Replace(" ", string.Empty);

            return customerContacts.FirstOrDefault(customerContact =>
                customerContact.ContactAddresses.Any(x => x.PostalCode != null && x.PostalCode.ToLower().Replace(" ", string.Empty).Equals(postCode)));
        }
        public CustomerContact GetCustomerContactByBullionObsAccountNumber(string bullionObsAccountNumber)
        {
            var obsAccountNumberFilter = new FilterElement(StringConstants.CustomFields.BullionObsAccountNumber, FilterElementType.Equal, bullionObsAccountNumber);
            var activatedFilter = new FilterElement(StringConstants.CustomFields.Activated, FilterElementType.Equal, true);
            var filters = new[] { obsAccountNumberFilter, activatedFilter };
            var customerContacts = BusinessManager.List(StringConstants.CustomFields.ContactClassName, filters).Cast<CustomerContact>();
            return customerContacts.FirstOrDefault();
        }

        public bool CheckCustomerExistsWithEmail(string email)
        {
            var emailFilter = new FilterElement(StringConstants.Email, FilterElementType.Equal, email);
            var isGuessFilter = new FilterElement(StringConstants.CustomFields.IsGuest, FilterElementType.Equal, false);
            var filters = new[] { emailFilter, isGuessFilter };
            var customerContacts = BusinessManager.List(StringConstants.CustomFields.ContactClassName, filters).Cast<CustomerContact>();
            return customerContacts.Any();
        }

        public MembershipUser CreateUser(string email, string password)
        {
            MembershipCreateStatus status;
            var newUser = Membership.CreateUser(email, password, email, null, null, true, out status);
            
            if (status != MembershipCreateStatus.Success)
            {
                return null;
            }

            AssignDefaultRolesToUser(newUser);

            return newUser;
        }

        public bool CreateUserForExistingContact(CustomerContact customerContact)
        {
            var password = customerContact.Password.IsNullOrEmpty() ? Guid.NewGuid().ToString() : customerContact.Password;

            MembershipCreateStatus createStatus;
            var newUser = Membership.CreateUser(customerContact.Email, password, customerContact.Email,
                null, null, true, out createStatus);

            if (createStatus != MembershipCreateStatus.Success)
            {
                return false;
            }

            AssignDefaultRolesToUser(newUser);

            var mapUserKey = new MapUserKey();
            customerContact.Properties[StringConstants.CustomFields.Activated].Value = true;
            customerContact.UserId = mapUserKey.ToTypedString(customerContact.Email);
            customerContact.SaveChanges();

            return true;
        }

        public bool CreateUserForExistingContact(CustomerContact customerContact, string password, out string message)
        {

            MembershipCreateStatus createStatus;
            var newUser = Membership.CreateUser(customerContact.Email, password, customerContact.Email,
                null, null, true, out createStatus);

            if (createStatus != MembershipCreateStatus.Success)
            {
                if (createStatus == MembershipCreateStatus.DuplicateUserName || createStatus == MembershipCreateStatus.DuplicateEmail)
                {
                    message = _localizationService.GetStringByCulture(StringResources.EmailAlreadyRegistered,
                        TranslationFallback.EmailAlreadyRegistered,
                        ContentLanguage.PreferredCulture);
                }
                else
                {
                    message = "There is an error when creating account. Please try again!";
                }
                return false;
            }

            AssignDefaultRolesToUser(newUser);

            var mapUserKey = new MapUserKey();
            customerContact.Properties[StringConstants.CustomFields.Activated].Value = true;
            customerContact.UserId = mapUserKey.ToTypedString(customerContact.Email);
            customerContact.SaveChanges();

            message = string.Empty;
            return true;
        }

        public bool TryToSendRegistrationConfirmationEmail(CustomerContact customerContact)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var registrationConfirmationEmailRef = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).RegistrationConfirmationEmail :
                startPage?.RegistrationConfirmationEmail;
            var registrationConfirmationEmailPage =
                _contentLoader.Get<TRMEmailPage>(registrationConfirmationEmailRef);
            if (registrationConfirmationEmailPage == null) return false;
            _emailHelper.SendRegistrationConfirmationEmail(registrationConfirmationEmailPage, customerContact);

            return true;
        }

        public virtual bool ResetUserPassword(string username, string newPassword, string token)
        {
            if (string.IsNullOrEmpty(username)) return false;
            if (!_resetTokenHelper.ValidatePasswordResetToken(username, token)) return false;
            var user = Membership.GetUser(username);

            if (user == null) return false;
            user.UnlockUser();
            user.ChangePassword(user.ResetPassword(), newPassword);

            var contact = GetExistingContactByUserName(username);
            var isSiteCoreUser = contact.GetBooleanProperty(StringConstants.CustomFields.IsSiteCoreUser);

            if (null != contact && !_bullionContactHelper.IsBullionAccount(contact))
            {
                LoginNewUser(user.UserName);
            }
            else if (_bullionContactHelper.IsBullionAccount(contact) && isSiteCoreUser)
            {
                UpdateMigratedCustomer(contact);
            }

            _resetTokenHelper.DeletePasswordResetToken(token);

            return true;
        }
        public bool CreateUserContext(string email)
        {
            LoginNewUser(email);

            UpdateCustomerGroupAndCurrentMarket(email);

            CheckForNewDeviceLogin(email);

            return true;
        }
        protected void LoginNewUser(string userName, string userData = "")
        {
            var inactivityTimeoutValue = GetInactivityTimeoutValue();
            var ticket = new FormsAuthenticationTicket(1, userName, DateTime.Now,
                DateTime.Now.AddMinutes(inactivityTimeoutValue), false, userData,
                FormsAuthentication.FormsCookiePath);
            var encrytedTicket = FormsAuthentication.Encrypt(ticket);
            HttpContext.Current.Response.Cookies.Set(new HttpCookie(FormsAuthentication.FormsCookieName, encrytedTicket));

            // Update Authentication entity
            var identity = new FormsIdentity(ticket);
            HttpContext.Current.User = new GenericPrincipal(identity, null);
        }

        // Impersonation
        #region Impersonation
        public void ImpersonateToUser(string userName)
        {
            LoginNewUser(userName, PrincipalInfo.Current.Name);

            UpdateCustomerGroupAndCurrentMarket(userName);
        }

        public bool IsImpersonating()
        {
            return !string.IsNullOrWhiteSpace(GetImpersonatingUserName());
        }

        public string GetImpersonatingUserName()
        {
            var formsIdentity = HttpContext.Current?.User?.Identity as FormsIdentity;
            return formsIdentity == null ? string.Empty : formsIdentity.Ticket.UserData;
        }

        public bool StopImpersonating()
        {
            // Check if is impersonating
            var impersonatingUserName = GetImpersonatingUserName();
            if (!string.IsNullOrWhiteSpace(impersonatingUserName))
            {

                // Signout current user
                SignOut();

                // Login to original user
                LoginNewUser(impersonatingUserName);

                return true;
            }

            return false;
        }

        public ImpersonateUser GetImpersonateUser()
        {
            return GetImpersonateUser(null);
        }

        public ImpersonateUser GetImpersonateUser(CustomerContact customerContact)
        {
            var contact = customerContact ?? GetContactBeforeImpersonating();
            if (contact != null)
            {
                return new ImpersonateUser
                {
                    UserId = contact.PrimaryKeyId.Value.ToString(),
                    Username = contact.Email,
                    UserType = _bullionContactHelper.GetBullionCustomerType(contact),
                    Fullname = $"{contact.FirstName} {contact.LastName}"
                };
            }

            return null;
        }

        public CustomerContact GetContactBeforeImpersonating()
        {
            var username = GetImpersonatingUserName();
            if (string.IsNullOrEmpty(username)) return null;

            var contact = GetExistingContactByUserName(username);
            if (contact?.PrimaryKeyId == null) return null;

            return contact;
        }

        #endregion 

        private int GetInactivityTimeoutValue()
        {
            const int value = 30;
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null)
            {
                return value;
            }

            return startPage.InactivityTimeout > 0
                ? startPage.InactivityTimeout
                : value;
        }

        public bool UpdateUserEmail(string oldEmail, string newEmail, string password, CustomerContact customer)
        {
            MembershipCreateStatus createStatus;
            MembershipUser newUser = Membership.CreateUser(newEmail, password, newEmail,
                null, null, true, out createStatus);

            // assign roles
            var roles = Roles.GetRolesForUser(oldEmail);
            if (roles.Any())
            {
                Roles.AddUserToRoles(newEmail, roles);
            }
            else
            {
                AssignDefaultRolesToUser(newUser);
            }

            if (createStatus != MembershipCreateStatus.Success) return false;

            if (newUser != null)
            {
                var mapUserKey = new MapUserKey();
                customer.UserId = mapUserKey.ToTypedString(newUser.UserName);
            }
            customer.Email = newEmail;
            customer.SaveChanges();
            Membership.DeleteUser(oldEmail);

            return true;
        }

        public void SignOut()
        {
            FormsAuthentication.SignOut();

            HttpContext.Current.Session.Abandon();
            HttpContext.Current.Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", ""));

            CookieHelper.RemoveCookie(Cookies.UserCookie);
            CookieHelper.RemoveCookie(Cookies.BasketSummary);
            CookieHelper.RemoveCookie(Cookies.RecentlyViewed);
            CookieHelper.RemoveCookie(Cookies.ProductCompare);
            CookieHelper.RemoveCookie(FormsAuthentication.FormsCookieName);
            CookieHelper.RemoveCookie(Cookies.MessageCookie);
            CookieHelper.RemoveCookie(Cookies.Currency);
        }


        /// <summary>
        /// Only update customer current market in case this field CurrentMarketId of Contact is not set yet.
        /// </summary>
        public void UpdateCustomerGroupAndCurrentMarket(string userId)
        {
            var customer = _customerContext.GetContactByUserId((new MapUserKey()).ToTypedString(userId));
            if (customer == null) return;

            var customerCurrentMarketId = customer.GetStringProperty(StringConstants.CustomFields.MarketIdFieldName);

            if (string.IsNullOrEmpty(customerCurrentMarketId))
            {
                var preferredShippingAddress = GetPreferredShippingAddress(customer);
                if (preferredShippingAddress != null)
                {
                    UpdateCustomerCurrentMarket(preferredShippingAddress.CountryCode, customer);
                    _vatHelper.UpdateCustomerGroup(preferredShippingAddress.CountryCode, customer);
                    customerCurrentMarketId = customer.GetStringProperty(StringConstants.CustomFields.MarketIdFieldName);
                }
            }

            if (!string.IsNullOrEmpty(customerCurrentMarketId))
            {
                _currentMarket.SetCurrentMarket(customerCurrentMarketId);
            }
            CookieHelper.CreateBasicCookie(Cookies.Currency, _bullionContactHelper.GetDefaultCurrencyCode(customer));
        }

        private void UpdateCustomerCurrentMarket(string countryCode, CustomerContact customer)
        {
            var market = _marketHelper.GetMarketFromCountryCode(countryCode);
            if (market != null)
            {
                customer.Properties[StringConstants.CustomFields.MarketIdFieldName].Value = market.MarketId.Value;
                customer.SaveChanges();
            }
        }

        private CustomerAddress GetPreferredShippingAddress(CustomerContact customer)
        {
            if (customer == null) return null;
            var addresses = _customerContext.GetContactAddresses(customer)?.ToList();
            if (addresses == null) return null;

            var preferredShippingAddress = addresses.FirstOrDefault(x => x.AddressId == customer.PreferredShippingAddressId);
            if (preferredShippingAddress != null) return preferredShippingAddress;

            var registeredAddress = addresses.FirstOrDefault(x => x.Name.Equals(StringConstants.DefaultRegisteredAddressName));
            if (registeredAddress != null) return registeredAddress;

            return addresses.FirstOrDefault();
        }

        public bool SendRequestUsername(string email)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var resetPasswordPage = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ResetPasswordPage :
                startPage?.ResetPasswordPage;

            var forgottenUsername = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ForgottenUsername :
                startPage?.ForgottenUsername;

            if (forgottenUsername == null || resetPasswordPage == null) return false;

            var forgottenUsernameEmailTemplate = _contentLoader.Get<TRMEmailPage>(forgottenUsername);

            return SendForgottenUsernameOrPassword(email, forgottenUsernameEmailTemplate, startPage);
        }

        private bool SendForgottenUsernameOrPassword(string email, TRMEmailPage emailTemplate, StartPage startPage)
        {
            var resetPasswordPage = (startPage?.UserAccountSettingsPage != null) ?
                _contentLoader.Get<UserAccountSettingsPage>(startPage.UserAccountSettingsPage).ResetPasswordPage :
                startPage?.ResetPasswordPage;
            var resetPageUrl = resetPasswordPage.GetAbsoluteUrl();

            var membershipUsers = Membership.FindUsersByEmail(email);

            if (membershipUsers == null || membershipUsers.Count <= 0) return false;

            List<string> userNameList = new List<string>();

            foreach (MembershipUser user in membershipUsers)
            {
                userNameList.Add(user.UserName);
            }

            _emailHelper.SendForgottenUsername(email, emailTemplate, resetPageUrl, userNameList);

            return true;
        }

        public bool SendBullionAccountUpdatedEmail(string emailTemplateName, AccountDetailChangedMailedUserInformationDto userInfo)
        {
            if (string.IsNullOrEmpty(emailTemplateName)) return false;
            var toAddresses = new List<MailAddress> { new MailAddress(userInfo.Email, userInfo.FullName) };
            var emailParams = GetEmailParametersForAccountDetailChanged(emailTemplateName, userInfo);
            string sendingEmailErrorMessage;
            return _emailHelper.SendBullionEmail(emailTemplateName, toAddresses, out sendingEmailErrorMessage, emailParams);
        }

        private Dictionary<string, object> GetEmailParametersForAccountDetailChanged(string emailCategory, AccountDetailChangedMailedUserInformationDto userInfo)
        {
            var emailParams = new Dictionary<string, object>
            {
                { EmailParameters.Title, userInfo.Title },
                { EmailParameters.FirstName, userInfo.FirstName },
                { EmailParameters.LastName, userInfo.LastName }
            };

            if (emailCategory.Equals(BullionEmailCategories.EmailChangedEmail, StringComparison.InvariantCultureIgnoreCase)
                || emailCategory.Equals(BullionEmailCategories.SecurityQuestionChangedEmail, StringComparison.InvariantCultureIgnoreCase))
            {
                emailParams.Add(EmailParameters.AccountNumber, userInfo.AccountNumber);
            }
            else
            {
                emailParams.Add(EmailParameters.NewUsername, userInfo.NewUserName);
            }

            if (emailCategory.Equals(BullionEmailCategories.EmailChangedEmail, StringComparison.InvariantCultureIgnoreCase))
            {
                emailParams.Add(EmailParameters.NewEmail, userInfo.NewEmail);
            }
            return emailParams;
        }

        public bool UnableToLogin(string username)
        {
            var mapUserKey = new MapUserKey();

            var customerContact = _customerContext.GetContactByUserId(mapUserKey.ToTypedString(username));

            return customerContact.GetBooleanProperty(StringConstants.CustomFields.BullionUnableToLogin);
        }

        public virtual void SendDeviceLoginEmail(CustomerContact customer)
        {
            var isBullionAccount = _bullionContactHelper.IsBullionAccount(customer);
            _emailHelper.SendNewDeviceLoginEmail(customer, isBullionAccount);
        }

        public string GetUsername()
        {
            var customerContact = _customerContext.CurrentContact;
            return _bullionContactHelper.GetUsername(customerContact);
        }
    }
}