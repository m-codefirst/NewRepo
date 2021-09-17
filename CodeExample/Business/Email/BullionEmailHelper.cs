using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using log4net;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.IntegrationServices.Models.Export.Emails;
using TRM.IntegrationServices.Models.Export.Orders;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Extentions;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Models.ViewModels.Bullion.CustomerBankAccount;
using TRM.Web.Models.ViewModels.Bullion.MixedCheckout;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Helpers;
using StringResources = TRM.Web.Constants.StringResources;

namespace TRM.Web.Business.Email
{
    public class BullionEmailHelper : IBullionEmailHelper
    {
        private readonly IEmailHelper _emailHelper;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BullionEmailHelper));
        private readonly CustomerContext _customerContext;
        private readonly LocalizationService _localizationService;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionEmailHelper(
            IEmailHelper emailHelper,
            CustomerContext customerContext,
            LocalizationService localizationService, IAmBullionContactHelper bullionContactHelper)
        {
            _emailHelper = emailHelper;
            _customerContext = customerContext;
            _localizationService = localizationService;
            _bullionContactHelper = bullionContactHelper;
        }

        public void SendBullionRequestWithdrawalEmail(CustomerContact currentContact, BankAccountViewModel bankAccountToUse)
        {
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionMailAddressSentTo(currentContact);
                var emailParams = GetRequestedWithdrawalEmailParams(currentContact, bankAccountToUse);
                string sendingEmailErrorMessage;
                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.FundsWithdrawalEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.ErrorFormat("Error when trying to send the email: {0}", sendingEmailErrorMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendBullionAddFundsEmail(CustomerContact currentContact, ManualPaymentDto manualPaymentDto)
        {
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionMailAddressSentTo(currentContact);
                var emailParams = GetBullionAddFundsEmailParams(currentContact, manualPaymentDto);
                string sendingEmailErrorMessage;
                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionAddFundsEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendBullionSellRequestEmail(CustomerContact currentContact, SellBullionDefaultLandingViewModel sellBullion)
        {
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionMailAddressSentTo(currentContact);
                var emailParams = GetSellRequestEmailParams(currentContact, sellBullion);
                string sendingEmailErrorMessage;
                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionSellRequestEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendBullionDeliverFromVaultEmail(CustomerContact currentContact, DeliverBullionLandingViewModel deliverBullionModel)
        {
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionMailAddressSentTo(currentContact);
                var emailParams = GetBullionDeliverFromVaultEmailParams(currentContact, deliverBullionModel);
                string sendingEmailErrorMessage;
                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionDeliverFromVaultEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendBankTransferFundsNowAvailableEmail(Guid customerId, BankDepositeModel bankDeposite)
        {
            var currentContact = _customerContext.GetContactById(customerId);
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionMailAddressSentTo(currentContact);
                if (!toAddresses.Any()) return;

                var emailParams = GetBullionBankTransferFundsNowAvailableEmailParams(currentContact, bankDeposite);
                string sendingEmailErrorMessage;

                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionBankDeposit, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendCancelPurchaseOrderEmail(CancelPurchaseOrderModel cancelPurchaseOrderModel)
        {
            var contact = _customerContext.GetContactById(cancelPurchaseOrderModel.CustomerId);
            if (contact == null)
            {
                Logger.Error($"Cannot find the contact with CustomerId: {cancelPurchaseOrderModel.CustomerId}");
                return;
            }
            var toAddresses = new List<MailAddress> { new MailAddress(contact.Email, contact.FullName) };
            string sendingEmailErrorMessage;

            if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.CancelPurchaseEmail, toAddresses, out sendingEmailErrorMessage, GetCancelEmailParams(contact, cancelPurchaseOrderModel))) return;

            Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");
        }

        private List<MailAddress> GetBullionStatementAndInvoiceSentTo(CustomerContact customer)
        {
            var mailAddresses = new List<MailAddress>();
            if (!_bullionContactHelper.IsSippContact(customer) || SendEmailToPensionCustomer(customer))
                mailAddresses.Add(new MailAddress(customer.Email, customer.FullName));

            return mailAddresses;
        }

        public List<MailAddress> GetBullionMailAddressSentTo(CustomerContact customer)
        {
            var mailAddresses = new List<MailAddress>();
            if (!_bullionContactHelper.IsSippContact(customer) || SendEmailToPensionCustomer(customer))
                mailAddresses.Add(new MailAddress(customer.Email, customer.FullName));


            if (!SendEmailToPensionProvider(customer)) return mailAddresses;

            var organization = customer.ContactOrganization;
            if (organization == null) return mailAddresses;

            var pensionProviders = organization.Contacts.Where(x => _bullionContactHelper.IsPensionProvider(x))
                .Select(x => new MailAddress(x.Email, x.FullName)).ToList();
            if (pensionProviders.Any()) mailAddresses.AddRange(pensionProviders);

            return mailAddresses;
        }

        public void SendBullionStatementAvailableEmail(Guid customerId, DateTime statementDate)
        {
            var currentContact = _customerContext.GetContactById(customerId);
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionStatementAndInvoiceSentTo(currentContact);
                if (!toAddresses.Any()) return;

                var emailParams = GetBullionStatementAvailableEmailParams(currentContact, statementDate);
                string sendingEmailErrorMessage;
                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionStatementEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendBullionDispatchFromVaultEmail(Guid customerId, DispatchFromVaultOrderDto dispatchFromVaultOrderDto)
        {
            var currentContact = _customerContext.GetContactById(customerId);
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionMailAddressSentTo(currentContact);
                var emailParams = GetBullionDispatchFromVaultEmailParams(currentContact, dispatchFromVaultOrderDto);
                string sendingEmailErrorMessage;
                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionDispatchFromVaultEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendBullionOrderConfirmationEmail(InvestmentPurchaseOrder po)
        {
            try
            {
                if (po == null) return;
                var currentContact = _customerContext.CurrentContact;
                if (currentContact == null) return;
                var toAddresses = GetBullionMailAddressSentTo(currentContact);

                var emailParams = GetOrderConfirmationEmailParams(po, currentContact);
                string sendingEmailErrorMessage;
                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionOrderConfirmationEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.ErrorFormat("Error when trying to send the email: {0}", sendingEmailErrorMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void SendBullionInvoiceAvailableEmail(Guid customerId, DateTime invoiceDate)
        {
            var currentContact = _customerContext.GetContactById(customerId);
            try
            {
                if (currentContact == null) return;

                var toAddresses = GetBullionStatementAndInvoiceSentTo(currentContact);
                if (!toAddresses.Any()) return;

                var emailParams = GetBullionInvoiceAvailableEmailParams(currentContact, invoiceDate);
                string sendingEmailErrorMessage;

                if (_emailHelper.SendBullionEmail(Constants.StringConstants.BullionEmailCategories.BullionInvoiceEmail, toAddresses, out sendingEmailErrorMessage, emailParams)) return;

                Logger.Error($"Error when trying to send the email: {sendingEmailErrorMessage}");

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public Dictionary<string, object> GetOrderConfirmationEmailParams(InvestmentPurchaseOrder po, CustomerContact currentContact)
        {
            var deliveryAddress = po.DeliveryAddress.ToFullAddressText();
            if (po.DeliveredItems == null || !po.DeliveredItems.Any())
            {
                deliveryAddress = _localizationService.GetStringByCulture(StringResources.BullionDeliveryToVault,
                    "Royal Mint Vault", ContentLanguage.PreferredCulture);
            }
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },

                { Constants.StringConstants.EmailParameters.OrderNumber, String.Join(", ",  po.OrderNumbers.ToArray())},
                { Constants.StringConstants.EmailParameters.OrderDate, DateTime.Today.ToString("dd MMMM yyyy")},
                { Constants.StringConstants.EmailParameters.OrderTotal, po.Total },
                { Constants.StringConstants.EmailParameters.OrderVat, po.InvestmentVat},
                { Constants.StringConstants.EmailParameters.OrderDeliveryCharge, po.DeliveryCost },
                { Constants.StringConstants.EmailParameters.SubTotal, po.SubTotal },


                { Constants.StringConstants.EmailParameters.DeliveryAddress, deliveryAddress },
                { Constants.StringConstants.EmailParameters.OrderPromotion, po.OrderSavings },
                { Constants.StringConstants.EmailParameters.PaymentType, _localizationService.GetStringByCulture(StringResources.InvestmentWallet, "Investment Wallet",ContentLanguage.PreferredCulture) },

                { Constants.StringConstants.EmailParameters.ORDERITEMS, GetEmailOrderItemInfoMappings(po) }
            };
            return result;
        }

        private IEnumerable<Dictionary<string, string>> GetEmailOrderItemInfoMappings(InvestmentPurchaseOrder po)
        {
            var result = new List<Dictionary<string, string>>();

            if (po.VaultItems != null && po.VaultItems.Any())
            {
                result.AddRange(po.VaultItems.Select(lineItem => GetLineItemInformationEmailParams(lineItem, _localizationService.GetStringByCulture(StringResources.Vaulted, "Vaulted", ContentLanguage.PreferredCulture))));
            }

            if (po.DeliveredItems != null && po.DeliveredItems.Any())
            {
                result.AddRange(po.DeliveredItems.Select(lineItem => GetLineItemInformationEmailParams(lineItem, _localizationService.GetStringByCulture(StringResources.Delivered, "Delivered", ContentLanguage.PreferredCulture))));
            }

            return result;
        }

        private Dictionary<string, string> GetLineItemInformationEmailParams(CartItemViewModel item, string deliveryOption)
        {
            var quantityInfo = item.BullionCartItem == null
                ? item.Quantity.ToString("##.###")
                : (item.BullionCartItem.IsSignatureVariant ? $"{item.BullionCartItem.Weight} oz" : item.Quantity.ToString("##.###"));
            return new Dictionary<string, string>
            {
                {Constants.StringConstants.EmailParameters.LineItemTitle, item.DisplayName},
                {Constants.StringConstants.EmailParameters.LineItemCode, item.Code},
                {Constants.StringConstants.EmailParameters.LineItemQuantity, quantityInfo},
                {Constants.StringConstants.EmailParameters.LineItemTotal, item.DiscountedPrice},
                {Constants.StringConstants.EmailParameters.LineItemImage, item.ImageUrl},
                {Constants.StringConstants.EmailParameters.LineItemDeliveryOption, deliveryOption}
            };
        }

        private Dictionary<string, object> GetBullionDispatchFromVaultEmailParams(CustomerContact currentContact, DispatchFromVaultOrderDto dispatchFromVaultOrderDto)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.OrderNumber, dispatchFromVaultOrderDto.OrderReference},
                { Constants.StringConstants.EmailParameters.ProductCode, dispatchFromVaultOrderDto.ProductCode},
                { Constants.StringConstants.EmailParameters.Quantity, dispatchFromVaultOrderDto.Quantity.ToString("####")},
                { Constants.StringConstants.EmailParameters.OrderTotal, dispatchFromVaultOrderDto.OrderTotal},
                { Constants.StringConstants.EmailParameters.ProductTitle, dispatchFromVaultOrderDto.ProductTitle},
                { Constants.StringConstants.EmailParameters.ProductSubtitle, dispatchFromVaultOrderDto.ProductSubtitle},
                { Constants.StringConstants.EmailParameters.OrderDate, dispatchFromVaultOrderDto.OrderDate},
                { Constants.StringConstants.EmailParameters.ProductImage, dispatchFromVaultOrderDto.ProductImage},
                { Constants.StringConstants.EmailParameters.DispatchAddress, dispatchFromVaultOrderDto.DeliveryAddress.DeliveryAddressLine.ToFullAddressText()}
            };

            return result;
        }

        private Dictionary<string, object> GetBullionStatementAvailableEmailParams(CustomerContact currentContact, DateTime statementDate)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.StatementDate, statementDate.ToString("dd MMMM yyyy")}
            };

            return result;
        }

        private bool SendEmailToPensionCustomer(CustomerContact customer)
        {
            var customerEmailSetting = customer.GetIntegerProperty(StringConstants.CustomFields.SendEmailTo);
            return _bullionContactHelper.IsSippContact(customer) && (customerEmailSetting == (int)Enums.EmailSettingsForPensionCustomerEnum.CustomerOnly
                || customerEmailSetting == (int)Enums.EmailSettingsForPensionCustomerEnum.CustomerAndSchemeAdmins
                || customerEmailSetting == 0); // No value
        }

        private bool SendEmailToPensionProvider(CustomerContact customer)
        {
            var customerEmailSetting = customer.GetIntegerProperty(StringConstants.CustomFields.SendEmailTo);
            return _bullionContactHelper.IsSippContact(customer)
                && (customerEmailSetting == (int)Enums.EmailSettingsForPensionCustomerEnum.SchemeAdminsOnly
                || customerEmailSetting == (int)Enums.EmailSettingsForPensionCustomerEnum.CustomerAndSchemeAdmins);
        }

        private Dictionary<string, object> GetBullionDeliverFromVaultEmailParams(CustomerContact currentContact, DeliverBullionLandingViewModel deliverBullionModel)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.TransactionNumber, deliverBullionModel.DeliverTransactionNumber},
                { Constants.StringConstants.EmailParameters.OrderDate, DateTime.Today.ToString("dd MMMM yyyy")},
                { Constants.StringConstants.EmailParameters.ProductCode, deliverBullionModel.DeliverVariant.Code},
                { Constants.StringConstants.EmailParameters.ProductTitle, deliverBullionModel.DeliverVariant.Title },
                { Constants.StringConstants.EmailParameters.ProductSubtitle, deliverBullionModel.DeliverVariant.Subtitle},
                { Constants.StringConstants.EmailParameters.ProductImage, deliverBullionModel.DeliverVariant.ImageUrl },
                { Constants.StringConstants.EmailParameters.Quantity, deliverBullionModel.DeliverVariant.QuantityToSellOrDeliver.ToString("####") },
                { Constants.StringConstants.EmailParameters.Weight, deliverBullionModel.DeliverVariant.Weight},
                { Constants.StringConstants.EmailParameters.PaymentType, _localizationService.GetStringByCulture(StringResources.InvestmentWallet, "Investment Wallet",ContentLanguage.PreferredCulture)},
                { Constants.StringConstants.EmailParameters.DeliveryAddress, deliverBullionModel.DeliverAddress},
                { Constants.StringConstants.EmailParameters.DeliveryMethod, deliverBullionModel.ShippingMethod.FriendlyName},
                { Constants.StringConstants.EmailParameters.InvestmentVat, deliverBullionModel.InvestmentVat.ToString()},
                { Constants.StringConstants.EmailParameters.DeliveryVat, deliverBullionModel.DeliverVat.ToString()},
                { Constants.StringConstants.EmailParameters.DeliveryCost, deliverBullionModel.DeliverCost.ToString()},
                { Constants.StringConstants.EmailParameters.OrderTotal, deliverBullionModel.DeliveryTotal.ToString()}
            };

            return result;
        }

        private Dictionary<string, object> GetBullionAddFundsEmailParams(CustomerContact currentContact, ManualPaymentDto manualPaymentDto)
        {
            var effectiveBalance = currentContact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.DepositType, manualPaymentDto.MastercardCardType},
                { Constants.StringConstants.EmailParameters.DepositAmount, new Money(manualPaymentDto.Amount, manualPaymentDto.CurrencyCode).ToString()},
                { Constants.StringConstants.EmailParameters.DepositDate, DateTime.Today.ToString("dd MMMM yyyy")},
                { Constants.StringConstants.EmailParameters.EffectiveBalance,new Money(effectiveBalance, manualPaymentDto.CurrencyCode).ToString()}
            };

            return result;
        }
        private Dictionary<string, object> GetRequestedWithdrawalEmailParams(CustomerContact currentContact, BankAccountViewModel bankAccountToUse)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.RequestedDate, DateTime.Today.ToString("dd MMMM yyyy")},
                { Constants.StringConstants.EmailParameters.BankNickname, bankAccountToUse.Nickname}
            };

            return result;
        }

        private Dictionary<string, object> GetSellRequestEmailParams(CustomerContact currentContact, SellBullionDefaultLandingViewModel sellBullion)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.TransactionNumber, sellBullion.SellTransactionNumber},
                { Constants.StringConstants.EmailParameters.ProductCode, sellBullion.SellVariant.Code},
                { Constants.StringConstants.EmailParameters.OrderTotal, sellBullion.SellTotal.ToString()},
                { Constants.StringConstants.EmailParameters.ProductTitle, sellBullion.SellVariant.Title},
                { Constants.StringConstants.EmailParameters.ProductSubtitle, sellBullion.SellVariant.Subtitle},
                { Constants.StringConstants.EmailParameters.OrderDate, DateTime.Today.ToString("dd MMMM yyyy")},
                { Constants.StringConstants.EmailParameters.Weight, sellBullion.CombinedWeightInSale.ToString("F3", CultureInfo.InvariantCulture)},
                { Constants.StringConstants.EmailParameters.ProductImage, sellBullion.SellVariant.ImageUrl},
                { Constants.StringConstants.EmailParameters.SellPrice, sellBullion.SellTotal},
                { Constants.StringConstants.EmailParameters.PricePerOzIncPremium, sellBullion.PremiumPricePerOzIncludingPremium.ToString()},
                { Constants.StringConstants.EmailParameters.Quantity, sellBullion.SellVariant.BullionType != Constants.Enums.BullionVariantType.Signature ? sellBullion.SellVariant.AvailableToSell.ToString("####") : string.Empty}
            };

            return result;
        }

        private Dictionary<string, object> GetBullionInvoiceAvailableEmailParams(CustomerContact currentContact, DateTime invoiceDate)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.InvoiceDate, invoiceDate.ToString("dd MMMM yyyy")}
            };

            return result;
        }

        private Dictionary<string, object> GetBullionBankTransferFundsNowAvailableEmailParams(CustomerContact currentContact, BankDepositeModel bankDeposite)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.DepositAmount, bankDeposite.DepositAmount},
                { Constants.StringConstants.EmailParameters.DepositDate, bankDeposite.Date.ToString("dd MMMM yyyy")},
                { Constants.StringConstants.EmailParameters.EffectiveBalance, bankDeposite.EffectiveBalance}

            };

            return result;
        }

        private Dictionary<string, object> GetCancelEmailParams(CustomerContact currentContact, CancelPurchaseOrderModel cancelPurchaseOrderModel)
        {
            var result = new Dictionary<string, object>
            {
                { Constants.StringConstants.EmailParameters.Title, currentContact.GetStringProperty(StringConstants.CustomFields.ContactTitleField) },
                { Constants.StringConstants.EmailParameters.FirstName, currentContact.FirstName },
                { Constants.StringConstants.EmailParameters.LastName, currentContact.LastName },
                { Constants.StringConstants.EmailParameters.AccountNumber, currentContact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber) },

                { Constants.StringConstants.EmailParameters.OrderDate, cancelPurchaseOrderModel.PurchaseDate},
                { Constants.StringConstants.EmailParameters.OrderNumber, cancelPurchaseOrderModel.PurchaseNumber},
            };

            return result;
        }
    }
}