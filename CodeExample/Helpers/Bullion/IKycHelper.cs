using System;
using Mediachase.Commerce.Customers;
using TRM.Shared.Models.DTOs;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.DTOs.Bullion;

namespace TRM.Web.Helpers.Bullion
{
    public interface IKycHelper
    {
        KycQueryResultDto PerformKycOnUserDetails(ApplicationUser userDetails);
        //Guid GetKycProfileId(string countryCode);
        KycQueryResultDto PerformKycOnDocument(string documentNumber, DateTime? expiryDate, string countryCode,
            string kycQueryReference, Guid authenticationId, KycHelper.KycDocumentType documentType);
        KycQueryResultDto PerformKycOnDocumentImage(byte[] documentImage, string countryCode, string kycQueryReference, Guid authenticationId);
        bool KycCanProceed(CustomerContact customer);//, AccountKycStatus miniumumKycLevelForThisOperation = AccountKycStatus.Approved);
        //void ResetCustomerKycStatus(CustomerContact customer);

        bool SendKycResultCheckingEmail(MailedUserInformationDto bullionUser, AccountKycStatus kycCheckStatus);
        void ValidateKycResponse(out string kycQueryReference, out Guid authenticationId);
        KycQueryResultDto PerformKycOnCustomer(CustomerKycApplication customer, AddressKycApplication address);
        AccountKycStatus GetCustomerKycStatus(CustomerContact customer);
        DateTime? GetCustomerKycDate(CustomerContact customer);
        decimal GetCustomerTotalSpend(int recheckmonths);
        decimal GetCustomerTotalSpend(int recheckMonths, CustomerContact contact);
    }
}