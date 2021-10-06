using Mediachase.Commerce.Customers;
using Microsoft.AspNet.Identity.Owin;
using System.Web.Security;
using TRM.Shared.Models.DTOs;
using TRM.Shared.Services;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Services
{
    public interface IUserService : IBaseUserService
    {
        SignInStatus SignIn(string email, string password);
        void SignOut();
        bool ResetUserPassword(string email, string newPassword, string token);
        bool UpdateUserEmail(string oldEmail, string newEmail, string password, CustomerContact customer);
        CustomerContact GetCustomerContactByActivationCode(string obsAccountNumber, string postCode);
        CustomerContact GetCustomerContactByBullionObsAccountNumber(string bullionObsAccountNumber);
        bool CheckCustomerExistsWithEmail(string email);
        MembershipUser CreateUser(string email, string password);
        bool CreateUserForExistingContact(CustomerContact customerContact);
        bool CreateUserForExistingContact(CustomerContact customerContact, string password, out string message);
        bool TryToSendRegistrationConfirmationEmail(CustomerContact customerContact);
        bool TryToMigrateSiteCoreUserToEpiserver(string email);
        bool SendResetPasswordEmail(string email);
        bool ValidateUser(string username, string password);
        bool Login(string email, string password);
        bool ChangePassword(string username, string oldPassword, string newPassword);
        bool CheckTwoStepAuthentication(CustomerContact customerContact, string securityAnswer);
        bool SendRequestUsername(string username);
        bool SendBullionAccountUpdatedEmail(string emailTemplateName, AccountDetailChangedMailedUserInformationDto userInfo);
        bool UnableToLogin(string username);
        void ImpersonateToUser(string userName);
        bool IsImpersonating();
        bool StopImpersonating();
        string GetImpersonatingUserName();
        ImpersonateUser GetImpersonateUser(); 
        ImpersonateUser GetImpersonateUser(CustomerContact customerContact); 
        CustomerContact GetContactBeforeImpersonating();
        string GetUsername();
        string GetSecurityQuestion(CustomerContact customerContact);
        bool UpdateCustomerCustomProperties(ApplicationUser model, CustomerContact customer);
        bool CreateUserContext(string email);
    }
}
