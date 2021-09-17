using System;
using System.Linq;
using EPiServer;
using EPiServer.Find.Helpers;
using EPiServer.Framework.Cache;
using EPiServer.Security;
using EPiServer.Web;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Extensions;
using TRM.Shared.Models.DTOs;
using TRM.Web.Business.Cart;
using TRM.Web.Extentions;
using TRM.Web.Models.DataLayer;
using TRM.Web.Models.DTOs.Cart;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers
{
    public class BullionContactHelper : IAmBullionContactHelper
    {
        private readonly IUserImpersonation _userImpersonation;
        private readonly IMetaFieldTypeHelper _metaFieldTypeHelper;
        private readonly IAmCurrencyHelper _currencyHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IBullionPremiumGroupHelper _bullionPremiumGroupHelper;
        private readonly ISynchronizedObjectInstanceCache _synchronizedObjectInstanceCache;

        public BullionContactHelper(IUserImpersonation userImpersonation,
            IMetaFieldTypeHelper metaFieldTypeHelper,
            IAmCurrencyHelper currencyHelper,
            IContentLoader contentLoader,
            IBullionPremiumGroupHelper bullionPremiumGroupHelper, ISynchronizedObjectInstanceCache synchronizedObjectInstanceCache)
        {
            _userImpersonation = userImpersonation;
            _metaFieldTypeHelper = metaFieldTypeHelper;
            _currencyHelper = currencyHelper;
            _contentLoader = contentLoader;
            _bullionPremiumGroupHelper = bullionPremiumGroupHelper;
            _synchronizedObjectInstanceCache = synchronizedObjectInstanceCache;
        }

        public bool IsConsumerAccount(CustomerContact contact)
        {
            var customerType = contact.GetStringProperty(StringConstants.CustomFields.CustomerType);
            return string.IsNullOrEmpty(customerType) || customerType.Equals(StringConstants.CustomerType.Consumer) ||
                   customerType.Equals(StringConstants.CustomerType.ConsumerAndBullion);
        }

        public bool IsBullionAccountOnly(CustomerContact contact)
        {
            var customerType = contact.GetStringProperty(StringConstants.CustomFields.CustomerType);
            return customerType.Equals(StringConstants.CustomerType.Bullion);
        }

        public bool IsMixedAccount(CustomerContact contact)
        {
            var customerType = contact.GetStringProperty(StringConstants.CustomFields.CustomerType);
            return customerType.Equals(StringConstants.CustomerType.ConsumerAndBullion);
        }

        public bool IsConsumerAccountOnly(CustomerContact contact)
        {
            var customerType = contact.GetStringProperty(StringConstants.CustomFields.CustomerType);
            return string.IsNullOrEmpty(customerType) || customerType.Equals(StringConstants.CustomerType.Consumer);
        }
        public bool IsCustomerServiceAccount(CustomerContact contact)
        {
            if (contact == null) return false;
            var principalInfo = _userImpersonation.CreatePrincipal(GetUsername(contact));

            return principalInfo?.IsInRole("CustomerService") ?? false;
        }

        public string GetBullionCustomerType(CustomerContact contact)
        {
            if (contact == null) return string.Empty;
            var bullionCustomerType = contact.GetIntegerProperty(StringConstants.CustomFields.BullionCustomerType);

            var customerType = _metaFieldTypeHelper.GetMetaEnumItems(StringConstants.CustomMetaFieldTypeNames.BullionCustomerType).FirstOrDefault(c => c.Handle == bullionCustomerType);
            return customerType == null ? string.Empty : customerType.Name;
        }

        public string GetBullionPremiumGroup(CustomerContact contact)
        {
            if (contact == null) return string.Empty;
            var bullionPremiumGroupInt = contact.GetIntegerProperty(StringConstants.CustomFields.BullionPremiumGroupInt);

            var customerType = _bullionPremiumGroupHelper.GetBullionPremiumGroup()
                .FirstOrDefault(pg => pg.Value == bullionPremiumGroupInt.ToString())?.DisplayName;

            return customerType == null ? string.Empty : customerType;
        }

        public bool IsSippContact(CustomerContact contact)
        {
            if (contact == null) return false;

            var bullionCustomerType = contact.GetStringProperty(StringConstants.CustomFields.BullionCustomerType);

            return bullionCustomerType.Equals(((int)Enums.BullionCustomerType.SIPPCustomer).ToString());
        }

        public CustomerAddress GetBullionAddress(CustomerContact contact)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);

            var customerAddress = contact.ContactAddresses.FirstOrDefault(x => x.Properties[StringConstants.CustomFields.BullionKycAddress]?.Value != null && x.Properties[StringConstants.CustomFields.BullionKycAddress].Value.Equals(true));

            if (startPage != null && startPage.OverrideKycCheckForBullionAccountActivity && customerAddress == null)
            {
                return contact.ContactAddresses.FirstOrDefault();
            }

            return customerAddress;
        }

        public bool UpdateCustomerKycStatus(CustomerContact contact, KycQueryResultDto kycQueryResultDto)
        {
            if (contact == null) return false;
            if (contact[StringConstants.CustomFields.KycStatus].CastTo(0) != (int)kycQueryResultDto.Status)
            {
                contact[StringConstants.CustomFields.KycStatusNotification] = DateTimeHelper.KycNotificationDate;
                contact[StringConstants.CustomFields.KycDate] = DateTime.Now;
                contact[StringConstants.CustomFields.TotalSpendSinceKyc] = 0m;
            }

            contact.Properties[StringConstants.CustomFields.KycStatus].Value = (int)kycQueryResultDto.Status;
            contact.Properties[StringConstants.CustomFields.BullionKycApiResponse].Value = kycQueryResultDto.Id3Response;
            contact.Properties[StringConstants.CustomFields.BullionObsAccountLastUpdatedDate].Value = DateTime.UtcNow;
            contact.SaveChanges();
            return true;
        }

        public Money GetMoneyAvailableToInvest(CustomerContact contact)
        {
            if (contact == null) return new Money();
            var customerCurrency = new Currency(_currencyHelper.GetDefaultCurrencyCode());
            var availableToInvestAmount = contact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend);
            var balanceAmount = new Money(availableToInvestAmount, customerCurrency);
            return balanceAmount;
        }

        public bool HasFailedStage1(CustomerContact contact)
        {
            if (!IsBullionAccount(contact)) return false;
            var kycStatus = contact.GetIntegerProperty(StringConstants.CustomFields.KycStatus);
            return kycStatus == (int)AccountKycStatus.PendingAdditionalInformation;
        }

        public bool HasFailedStage2(CustomerContact contact)
        {
            if (!IsBullionAccount(contact)) return false;
            var kycStatus = contact.GetIntegerProperty(StringConstants.CustomFields.KycStatus);
            return kycStatus == (int)AccountKycStatus.Rejected || kycStatus == (int)AccountKycStatus.ReadyForReview;
        }

        public bool IsKycStatusApproved(CustomerContact customerContact)
        {
            if (!IsBullionAccount(customerContact)) return false;
            var kycStatus = customerContact.GetIntegerProperty(StringConstants.CustomFields.KycStatus);
            return kycStatus == (int)AccountKycStatus.Approved;
        }

        public bool KycRefered(CustomerContact contact)
        {
            return contact != null && HasFailedStage1(contact);
        }

        public bool KycRejected(CustomerContact contact)
        {
            return contact != null && HasFailedStage2(contact);
        }

        public string GetUsername(CustomerContact contact)
        {
            if (contact.IsNull()) return string.Empty;
            var mapUserKey = new MapUserKey();
            return mapUserKey.ToUserKey(contact.UserId)?.ToString() ?? contact.UserId;
        }

        public string GetFullname(CustomerContact customer)
        {
            if (customer == null) return string.Empty;
            return string.IsNullOrEmpty(customer.FullName)
                ? $"{customer.GetStringProperty(StringConstants.CustomFields.ContactTitleField)} {customer.FirstName} {customer.LastName}"
                : $"{customer.GetStringProperty(StringConstants.CustomFields.ContactTitleField)} {customer.FullName}";
        }

        public string GetBeneficiaryReference(CustomerContact contact)
        {
            var bullionAccountNumber = contact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber);
            var beneficiaryReference = $"{bullionAccountNumber}{contact.LastName}".RemoveNonAzCharacters();

            return beneficiaryReference;
        }

        public decimal GetEffectiveBalance(CustomerContact contact)
        {
            return contact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);
        }

        public void UpdateBalances(CustomerContact contact, decimal effectiveBalanceAmount)
        {
            var effectiveBalance = contact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);

            contact.Properties[StringConstants.CustomFields.BullionCustomerEffectiveBalance].Value = Math.Round(effectiveBalance + effectiveBalanceAmount, 2);
            contact.SaveChanges();
        }

        public void UpdateLifeTimeBalance(CustomerContact contact, decimal lifetimeValueBalanceAmount)
        {
            try
            {
                var lifetimeValue = contact.GetDecimalProperty(StringConstants.CustomFields.LifetimeValue);
                contact.Properties[StringConstants.CustomFields.LifetimeValue].Value = Math.Round(lifetimeValue + lifetimeValueBalanceAmount, 2);
                contact.SaveChanges();

                var cacheKey = $"{contact.PrimaryKeyId}{contact.FirstName}{GlobalCache.InitializeDataLayerCustomer}";
                if (_synchronizedObjectInstanceCache.Get(cacheKey) is AnalyticsDigitalDataModel cache)
                {
                    var analyticsData = Newtonsoft.Json.JsonConvert.DeserializeObject<AnalyticsDataLayer.Customer>(cache.JsonString);
                    var customerLtv = contact.Properties[StringConstants.CustomFields.LifetimeValue].Value;
                    analyticsData.CustomerDetails.LifetimeValue = !string.IsNullOrWhiteSpace(customerLtv?.ToString()) ? customerLtv.ToString() : string.Empty;
                    var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(analyticsData);
                    cache.JsonString = jsonString;
                }
            }
            catch (Exception) { }

        }

        public void UpdateBalances(CustomerContact contact, decimal effectiveBalanceAmount, decimal availableToSpendAmount, decimal availableToWithdrawAmount)
        {
            var effectiveBalance = contact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerEffectiveBalance);
            var availableToSpend = contact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToSpend);
            var availableToWithdraw = contact.GetDecimalProperty(StringConstants.CustomFields.BullionCustomerAvailableToWithdraw);

            contact.Properties[StringConstants.CustomFields.BullionCustomerEffectiveBalance].Value = Math.Round(effectiveBalance + effectiveBalanceAmount, 2);
            contact.Properties[StringConstants.CustomFields.BullionCustomerAvailableToSpend].Value = Math.Round(availableToSpend + availableToSpendAmount, 2);
            contact.Properties[StringConstants.CustomFields.BullionCustomerAvailableToWithdraw].Value = Math.Round(availableToWithdraw + availableToWithdrawAmount, 2);
            contact.SaveChanges();
        }

        public bool IsPensionProvider(CustomerContact contact)
        {
            if (contact == null) return false;

            var bullionCustomerType = contact.GetStringProperty(StringConstants.CustomFields.BullionCustomerType);

            return bullionCustomerType.Equals(((int)Enums.BullionCustomerType.SIPPProvider).ToString());
        }

        public string GetDefaultCurrencyCode(CustomerContact currentCustomer)
        {
            if (currentCustomer == null) return StringConstants.DefaultValues.CurrencyCode;
            return string.IsNullOrEmpty(currentCustomer.PreferredCurrency) ? StringConstants.DefaultValues.CurrencyCode : currentCustomer.PreferredCurrency;
        }

        public bool IsBullionAccount(CustomerContact contact)
        {
            if (contact == null) return false;
            var customerType = contact.GetStringProperty(StringConstants.CustomFields.CustomerType);
            return IsBullionCustomerType(customerType);
        }

        public bool IsBullionCustomerType(string customerType)
        {
            if (string.IsNullOrWhiteSpace(customerType))
            {
                return false;
            }

            return customerType.Equals(StringConstants.CustomerType.Bullion) ||
                   customerType.Equals(StringConstants.CustomerType.ConsumerAndBullion);
        }
    }
}