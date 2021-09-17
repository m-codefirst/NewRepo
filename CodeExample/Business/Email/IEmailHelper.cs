using System;
using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using PricingAndTradingService.Models;
using TRM.Shared.Models.DTOs;
using TRM.Web.Models.Articles;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.EntityFramework.RmgOrders;
using TRM.Web.Models.Pages;
using TRM.Web.Models.SpecialEvents;
using TRM.Web.Models.ViewModels.Cart;

namespace TRM.Web.Business.Email
{
    public interface IEmailHelper
    {
        void SendJobFailedEmail(TrmErrorEmailPage emailPage, string jobName, Exception ex);
        void SendEmailToContact(TRMEmailPage emailPage, string email, Dictionary<string, object> optionalParameters);
        void SendJobFailedEmail(TrmErrorEmailPage emailPage, string jobName, string ex);
        void SendEmailForKycStatusChange(TRMEmailPage emailPage, CustomerContact contact);
        void SendEmailForMissingPampQuoteIds(TRMEmailPage emailPage, string toEmailAddress, string content);
        void SendRegistrationConfirmationEmail(TRMEmailPage emailPage, ApplicationUser user);
        void SendOrderConfirmationEmail(TRMEmailPage orderConfirmationEmail, PurchaseOrderViewModel purchaseOrder, CustomerContact customerContact);
        void SendAccountDetailsChangedEmail(TRMEmailPage accountDetailsChangedEmail, string emailAddress, List<string> changedFields, List<string> oldValues, List<string> newValues, string accountName);
        void SendSovereignCertificateSignUpEmail(TRMEmailPage certificateSignUpEmail, SovereignCertificateSignUpDto signup);
        void SendRegistrationConfirmationEmail(TRMEmailPage emailPage, CustomerContact customerContact);
        void SendGdprConfirmEmail(TRMEmailPage emailPage, string emailAddress, string lastName);
        void SendGdprIdentityEmail(TRMEmailPage emailPage, string emailAddress, string lastName, string pageUrl);
        void SendNewsletterSignUpConsentEmail(TRMEmailPage emailPage, string email, int newsLetterSubscriptionId, string newsletterSignUpPageUrl);
        void SendRmgConfirmationEmail(TRMEmailPage rmgOrderConfirmation, RmgOrder order);
        void SendWishListConfirmationEmail(TRMEmailPage wishListConfirmationEmail, List<CartItemViewModel> wishList, string emailSend, CustomerContact customerContact);
        void SendSignupEmailBackInStockConfirmEmail(TRMEmailPage emailPage, string emailAddress, string lastName, TrmVariant variant);
        bool SendEmailNotifyBackInStock(TRMEmailPage emailPage, string emailAddress, string lastName, TrmVariant variant);
        void SendCommentApprovalEmail(TRMEmailPage commentApprovalPage, string toEmailAddress, string toDisplayName, string pageUrl, UserComment userComment);
        bool SendSpecialEventEmail(TRMEmailPage emailPage, string toEmailAddress, AppointmentResult appointment);
        void SendGlobalPurchaseLimitEmail(TRMEmailPage emailPage, string toEmail);
        void SendInvalidIndicativePricesEmail(TRMEmailPage emailPage, string emailAddresses, List<MetalPrice> metalPriceList);
        void SendForgottenUsername(string email, TRMEmailPage emailPage, string resetPasswordLink, List<string> usernameList);
        bool SendBullionEmail(string emailTemplateName, List<MailAddress> toAddresses, out string errorMessage, Dictionary<string, object> optionalParameters = null);
	    void SendSecurityQAChangedEmail(TRMEmailPage emailPage, CustomerContact customerContact);
        void SendNewDeviceLoginEmail(CustomerContact currentContact, bool isBullionAccount);
        bool SendSIPPSSASSWelcomeEmail(CustomerContact contact, TRMEmailPage emailTemplate, string resetPasswordLink, string username);
        bool SendPriceAlertEmail(TRMEmailPage emailPage, CustomerContact customerContact, string alertAtPrice,
            string indicativeGoldPrice, string customerFullName);
        TRMEmailPage GetBullionEmailPage(string emailTemplateName, out string errorMessage);
        void SendDormantFundsEmail(TRMEmailPage emailPage, CustomerContact contact);
        bool SendAutoInvestBullionEmail(TRMEmailPage emailPages, List<MailAddress> toAddresses, out string errorMessage, Dictionary<string, object> optionalParameters = null);
    }
}