using Mediachase.Commerce.Customers;
using Microsoft.AspNet.Identity.Owin;
using System;
using TRM.Shared.Models.DTOs;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Bullion;

namespace TRM.Web.Services
{
    public interface IBullionUserService
    {
        ContactIdentityResult BullionRegisterAccount(ApplicationUser model);
        SignInStatus BullionSignIn(string username, string password);
        void UpgradeToBullionAccount(CustomerContact contact, ApplicationUser model);
        bool UpdateSecurityQuestionOfBullionAccount(string newQuestion, string newAnswer);
        bool SendApplicationReceivedEmail(MailedUserInformationDto userInfo);
        bool UpdateBullionCustomerBalances(AxImportData.CustomerBalance balance, Guid customerId);
        bool UpdateBullionCustomerCustomProperties(ApplicationUser model, CustomerContact customer);
    }
}