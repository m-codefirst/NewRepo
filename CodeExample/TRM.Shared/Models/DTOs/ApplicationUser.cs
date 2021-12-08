using System;
using System.Collections.Generic;
using Mediachase.Commerce.Customers;

namespace TRM.Shared.Models.DTOs
{
    public class ApplicationUser
    {
        public ApplicationUser()
        {
            Addresses = new List<CustomerAddress>();
            IsActivated = true;
            IsSiteCoreUser = false;
        }

        public string UserId { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public List<CustomerAddress> Addresses { get; set; }
        public string Title { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Password { get; set; }
        public string RegistrationSource { get; set; }
        public string ObsAccountNumber { get; set; }
        public bool? IsLockedOut { get; set; }
        public bool? IsActivated { get; set; }
        public bool? IsMigrated { get; set; }
        public bool? OfflineActivation { get; set; }
        public string D2CCustomerType { get; set; }
        public string TaxGroup { get; set; }
        public decimal? CreditLimit { get; set; }
        public bool? InclTax { get; set; }
        public string StatementPreference { get; set; }
        public string DlvMode { get; set; }
        public string Department { get; set; }
        public string ClassificationId { get; set; }
        public string Currency { get; set; }
        public bool? ByEmail { get; set; }
        public DateTime? EmailConsentDateTime { get; set; }
        public string EmailConsentSource { get; set; }
        public bool? ByPost { get; set; }
        public DateTime? PostConsentDateTime { get; set; }
        public string PostConsentSource { get; set; }
        public bool? ByTelephone { get; set; }
        public DateTime? TelephoneConsentDateTime { get; set; }
        public string TelephoneConsentSource { get; set; }
        public string Affix { get; set; }

        //For bullion registration user
        public string MiddleName { get; set; }
        public string SecondSurname { get; set; }
        public string Gender { get; set; }
        public string TwoStepAuthenticationQuestion { get; set; }
        public string TwoStepAuthenticationAnswer { get; set; }
        public string CustomerType { get; set; }
        public string MobilePhone { get; set; }
        public string BullionCustomerType { get; set; }
        public int BullionPremiumGroupInt { get; set; }
        public string BullionAccountNumber { get; set; }

        public KycQueryResultDto CustomerKycData { get; set; }
        public bool? BullionUnableToPurchaseBullion { get; set; }
        public bool? BullionUnableToSellorDeliverFromVault { get; set; }
        public bool? BullionUnableToDepositViaCard { get; set; }
        public bool? BullionUnableToWithdraw { get; set; }
        public bool? BullionUnableToLogin { get; set; }
        public bool? IsSiteCoreUser { get; set; }
    }
}