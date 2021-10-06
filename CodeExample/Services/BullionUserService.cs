using System;
using System.Collections.Generic;
using System.Web.Security;
using EPiServer;
using EPiServer.Framework.Localization;
using EPiServer.Security;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Microsoft.AspNet.Identity.Owin;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Web.Business.Email;
using TRM.Web.Helpers;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Bullion;
using static TRM.Web.Constants.StringConstants;

namespace TRM.Web.Services
{
    public class BullionUserService : UserService, IBullionUserService
    {
        private readonly CustomerContext _customerContext;
        private readonly IAmTransactionHistoryHelper _transactionHistoryHelper;

        public BullionUserService(
            CustomerContext customerContext,
            LocalizationService localizationService,
            IEmailHelper emailHelper,
            IContentLoader contentLoader,
            IAmResetTokenHelper resetTokenHelper,
            IUserImpersonation userImpersonation,
            IAmMarketHelper marketHelper,
            IAmVatHelper vatHelper,
            ICurrentMarket currentMarket,
            IAmTransactionHistoryHelper transactionHistoryHelper,
            IAmBullionContactHelper bullionContactHelper,
            IAmSecurityQuestionHelper securityQuestionHelper) : base(customerContext, localizationService, emailHelper, contentLoader, resetTokenHelper, userImpersonation, marketHelper, vatHelper, currentMarket, bullionContactHelper, securityQuestionHelper)
        {
            _customerContext = customerContext;
            _transactionHistoryHelper = transactionHistoryHelper;
        }

        public bool SendApplicationReceivedEmail(MailedUserInformationDto userInfo)
        {
            var emailTemplateName = BullionEmailCategories.ApplicationReceivedEmail;
            var toAddresses = new List<MailAddress> { new MailAddress(userInfo.Email, userInfo.FullName) };
            var emailParams = new Dictionary<string, object>
            {
                { EmailParameters.Title, userInfo.Title },
                { EmailParameters.FirstName, userInfo.FirstName },
                { EmailParameters.LastName, userInfo.LastName }
            };
            string sendingEmailErrorMessage;
            return _emailHelper.SendBullionEmail(emailTemplateName, toAddresses, out sendingEmailErrorMessage, emailParams);
        }

        public bool UpdateBullionCustomerBalances(AxImportData.CustomerBalance balance, Guid customerId)
        {
            var customer = _customerContext.GetContactById(customerId);
            if (customer == null) return false;

            var effectiveBalance = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);
            var availableToSpend = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend);
            var availableToWithdraw = customer.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToWithdraw);

            // if all the balances are unchanged- ignore if this is true
            if (availableToSpend == balance.Balances.AvailableToSpend &&
                availableToWithdraw == balance.Balances.AvailableToWithdraw &&
                effectiveBalance == balance.Balances.Effective)
            {
                return true;
            }

            // if there are any transactions in the transaction history header with IncludedInAXBalances = false
            // ignore the balances update
            if (_transactionHistoryHelper.ShouldNotUpdateBalance(customerId.ToString()))
            {
                return true;
            }

            customer.Properties[StringConstants.CustomFields.BullionCustomerAvailableToSpend].Value = balance.Balances.AvailableToSpend;
            customer.Properties[StringConstants.CustomFields.BullionCustomerAvailableToWithdraw].Value = balance.Balances.AvailableToWithdraw;
            customer.Properties[StringConstants.CustomFields.BullionCustomerEffectiveBalance].Value = balance.Balances.Effective;

            customer.Properties[StringConstants.CustomFields.BullionObsAccountLastUpdatedDate].Value = DateTime.UtcNow;
            customer.SaveChanges();

            return true;
        }

        public virtual ContactIdentityResult BullionRegisterAccount(ApplicationUser model)
        {
            return RegisterAccount(model, true);
        }

        public SignInStatus BullionSignIn(string username, string password)
        {
            if (!string.IsNullOrEmpty(username))
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

        public void UpgradeToBullionAccount(CustomerContact customer, ApplicationUser model)
        {
            customer.FirstName = model.FirstName;
            customer.MiddleName = model.MiddleName;
            customer.LastName = model.LastName;
            customer.FullName = !string.IsNullOrEmpty(model.FullName) ? model.FullName : $"{model.FirstName} {model.LastName}";
            customer.BirthDate = model.BirthDate;
            customer.RegistrationSource = model.RegistrationSource;
            foreach (var address in model.Addresses)
            {
                address.AddressType = CustomerAddressTypeEnum.Public;
                customer.AddContactAddress(address);
            }

            //Title
            customer.Properties[StringConstants.CustomFields.ContactTitleField].Value = model.Title;
            customer.Properties[StringConstants.CustomFields.Telephone].Value = model.PhoneNumber;
            customer.Properties[StringConstants.CustomFields.Mobile].Value = model.MobilePhone;

            //Customer Type
            customer.Properties[StringConstants.CustomFields.CustomerType].Value = StringConstants.CustomerType.ConsumerAndBullion;
            customer.Properties[StringConstants.CustomFields.BullionCustomerType].Value = (int)Enums.BullionCustomerType.Standard;

            //Currency
            customer.PreferredCurrency = model.Currency;
            customer.Properties[StringConstants.CustomFields.Gender].Value = model.Gender;
            customer.Properties[StringConstants.CustomFields.BullionObsAccountLastUpdatedDate].Value = DateTime.Now;
            customer.Properties[StringConstants.CustomFields.SecondSurname].Value = model.SecondSurname;

            customer.UpdateIntegerPropertyIfPossible(StringConstants.CustomFields.TwoStepAuthenticationQuestion, model.TwoStepAuthenticationQuestion);

            customer.Properties[StringConstants.CustomFields.TwoStepAuthenticationAnswer].Value = model.TwoStepAuthenticationAnswer;

            //KYC
            customer.Properties[StringConstants.CustomFields.BullionKycApiResponse].Value = model.CustomerKycData.Id3Response;
            customer.Properties[StringConstants.CustomFields.KycStatus].Value = (int)model.CustomerKycData.Status;

            customer.SaveChanges();
        }

        public bool UpdateSecurityQuestionOfBullionAccount(string newQuestion, string newAnswer)
        {
            try
            {
                var customer = _customerContext.CurrentContact;

                customer.UpdateIntegerPropertyIfPossible(StringConstants.CustomFields.TwoStepAuthenticationQuestion, newQuestion);

                customer.Properties[StringConstants.CustomFields.TwoStepAuthenticationAnswer].Value = newAnswer;
                customer.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}