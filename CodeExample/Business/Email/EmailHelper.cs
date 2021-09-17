using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Castle.Core.Internal;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Web;
using Hephaestus.Commerce.Shared.Models;
using log4net;
using Mediachase.Commerce.Customers;
using PricingAndTradingService.Models;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Models.Articles;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.EntityFramework.RmgOrders;
using TRM.Web.Models.Pages;
using TRM.Web.Models.SpecialEvents;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Services;
using static TRM.Web.Constants.StringConstants;
using Regex = System.Text.RegularExpressions.Regex;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Business.Email
{
    public class EmailHelper : IEmailHelper
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(EmailHelper));
        private readonly IAmResetTokenHelper _resetTokenHelper;
        private readonly IContentLoader _contentLoader;

        public EmailHelper(IAmResetTokenHelper resetTokenHelper, IContentLoader contentLoader)
        {
            _resetTokenHelper = resetTokenHelper;
            _contentLoader = contentLoader;
        }

        public void SendJobFailedEmail(TrmErrorEmailPage emailPage, string jobName, Exception ex)
        {
            SendJobFailedEmail(emailPage, jobName, ex.ToString());
        }

        public void SendJobFailedEmail(TrmErrorEmailPage emailPage, string jobName, string ex)
        {
            var emailSettings = GetEmailSettings();

            var subject = string.Format(emailPage.Subject.Replace("[_JobName]", jobName));
            var to = new MailAddress { Address = emailPage.ToEmailAddresses };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };
            var emailBody = emailPage.Body.ToString();
            emailBody = emailBody.Replace("[_JobName]", jobName);
            emailBody = emailBody.Replace("[_Exception]", ex);

            _logger.Info("Sending Job Failed Notification Email");

            if (!SendEmail(to, from, subject, emailBody, emailSettings, _logger))
            {
                _logger.Error($"Failed to send Job Failed Email to {emailPage.ToEmailAddresses} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public void SendEmailToContact(TRMEmailPage emailPage, string email, Dictionary<string, object> optionalParameters)
        {
            if (emailPage == null)
            {
                throw new ArgumentNullException("emailPage", "TRMEmailPage cannot be null");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentNullException("email", "email cannot be null or empty");
            }


            var to = new MailAddress { Address = email };
            var fromAddress = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var subject = string.Format(emailPage.Subject);

            var emailBody = emailPage.Body?.ToString();
            emailBody = emailBody ?? string.Empty;

            if (!optionalParameters.IsNullOrEmpty())
            {
                foreach (var param in optionalParameters)
                {
                    var contentValue = IsItemListingParam(param.Key)
                        ? BuildItemListingHtml(emailPage.ItemListing, param.Value)
                        : param.Value?.ToString();
                    emailBody = emailBody.Replace($"[{param.Key}]", contentValue);
                }
            }
            var emailToSend = string.Format(GetEmailToSendString(), SiteDefinition.Current.SiteUrl, emailPage.Header, emailBody);
            var emailSettings = GetEmailSettings();

            var logMessage = $"Sending Email to {email} with template from PageID {emailPage.PageLink.ID}";
            _logger.Info(logMessage);

            if (!SendEmail(to, fromAddress, subject, emailToSend, emailSettings, _logger))
            {
                _logger.Error($"[FAILED]: {logMessage}");
            }
        }

        public void SendEmailForKycStatusChange(TRMEmailPage emailPage, CustomerContact contact)
        {
            if (emailPage == null)
            {
                throw new ArgumentNullException("emailPage", "TRMEmailPage cannot be null");
            }

            if (contact == null)
            {
                throw new ArgumentNullException("contact", "contact cannot be null");
            }

            var emailSettings = GetEmailSettings();

            var title = contact.GetStringProperty(StringConstants.CustomFields.ContactTitleField);
            var firstName = contact.FirstName;
            var lastName = contact.LastName;
            var email = contact.Email;

            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress { Address = email };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };
            var emailBody = emailPage.Body?.ToString();
            emailBody = emailBody?.Replace("[TITLE]", HttpUtility.HtmlEncode(title));
            emailBody = emailBody?.Replace("[FIRSTNAME]", HttpUtility.HtmlEncode(firstName));
            emailBody = emailBody?.Replace("[LASTNAME]", HttpUtility.HtmlEncode(lastName));

            var logMessage = $"Sending Email to {email} with template from PageID {emailPage.PageLink.ID}";
            _logger.Info(logMessage);

            if (!SendEmail(to, from, subject, emailBody, emailSettings, _logger))
            {
                _logger.Error($"[FAILED]: {logMessage}");
            }
        }

        public void SendEmailForMissingPampQuoteIds(TRMEmailPage emailPage, string toEmailAddress, string content)
        {
            if (emailPage == null)
            {
                throw new ArgumentNullException("emailPage", "TRMEmailPage cannot be null");
            }

            if (string.IsNullOrEmpty(toEmailAddress))
            {
                throw new ArgumentNullException("toEmailAddress", "toEmailAddress cannot be null or empty");
            }

            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException("content", "content cannot be null or empty");
            }

            var emailSettings = GetEmailSettings();

            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress { Address = toEmailAddress };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };
            var emailBody = emailPage.Body?.ToString();
            emailBody = emailBody.Replace("[_CONTENT]", content);

            var logMessage = $"Sending Email to {toEmailAddress} with template from PageID {emailPage.PageLink.ID}";
            _logger.Info(logMessage);

            if (!SendEmail(to, from, subject, emailBody, emailSettings, _logger))
            {
                _logger.Error($"[FAILED]: {logMessage}");
            }
        }

        public void SendNewsletterSignUpConsentEmail(TRMEmailPage newsletterSignUpConsentEmail, string email, int newsLetterSubscriptionId, string newsletterSignUpPageUrl)
        {
            var subject = string.Format(newsletterSignUpConsentEmail.Subject);
            var to = new MailAddress { Address = email };
            var from = new MailAddress
            {
                Address = newsletterSignUpConsentEmail.FromEmailAddress,
                DisplayName = newsletterSignUpConsentEmail.FromDisplayName
            };

            var emailBody = newsletterSignUpConsentEmail.Body.ToString();
            var url = $"{newsletterSignUpPageUrl}?email={email}&newsletterID={newsLetterSubscriptionId}";
            var urlHtml = $"Your newsletter sign-up consent link is available <a href=\"{url}\">here</a>.";

            emailBody = emailBody.Replace("[PAGEURL]", urlHtml);

            var settings = GetEmailSettings();
            _logger.Info("Sending Forgotten Password Email");

            if (!SendEmail(to, from, subject, emailBody, settings, _logger))
            {
                _logger.Error($"Failed to send Newsletter Sign-Up Email to {email} with template from PageID {newsletterSignUpConsentEmail.PageLink.ID}");
            }
        }

        public void SendRegistrationConfirmationEmail(TRMEmailPage emailPage, CustomerContact customerContact)
        {
            var subject = string.Format(emailPage.Subject);

            var to = new MailAddress
            {
                Address = customerContact.Email
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body.ToString();

            var address = customerContact.ContactAddresses.FirstOrDefault();
            var title = customerContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField);
            var telephone = customerContact.GetStringProperty(StringConstants.CustomFields.Telephone);

            emailBody = emailBody.Replace("[_AccountName]", $"{HttpUtility.HtmlEncode(title)} {HttpUtility.HtmlEncode(customerContact.LastName)}");
            emailBody = emailBody.Replace("[_Title]", HttpUtility.HtmlEncode(title));
            emailBody = emailBody.Replace("[_FirstName]", HttpUtility.HtmlEncode(customerContact.FirstName));
            emailBody = emailBody.Replace("[_LastName]", HttpUtility.HtmlEncode(customerContact.LastName));
            emailBody = emailBody.Replace("[_EmailAddress]", HttpUtility.HtmlEncode(customerContact.Email));

            emailBody = emailBody.Replace("[_DateOfBirth]", customerContact.BirthDate?.ToString("dd/MM/yy") ?? string.Empty);
            emailBody = emailBody.Replace("[_Telephone]", HttpUtility.HtmlEncode(telephone));

            if (address != null)
            {

                var addressString = address.Line1;

                if (!String.IsNullOrEmpty(address.Line2))
                    addressString = $"{addressString} <br /> {address.Line2}";

                if (!String.IsNullOrEmpty(address.City))
                    addressString = $"{addressString} <br /> {address.City}";

                if (!String.IsNullOrEmpty(address.PostalCode))
                    addressString = $"{addressString} <br /> {address.PostalCode}";

                if (!String.IsNullOrEmpty(address.CountryName))
                    addressString = $"{addressString} <br /> {address.CountryName}";

                emailBody = emailBody.Replace("[_Address]", addressString);
            }

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Registration Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Registration Email to {customerContact.Email} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public void SendRegistrationConfirmationEmail(TRMEmailPage emailPage, ApplicationUser user)
        {
            var subject = string.Format(emailPage.Subject);

            var to = new MailAddress
            {
                Address = user.Email
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body.ToString();

            var address = user.Addresses.FirstOrDefault();

            emailBody = emailBody.Replace("[_AccountName]", $"{HttpUtility.HtmlEncode(user.Title)} {HttpUtility.HtmlEncode(user.LastName)}");
            emailBody = emailBody.Replace("[_Title]", HttpUtility.HtmlEncode(user.Title));
            emailBody = emailBody.Replace("[_FirstName]", HttpUtility.HtmlEncode(user.FirstName));
            emailBody = emailBody.Replace("[_LastName]", HttpUtility.HtmlEncode(user.LastName));
            emailBody = emailBody.Replace("[_EmailAddress]", HttpUtility.HtmlEncode(user.Email));

            emailBody = emailBody.Replace("[_DateOfBirth]", user.BirthDate?.ToString("dd/MM/yy") ?? string.Empty);

            emailBody = emailBody.Replace("[_Telephone]", HttpUtility.HtmlEncode(user.PhoneNumber));

            if (address != null)
            {
                var addressString = address.Line1;

                if (!String.IsNullOrEmpty(address.Line2))
                    addressString = $"{addressString} <br /> {address.Line2}";

                if (!String.IsNullOrEmpty(address.City))
                    addressString = $"{addressString} <br /> {address.City}";

                if (!String.IsNullOrEmpty(address.PostalCode))
                    addressString = $"{addressString} <br /> {address.PostalCode}";

                if (!String.IsNullOrEmpty(address.CountryName))
                    addressString = $"{addressString} <br /> {address.CountryName}";

                emailBody = emailBody.Replace("[_Address]", addressString);
            }

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Registration Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Registration Email to {user.Email} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public void SendOrderConfirmationEmail(TRMEmailPage orderConfirmationEmail, PurchaseOrderViewModel purchaseOrder, CustomerContact customerContact)
        {
            var to = new MailAddress
            {
                // needs amending later on
                Address = customerContact.Email
            };
            var from = new MailAddress
            {
                Address = orderConfirmationEmail.FromEmailAddress,
                DisplayName = orderConfirmationEmail.FromDisplayName
            };
            //  render email body as string
            var emailBody = orderConfirmationEmail.Body.ToString();

            var title = customerContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField);
            var lastName = customerContact.LastName;

            var orderNumbers = string.Empty;

            var firstOrder = true;

            foreach (var number in purchaseOrder.OrderNumbers)
            {
                if (firstOrder)
                {
                    firstOrder = false;
                    orderNumbers = number;
                }
                else
                {
                    orderNumbers = $"{orderNumbers}, {number}";
                }
            }

            var subject = $"{orderConfirmationEmail.Subject} - {orderNumbers}";

            var deliveryAddress = AddressToHtml(purchaseOrder.Shipments.FirstOrDefault()?.Address, true);
            var billingAddress = AddressToHtml(purchaseOrder.Payments.FirstOrDefault()?.Address);

            emailBody = emailBody.Replace("[_OrderNumber]", orderNumbers);
            emailBody = emailBody.Replace("[_OrderDate]", purchaseOrder.Modified?.ToShortDateString() ?? string.Empty);
            emailBody = emailBody.Replace("[_User]", $"{title} {lastName}");
            emailBody = emailBody.Replace("[_BillingAddress]", billingAddress);
            emailBody = emailBody.Replace("[_DeliveryAddress]", deliveryAddress);
            emailBody = emailBody.Replace("[_SubTotal]", HttpUtility.HtmlEncode(purchaseOrder.SubTotal));
            emailBody = emailBody.Replace("[_DeliveryTotal]", HttpUtility.HtmlEncode(purchaseOrder.TotalDelivery));
            emailBody = emailBody.Replace("[_Savings]", HttpUtility.HtmlEncode(purchaseOrder.TotalDiscount));
            emailBody = emailBody.Replace("[_Total]", HttpUtility.HtmlEncode(purchaseOrder.Total));
            emailBody = emailBody.Replace("<!-- Product Item Row --><!-- Product Item Row Ends -->", OrderItemsToHtml(purchaseOrder));


            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, orderConfirmationEmail.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Order Confirmation Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Order Confirmation Email to {customerContact.Email} with template from PageID {orderConfirmationEmail.PageLink.ID}");
            }
        }

        public void SendWishListConfirmationEmail(TRMEmailPage wishListConfirmationEmail, List<CartItemViewModel> wishList, string emailSend, CustomerContact customerContact)
        {
            var to = new MailAddress
            {
                Address = emailSend
            };
            var from = new MailAddress
            {
                Address = wishListConfirmationEmail.FromEmailAddress,
                DisplayName = wishListConfirmationEmail.FromDisplayName
            };
            var subject = wishListConfirmationEmail.Subject;
            var emailBody = wishListConfirmationEmail.Body.ToString();

            var title = customerContact
                .Properties[StringConstants.CustomFields.ContactTitleField].Value;
            var lastName = customerContact.LastName;

            emailBody = emailBody.Replace("[_Title]", HttpUtility.HtmlEncode(title));
            emailBody = emailBody.Replace("[_FirstName]", HttpUtility.HtmlEncode(customerContact.FirstName));
            emailBody = emailBody.Replace("[_LastName]", HttpUtility.HtmlEncode(lastName));
            emailBody = emailBody.Replace("[_User]", to.Address);
            emailBody = emailBody.Replace("<!-- Product Item Row --><!-- Product Item Row Ends -->", WishListToHtml(wishList));

            var otherUrl = SiteDefinition.Current.SiteUrl;

            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, wishListConfirmationEmail.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending WishList Confirmation Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send WishList Confirmation Email to {emailSend} with template from PageID {wishListConfirmationEmail.PageLink.ID}");
            }
        }

        public void SendAccountDetailsChangedEmail(TRMEmailPage accountDetailsChangedEmail, string emailAddress, List<string> changedFields, List<string> oldValues, List<string> newValues, string accountName)
        {
            var subject = accountDetailsChangedEmail.Subject;

            var to = new MailAddress
            {
                Address = emailAddress
            };

            var from = new MailAddress
            {
                Address = accountDetailsChangedEmail.FromEmailAddress,
                DisplayName = accountDetailsChangedEmail.FromDisplayName
            };

            var emailBody = accountDetailsChangedEmail.Body.ToString();

            emailBody = emailBody.Replace("[_AccountName]", HttpUtility.HtmlEncode(accountName));

            var changes = new StringBuilder();

            var i = 0;

            var defaultItemHtml = LocalizationService.Current.GetStringByCulture(
              StringResources.AccountAmendmentItem, "<p>The registered {0} has been changed from <span style=\"color:#9b834f; text-decoration: underline;\">{1}</span> to <span style=\"color:#9b834f; text-decoration: underline;\">{2}</span>.</p>", ContentLanguage.PreferredCulture);

            foreach (var change in changedFields)
            {
                changes.Append(
                    string.Format(defaultItemHtml,
                        HttpUtility.HtmlEncode(change), HttpUtility.HtmlEncode(oldValues[i]), HttpUtility.HtmlEncode(newValues[i])));
                i++;
            }

            emailBody = emailBody.Replace("[_AccountChanges]", changes.ToString());

            var otherUrl = SiteDefinition.Current.SiteUrl;

            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, accountDetailsChangedEmail.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Account Details Changed Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Account Details Changed Email to {emailAddress} with template from PageID {accountDetailsChangedEmail.PageLink.ID}");
            }
        }

        public void SendSovereignCertificateSignUpEmail(TRMEmailPage certificateSignUpEmail, SovereignCertificateSignUpDto signUp)
        {
            var subject = string.Format(certificateSignUpEmail.Subject);
            var to = new MailAddress
            {
                Address = signUp.EmailAddress
            };
            var from = new MailAddress
            {
                Address = certificateSignUpEmail.FromEmailAddress,
                DisplayName = certificateSignUpEmail.FromEmailAddress
            };

            var emailBody = certificateSignUpEmail.Body.ToString();
            emailBody = emailBody.Replace("[_Name]", $"{signUp.FirstName} {signUp.Surname}");

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, certificateSignUpEmail.Header, emailBody);
            var settings = GetEmailSettings();

            _logger.Info("Sending Sovereign Certification SignUp Confirmation Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Forgotten Password Email to {signUp.EmailAddress} with template from PageID {certificateSignUpEmail.PageLink.ID}");
            }
        }

        public void SendGdprConfirmEmail(TRMEmailPage emailPage, string emailAddress, string lastName)
        {
            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress
            {
                Address = emailAddress
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body?.ToString() ?? "Dear [_User]";
            emailBody = emailBody.Replace("[_User]", lastName);

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending GDPR Confirmation Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send GDPR Confirmation Email to {emailAddress} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public void SendGdprIdentityEmail(TRMEmailPage emailPage, string emailAddress, string lastName, string pageUrl)
        {
            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress
            {
                Address = emailAddress
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body?.ToString() ?? "Dear [_User]";
            emailBody = emailBody.Replace("[_User]", lastName);
            emailBody = emailBody.Replace("[PAGEURL]", pageUrl);

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending GDPR Identity Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send GDPR Identity Email to {emailAddress} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public void SendSignupEmailBackInStockConfirmEmail(TRMEmailPage emailPage, string emailAddress, string lastName, TrmVariant variant)
        {
            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress
            {
                Address = emailAddress
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body?.ToString() ?? "Dear [_User]";
            emailBody = emailBody.Replace("[_User]", lastName);
            emailBody = emailBody.Replace("[_ProductCode]", variant.Code);
            emailBody = emailBody.Replace("[_ProductName]", variant.DisplayName);

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Signup Email Back In Stock Confirmation Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Signup Email Back In Stock Confirmation Email to {emailAddress} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public bool SendEmailNotifyBackInStock(TRMEmailPage emailPage, string emailAddress, string lastName, TrmVariant variant)
        {
            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress
            {
                Address = emailAddress
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body?.ToString() ?? "Dear [_User]";
            emailBody = emailBody.Replace("[_User]", lastName);
            emailBody = emailBody.Replace("[_ProductCode]", variant.Code);
            emailBody = emailBody.Replace("[_ProductName]", variant.DisplayName);
            emailBody = emailBody.Replace("[_Subtitle]", variant.SubDisplayName);
            emailBody = emailBody.Replace("[_Url]", variant.ContentLink.GetExternalUrl_V2());

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Email Notify Back In Stock");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Email Notify Back In Stock to {emailAddress} with template from PageID {emailPage.PageLink.ID}");
                return false;
            }

            return true;
        }

        public void SendInvalidIndicativePricesEmail(TRMEmailPage emailPage, string emailAddresses, List<MetalPrice> metalPriceList)
        {
            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress
            {
                Address = emailAddresses
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var formattedPrices = metalPriceList.Select(x => $"<div>Currency/Metal: {x.CurrencyPair}, Buy Price: {x.BuyPrice}, Sell Price: {x.SellPrice}</div>");

            var emailBody = emailPage.Body?.ToString() ?? "[_Prices]";
            emailBody = emailBody.Replace("[_Prices]", string.Join(string.Empty, formattedPrices));

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Invalid Indicative Prices Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Invalid Indicative Prices Email to {emailAddresses} with template from PageID {emailPage.PageLink.ID}");
            }
        }
        private string AddressToHtml(RmgOrder order)
        {
            var address = new AddressModel();
            address.Line1 = order.Address1;
            address.Line2 = order.Address2;
            address.City = order.City;
            address.CountryRegion.Region = order.County;
            address.CountryCode = order.Country;
            address.PostalCode = order.Postcode;

            return AddressToHtml(address);
        }

        private string AddressToHtml(AddressModel address, bool delivery = false)
        {
            if (address == null) return string.Empty;

            const string br = "<br />";

            var stringBuilder = new StringBuilder();

            if (delivery && !string.IsNullOrWhiteSpace(address.Organization))
            {
                stringBuilder.AppendLine($"{HttpUtility.HtmlEncode(address.Organization)} {br}");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(address.FirstName) ||
                    !string.IsNullOrWhiteSpace(address.LastName))
                {
                    stringBuilder.AppendLine(
                        $"{HttpUtility.HtmlEncode(address.FirstName)} {HttpUtility.HtmlEncode(address.LastName)} {br}");
                }
            }

            if (!string.IsNullOrWhiteSpace(address.Line1))
            {
                stringBuilder.AppendLine(HttpUtility.HtmlEncode(address.Line1) + br);
            }

            if (!string.IsNullOrWhiteSpace(address.Line2))
            {
                stringBuilder.AppendLine(HttpUtility.HtmlEncode(address.Line2) + br);
            }

            if (!string.IsNullOrWhiteSpace(address.City))
            {
                stringBuilder.AppendLine(HttpUtility.HtmlEncode(address.City) + br);
            }

            if (!string.IsNullOrWhiteSpace(address.CountryRegion.Region))
            {
                stringBuilder.AppendLine(HttpUtility.HtmlEncode(address.CountryRegion.Region) + br);
            }

            if (!string.IsNullOrWhiteSpace(address.PostalCode))
            {
                stringBuilder.AppendLine(HttpUtility.HtmlEncode(address.PostalCode) + br);
            }

            if (!string.IsNullOrWhiteSpace(address.CountryName))
            {
                stringBuilder.AppendLine(HttpUtility.HtmlEncode(address.CountryName) + br);
            }

            return stringBuilder.ToString();
        }

        private string OrderItemsToHtml(IOrderGroupViewModel purchaseOrder)
        {
            var items = new StringBuilder();

            var defaultItemHtml = LocalizationService.Current.GetStringByCulture(
                StringResources.OrderEmailItem, @"<tr>
                    <td>[_DisplayName]<br /> 
                    <p style='margin-bottom: 5px;'><span style='font-size: 14px; color: #777777;'>[_SubTitle]<br /></span></p>
                    [_HasBeenPersonalisedMessage]<br />
                    <span style='font-size: 12px;'>[_ShippingMessage]<br /></span> <span style= 'font-size: 12px;'>[_RecurringCheckoutMessage]</span></td>
                    <td><a style = 'color:#9b834f; text-decoration: underline;' href ='[_Url]' title = '[_DisplayName]' >[_Code]</a></td>
                    <td style='text-align: center;'>[_Quantity]</td>
                    <td>[_DiscountedPrice]</td>
                    </tr> 
                    <tr>
                    <td colspan='4'><hr style='border-color: #d6d6d6;'/></td>
                    </tr>", ContentLanguage.PreferredCulture);
            var personalisedMessage = $"<span class='bg-lightgray p-1xs mr-1x' style='display: inline-block'>{LocalizationService.Current.GetStringByCulture(StringResources.HasBeenPersonalised, TranslationFallback.HasBeenPersonalised, ContentLanguage.PreferredCulture)}</span>";

            foreach (var shipment in purchaseOrder.Shipments)
            {
                foreach (var item in shipment.CartItems)
                {
                    var itemHtml = defaultItemHtml;

                    itemHtml = itemHtml.Replace("[_DisplayName]", item.DisplayName);
                    itemHtml = itemHtml.Replace("[_SubTitle]", item.SubTitle);
                    itemHtml = itemHtml.Replace("[_HasBeenPersonalisedMessage]", item.HasbeenPersonalised ? personalisedMessage : string.Empty);
                    itemHtml = itemHtml.Replace("[_ShippingMessage]", item.StockSummary.ShippingMessage);
                    itemHtml = itemHtml.Replace("[_RecurringDetailsMessage]", item.RecurringDetailsMessage);
                    itemHtml = itemHtml.Replace("[_RecurringCheckoutMessage]", item.RecurringCheckoutMessage);
                    itemHtml = itemHtml.Replace("[_RecurringConfirmationMessage]", item.RecurringConfirmationMessage);
                    itemHtml = itemHtml.Replace("[_Code]", item.Code);
                    itemHtml = itemHtml.Replace("[_Url]", item.Entry.GetExternalUrl_V2());
                    itemHtml = itemHtml.Replace("[_Quantity]", item.Quantity.ToString("0.#", CultureInfo.InvariantCulture));
                    itemHtml = itemHtml.Replace("[_DiscountedPrice]", item.DiscountedPrice);

                    items.Append(itemHtml);
                }
            }

            return items.ToString();
        }

        private string WishListToHtml(List<CartItemViewModel> wishList)
        {
            var items = new StringBuilder();
            var defaultItemHtml = LocalizationService.Current.GetStringByCulture(
                StringResources.WishListEmailItem, @"<tr><td><a href='[_Url]' title='[_DisplayName]' ><img width='120' height='120' src='[_Image]'></a></td>
                       <td><a style = 'color:#9b834f; text-decoration: underline;' href ='[_Url]' title = '[_DisplayName]' >[_DisplayName]</a> <br/>
                       <p style = 'margin-bottom: 5px;' ><span style = 'font-size: 14px; color:#777777'>[_SubTitle]<br/></ span ></p></td>
                <td>[_DiscountedPrice]</ td >
                </tr>", ContentLanguage.PreferredCulture);


            foreach (var product in wishList)
            {
                var itemHtml = defaultItemHtml;

                itemHtml = itemHtml.Replace("[_DisplayName]", product.DisplayName);
                itemHtml = itemHtml.Replace("[_SubTitle]", product.SubTitle);
                itemHtml = itemHtml.Replace("[_ShippingMessage]", product.StockSummary.ShippingMessage);
                itemHtml = itemHtml.Replace("[_RecurringCheckoutMessage]", product.RecurringCheckoutMessage);
                itemHtml = itemHtml.Replace("[_Code]", product.Code);
                itemHtml = itemHtml.Replace("[_DiscountedPrice]", product.DiscountedPrice);
                itemHtml = itemHtml.Replace("[_Url]", product.Entry.GetExternalUrl_V2());
                itemHtml = itemHtml.Replace("[_Image]", product.ImageUrl);

                items.Append(itemHtml);
            }

            return items.ToString();
        }

        private Dictionary<string, string> GetEmailSettings()
        {
            return new Dictionary<string, string>
            {
                {"Label", "SendEmailRequest"}
            };
        }

        private string GetEmailToSendString()
        {
            return "<!doctype html><html xmlns =\"http://www.w3.org/1999/xhtml\"  xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\"<head><base href=\"{0}\">{1}</head>{2}</html>";
        }

        private bool SendEmail(MailAddress to, MailAddress from, string subject, string emailToSend, Dictionary<string, string> settings, ILog logger)
        {
            var transmissionType = Enums.EmailTransmissionType.Msmq;
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            if (startPage != null)
                transmissionType = startPage.TransmissionType;

            var emailService = new EmailService(transmissionType);

            try
            {
                emailService.SendEmail(new List<MailAddress> { to }, from, null, null, subject, emailToSend, true, settings);

                return true;
            }
            catch (Exception e)
            {
                logger.Error("Email Service failed to send email", e);

                return false;
            }
        }

        private bool SendEmail(List<MailAddress> toAddressses, MailAddress from, string subject, string emailToSend, Dictionary<string, string> settings, ILog logger)
        {
            var emailService = new EmailService(from.GetAppropriateStartPageForSiteSpecificProperties().TransmissionType);

            try
            {
                emailService.SendEmail(toAddressses, from, null, null, subject, emailToSend, true, settings);

                return true;
            }
            catch (Exception e)
            {
                logger.Error("Email Service failed to send email", e);

                return false;
            }
        }

        public void SendRmgConfirmationEmail(TRMEmailPage rmgOrderConfirmation, RmgOrder order)
        {
            var to = new MailAddress
            {
                // needs amending later on
                Address = order.EmailAddress
            };
            var from = new MailAddress
            {
                Address = rmgOrderConfirmation.FromEmailAddress,
                DisplayName = rmgOrderConfirmation.FromDisplayName
            };
            //  render email body as string
            var emailBody = rmgOrderConfirmation.Body.ToString();

            var title = order.Title;
            var lastName = order.LastName;

            var orderNumbers = order.OrderNumber;
            var billingAddress = AddressToHtml(order);
            var subject = $"{rmgOrderConfirmation.Subject} - {orderNumbers}";

            emailBody = emailBody.Replace("[_OrderNumber]", orderNumbers);
            emailBody = emailBody.Replace("[_OrderDate]", order.CreatedDate.ToShortDateString());
            emailBody = emailBody.Replace("[_User]", $"{title} {lastName}");
            emailBody = emailBody.Replace("[_RmgTotal]", HttpUtility.HtmlEncode("£ " + order.Amount.ToString("#,##0.00")));
            emailBody = emailBody.Replace("[_Charges]", HttpUtility.HtmlEncode("£ " + order.PremiumAmount.ToString("#,##0.00")));
            emailBody = emailBody.Replace("[_Total]", HttpUtility.HtmlEncode("£ " + order.Total.ToString("#,##0.00")));
            emailBody = emailBody.Replace("[_Wallet]", HttpUtility.HtmlEncode(order.WalletId));
            emailBody = emailBody.Replace("[_BillingAddress]", billingAddress);

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, rmgOrderConfirmation.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Rmg Order Confirmation Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Order Confirmation Email to {order.EmailAddress} with template from PageID {rmgOrderConfirmation.PageLink.ID}");
            }
        }

        public void SendCommentApprovalEmail(TRMEmailPage commentApprovalPage, string toEmailAddress, string toEmailDisplayName, string pageUrl, UserComment userComment)
        {
            var subject = string.Format(commentApprovalPage.Subject);

            var to = new MailAddress
            {
                DisplayName = toEmailDisplayName,
                Address = toEmailAddress
            };
            var from = new MailAddress
            {
                Address = commentApprovalPage.FromEmailAddress,
                DisplayName = commentApprovalPage.FromDisplayName
            };

            var emailBody = commentApprovalPage.Body.ToString();
            emailBody = emailBody.Replace("[PAGEURL]", pageUrl);
            emailBody = emailBody.Replace("[COMMENT_AT]", userComment.CommentAt.ToString("MM/dd/yyyy h:mm tt"));
            emailBody = emailBody.Replace("[CONTACT_NAME]", userComment.ContactName);
            emailBody = emailBody.Replace("[MESSAGE]", userComment.Message);
            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, commentApprovalPage.Header, emailBody);
            var settings = GetEmailSettings();

            _logger.Info("Sending Comment Approval Email");
            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send comment approval message to {to.Address} with template from PageID {commentApprovalPage.PageLink.ID}");
            }
        }

        public bool SendSpecialEventEmail(TRMEmailPage emailPage, string toEmailAddress, AppointmentResult appointment)
        {
            var subject = string.Format(emailPage.Subject);

            var to = new MailAddress
            {
                DisplayName = appointment.ContactName,
                Address = toEmailAddress
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body.ToString();
            emailBody = emailBody.Replace("[EVENT_NAME]", appointment.Name);
            emailBody = emailBody.Replace("[EVENT_DATE]", appointment.Date);
            emailBody = emailBody.Replace("[CONTACT_NAME]", appointment.ContactName);
            emailBody = emailBody.Replace("[EVENT_TYPE]", appointment.EventTypeName);
            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);
            var settings = GetEmailSettings();

            _logger.Info("Sending Special Event Email");
            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send special event to {to.Address} with template from PageID {emailPage.PageLink.ID}");
                return false;
            }
            return true;
        }

        public bool SendSIPPSSASSWelcomeEmail(CustomerContact contact, TRMEmailPage emailTemplate, string resetPasswordLink, string username)
        {
            var to = new MailAddress
            {
                Address = contact.Email
            };
            var from = new MailAddress
            {
                Address = emailTemplate.FromEmailAddress,
                DisplayName = emailTemplate.FromDisplayName
            };

            var token = _resetTokenHelper.CreatePasswordResetToken(username);
            var passwordResetUrl = $"{resetPasswordLink}?username={HttpUtility.UrlEncode(username)}&token={token}";

            var emailBody = HttpUtility.HtmlDecode(emailTemplate.Body.ToString());
            emailBody = emailBody.Replace("<name>", contact.FullName);
            emailBody = emailBody.Replace("<password_reset_url>", passwordResetUrl);
            emailBody = emailBody.Replace("<username>", username);

            return SendEmail(to, from, emailTemplate.Subject, emailBody, GetEmailSettings(), _logger);
        }

        public void SendForgottenUsername(string emailAddress, TRMEmailPage emailTemplate, string resetPasswordLink, List<string> usernameList)
        {
            var subject = string.Format(emailTemplate.Subject);
            var to = new MailAddress
            {
                Address = emailAddress
            };
            var from = new MailAddress
            {
                Address = emailTemplate.FromEmailAddress,
                DisplayName = emailTemplate.FromDisplayName
            };
            StringBuilder stringBuilder = new StringBuilder();
            var emailBody = emailTemplate.Body.ToString();
            var formatCorrectTempalate = "[USERNAME]{0} - [PAGEURL] Click here to reset the password for {0}[/PAGEURL][/USERNAME]";
            //[USERNAME]martin1 - click here to reset the password for martin1[/USERNAME]
            int openUsernameIndex = emailBody.IndexOf("[USERNAME]");
            int closeUsernameIndex = emailBody.IndexOf("[/USERNAME]");
            if (openUsernameIndex == -1 || closeUsernameIndex == -1)
            {
                _logger.Error($"Failed to send Forgotten Password or Username to {emailAddress} with template from PageID {emailTemplate.PageLink.ID}: We need correct formart as {formatCorrectTempalate}");
            }

            var usernameTemplate = (openUsernameIndex == -1 || closeUsernameIndex == -1) ? string.Empty : emailBody.Substring(openUsernameIndex + "[USERNAME]".Length, closeUsernameIndex - openUsernameIndex - "[USERNAME]".Length);
            int openPageUrlIndex = usernameTemplate.IndexOf("[PAGEURL]");
            int closePageUrlIndex = usernameTemplate.IndexOf("[/PAGEURL]");
            if (openPageUrlIndex == -1 || closePageUrlIndex == -1)
            {
                _logger.Error($"Failed to send Forgotten Password or Username to {emailAddress} with template from PageID {emailTemplate.PageLink.ID}: We need correct formart as {formatCorrectTempalate}");
            }

            var pageUrlTemplate = (openPageUrlIndex == -1 || closePageUrlIndex == -1) ? string.Empty : usernameTemplate.Substring(openPageUrlIndex + "[PAGEURL]".Length, closePageUrlIndex - openPageUrlIndex - "[PAGEURL]".Length);

            if (!string.IsNullOrEmpty(pageUrlTemplate) && !string.IsNullOrEmpty(usernameTemplate))
            {
                usernameList = usernameList.OrderBy(x => x).ToList();
                foreach (var username in usernameList)
                {
                    //link forgot
                    var usernameForLink = HttpUtility.UrlEncode(username);
                    var token = !string.IsNullOrEmpty(username) ? _resetTokenHelper.CreatePasswordResetToken(username) : string.Empty;
                    var url = $"{resetPasswordLink}?username={usernameForLink}&token={token}";
                    var textPasswordUrl = string.Format(pageUrlTemplate, username);
                    var contentPassword = $"<a href=\"{url}\">{textPasswordUrl}</a>";
                    var usernameContent = usernameTemplate.Replace($"[PAGEURL]{pageUrlTemplate}[/PAGEURL]", contentPassword);

                    stringBuilder.Append(string.Format(usernameContent, username));
                }
                emailBody = emailBody.Replace($"[USERNAME]{usernameTemplate}[/USERNAME]", stringBuilder.ToString());
            }

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailTemplate.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Forgotten Username");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Forgotten Username to {emailAddress} with template from PageID {emailTemplate.PageLink.ID}");
            }
        }

        public void SendGlobalPurchaseLimitEmail(TRMEmailPage emailPage, string toEmail)
        {
            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress { Address = toEmail };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body.ToString();
            var settings = GetEmailSettings();
            _logger.Info("Sending warning email for global purchase limits.");

            if (!SendEmail(to, from, subject, emailBody, settings, _logger))
            {
                _logger.Error($"Failed to send Global Purchase Limits email to {toEmail} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public TRMEmailPage GetBullionEmailPage(string emailTemplateName, out string errorMessage)
        {
            errorMessage = string.Empty;
            var bullionEmailsRoot = _contentLoader.GetAppropriateStartPageForSiteSpecificProperties()?.BullionEmailsRoot;

            if (bullionEmailsRoot == null)
            {
                errorMessage = ContentSetupErrors.EmailsRootNull;
                return null;
            }
            var allBullionEmails = GetAllEmailPages(bullionEmailsRoot);
            var emailTemplate = allBullionEmails.FirstOrDefault(x => x.Name.Equals(emailTemplateName));
            if (emailTemplate != null) return emailTemplate;
            errorMessage = string.Format(ContentSetupErrors.EmailTemplateCouldNotBeFound, emailTemplateName);
            return null;
        }

        public bool SendAutoInvestBullionEmail(TRMEmailPage emailPages, List<MailAddress> toAddresses, out string errorMessage, Dictionary<string, object> optionalParameters = null)
        {
            errorMessage = string.Empty;
            if (emailPages == null) return false;

            var body = emailPages.Body?.ToString();
            body = body ?? string.Empty;

            if (!optionalParameters.IsNullOrEmpty())
            {
                foreach (var param in optionalParameters)
                {
                    var contentValue = IsItemListingParam(param.Key)
                        ? BuildItemListingHtml(emailPages.ItemListing, param.Value)
                        : param.Value?.ToString();
                    body = body.Replace($"[{param.Key}]", contentValue);
                }
            }
            var emailToSend = string.Format(GetEmailToSendString(), SiteDefinition.Current.SiteUrl, emailPages.Header, body);
            var settings = GetEmailSettings();
            var fromAddress = new MailAddress
            {
                Address = emailPages.FromEmailAddress,
                DisplayName = emailPages.FromDisplayName
            };

            if (!SendEmail(toAddresses, fromAddress, emailPages.Subject, emailToSend, settings, _logger))
            {
                _logger.ErrorFormat("Failed to send auto invest confirmation email to {0}", string.Join(";", toAddresses));
                errorMessage = ContentSetupErrors.MailFailed;
                return false;
            }
            return true;
        }
        public bool SendBullionEmail(string emailTemplateName, List<MailAddress> toAddresses, out string errorMessage, Dictionary<string, object> optionalParameters = null)
        {
            errorMessage = string.Empty;
            var emailTemplate = GetBullionEmailPage(emailTemplateName, out errorMessage);

            if (emailTemplate == null)
            {
                return false;
            }

            var body = emailTemplate.Body?.ToString();
            body = body ?? string.Empty;

            if (!optionalParameters.IsNullOrEmpty())
            {
                foreach (var param in optionalParameters)
                {
                    var contentValue = IsItemListingParam(param.Key)
                        ? BuildItemListingHtml(emailTemplate.ItemListing, param.Value)
                        : param.Value?.ToString();
                    body = body.Replace($"[{param.Key}]", contentValue);
                }
            }
            var emailToSend = string.Format(GetEmailToSendString(), SiteDefinition.Current.SiteUrl, emailTemplate.Header, body);
            var settings = GetEmailSettings();
            var fromAddress = new MailAddress
            {
                Address = emailTemplate.FromEmailAddress,
                DisplayName = emailTemplate.FromDisplayName
            };

            if (!SendEmail(toAddresses, fromAddress, emailTemplate.Subject, emailToSend, settings, _logger))
            {
                _logger.ErrorFormat("Failed to send {0} email to {1}", emailTemplateName, string.Join(";", toAddresses));
                errorMessage = ContentSetupErrors.MailFailed;
                return false;
            }
            return true;
        }

        public void SendSecurityQAChangedEmail(TRMEmailPage emailPage, CustomerContact customerContact)
        {
            var to = new MailAddress
            {
                Address = customerContact.Email
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };
            var subject = emailPage.Subject;
            var emailBody = emailPage.Body.ToString();

            var title = customerContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField);
            var bullionObsAccountNumber = customerContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber);
            var lastName = customerContact.LastName;

            emailBody = emailBody.Replace("[TITLE]", HttpUtility.HtmlEncode(title));
            emailBody = emailBody.Replace("[FIRSTNAME]", HttpUtility.HtmlEncode(customerContact.FirstName));
            emailBody = emailBody.Replace("[LASTNAME]", HttpUtility.HtmlEncode(lastName));
            emailBody = emailBody.Replace("[ACCOUNTNUMBER]", bullionObsAccountNumber);


            var otherUrl = SiteDefinition.Current.SiteUrl;

            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            _logger.Info("Sending Security Q&A Changed Email");

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Security Q&A Changed Email to {customerContact.Email} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        public void SendNewDeviceLoginEmail(CustomerContact currentContact, bool isBullionAccount)
        {
            try
            {
                if (currentContact == null) return;

                var toAddresses = new List<MailAddress> { new MailAddress(currentContact.Email, currentContact.FullName) };
                var emailParams = GetNewDeviceLoginEmailParams(currentContact, isBullionAccount);
                string sendingEmailErrorMessage;
                if (SendBullionEmail(BullionEmailCategories.NewDeviceEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                _logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");

            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
        }

        public bool SendPriceAlertEmail(TRMEmailPage emailPage, CustomerContact customerContact, string alertAtPrice, string indicativeGoldPrice, string customerFullName)
        {
            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress
            {
                Address = customerContact.Email
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body?.ToString() ?? "Dear [_User]";

            emailBody = emailBody.Replace("[_User]", customerFullName);
            emailBody = emailBody.Replace("[_AlertAtPrice]", alertAtPrice);
            emailBody = emailBody.Replace("[_IndicativeGoldPrice]", indicativeGoldPrice);

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Price Alert Email to {customerContact.Email} with template from PageID {emailPage.PageLink.ID}");
                return false;
            }

            return true;
        }

        private Dictionary<string, object> GetNewDeviceLoginEmailParams(CustomerContact currentContact, bool isBullionAccount)
        {
            var result = new Dictionary<string, object>
            {
                { EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { EmailParameters.FirstName, currentContact.FirstName },
                { EmailParameters.LastName, currentContact.LastName },
                {EmailParameters.AccountNumber, isBullionAccount
                    ? currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber)
                    : currentContact.GetStringProperty(StringConstants.CustomFields.ObsAccountNumber)  }
            };

            return result;
        }

        private string BuildItemListingHtml(string itemListingTemplate, object value)
        {
            var itemKeyAndValueMappings = value as IEnumerable<Dictionary<string, string>>;
            if (string.IsNullOrEmpty(itemListingTemplate) || itemKeyAndValueMappings.IsNullOrEmpty()) return string.Empty;

            var itemKeyAndHeaderTextMappings = ExtractItemTableHeaders(itemListingTemplate, itemKeyAndValueMappings.First());
            var orderedItemKeyAndValueMappings = ExtractDictionaryWithOrderedKeys(itemKeyAndHeaderTextMappings.Keys, itemKeyAndValueMappings);

            return BuildTableHtml(itemKeyAndHeaderTextMappings, orderedItemKeyAndValueMappings);
        }

        private IEnumerable<Dictionary<string, string>> ExtractDictionaryWithOrderedKeys(IEnumerable<string> orderedKeys, IEnumerable<Dictionary<string, string>> source)
        {
            if (orderedKeys.IsNullOrEmpty() || source.IsNullOrEmpty()) return null;

            var result = new List<Dictionary<string, string>>();
            foreach (var orderLineItemMapping in source)
            {
                var orderLineItemKeyValueMapping = new Dictionary<string, string>();
                foreach (var key in orderedKeys)
                {
                    orderLineItemKeyValueMapping.Add(key, orderLineItemMapping.ContainsKey(key) ? orderLineItemMapping[key] : string.Empty);
                }
                result.Add(orderLineItemKeyValueMapping);
            }
            return result;
        }
        private string BuildTableHtml(Dictionary<string, string> headers, IEnumerable<Dictionary<string, string>> rowValues)
        {
            if (!rowValues.Any() || rowValues.First().Count() != headers.Count()) return string.Empty;
            StringWriter stringWriter = new StringWriter();
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Table);
                //RenderTableRowHtml(writer, headers, true);
                foreach (var item in rowValues)
                {
                    RenderTableRowHtml(writer, item, false);
                }
                writer.RenderEndTag();
            }
            return stringWriter.ToString();
        }

        private void RenderTableRowHtml(HtmlTextWriter wr, Dictionary<string, string> cellKeyValueMappings, bool isHeader = false)
        {
            if (wr == null) return;
            wr.RenderBeginTag(HtmlTextWriterTag.Tr);
            var cellTag = isHeader ? HtmlTextWriterTag.Th : HtmlTextWriterTag.Td;
            foreach (var cell in cellKeyValueMappings)
            {
                wr.RenderBeginTag(cellTag);
                if (!isHeader && cell.Key.Equals(EmailParameters.LineItemImage))
                {
                    wr.AddAttribute(HtmlTextWriterAttribute.Src, cell.Value);
                    wr.AddAttribute(HtmlTextWriterAttribute.Width, "200");
                    wr.AddAttribute(HtmlTextWriterAttribute.Style, "width: 100%;display: block;max-width: 200px;");
                    wr.RenderBeginTag(HtmlTextWriterTag.Img);
                    wr.RenderEndTag();
                }
                else
                {
                    wr.Write(cell.Value);
                }
                wr.RenderEndTag();
            }
            wr.RenderEndTag();
        }

        private Dictionary<string, string> ExtractItemTableHeaders(string itemListingTemplate, Dictionary<string, string> itemValueListings)
        {
            var itemRegex = new Regex(@"\[.*?\]");
            var allMatches = itemRegex.IsMatch(itemListingTemplate)
                ? itemRegex.Matches(itemListingTemplate).Cast<Match>().Where(x => x.Value.Length > 2).Select(x => x.Value.Substring(1, x.Value.Length - 2))
                : Enumerable.Empty<string>();

            if (allMatches.IsNullOrEmpty()) return new Dictionary<string, string>();
            var headerMappings = new Dictionary<string, string>();
            const char SPLITCHAR = ':';
            foreach (var headerTextAndItemKeyStr in allMatches)
            {
                if (!headerTextAndItemKeyStr.Contains(SPLITCHAR) && itemValueListings.All(x => x.Key != headerTextAndItemKeyStr))
                {
                    continue;
                }
                var headerTextAndItemKeyArray = headerTextAndItemKeyStr.Split(SPLITCHAR);
                var headerText = headerTextAndItemKeyArray.Last().Trim();
                var itemKey = headerTextAndItemKeyArray.First().Trim();
                if (!itemValueListings.ContainsKey(itemKey)) continue;
                headerMappings.Add(itemKey, headerText);
            }
            return headerMappings;
        }

        private bool IsItemListingParam(string paramKey)
        {
            return EmailParameters.ORDERITEMS.Equals(paramKey);
        }

        private IEnumerable<TRMEmailPage> GetAllEmailPages(ContentReference bullionEmailsRoot)
        {
            var allEmails = _contentLoader.GetChildren<TRMEmailPage>(bullionEmailsRoot);
            return allEmails.IsNullOrEmpty() ? Enumerable.Empty<TRMEmailPage>() : allEmails;
        }

        public void SendDormantFundsEmail(TRMEmailPage emailPage, CustomerContact contact)
        {
            var firstName = contact.FirstName;
            var lastName = contact.LastName;
            var customerCode = contact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber);

            var email = contact.Email;

            var subject = string.Format(emailPage.Subject);
            var to = new MailAddress
            {
                Address = email
            };
            var from = new MailAddress
            {
                Address = emailPage.FromEmailAddress,
                DisplayName = emailPage.FromDisplayName
            };

            var emailBody = emailPage.Body?.ToString();
            emailBody = emailBody.Replace("[_FirstName]", firstName);
            emailBody = emailBody.Replace("[_LastName]", lastName);
            emailBody = emailBody.Replace("[_CustomerCode]", customerCode);

            var otherUrl = SiteDefinition.Current.SiteUrl;
            var emailToSend = string.Format(GetEmailToSendString(), otherUrl, emailPage.Header, emailBody);

            var settings = GetEmailSettings();

            if (!SendEmail(to, from, subject, emailToSend, settings, _logger))
            {
                _logger.Error($"Failed to send Reminder Notification Email to {email} with template from PageID {emailPage.PageLink.ID}");
            }
        }

        
    }
}