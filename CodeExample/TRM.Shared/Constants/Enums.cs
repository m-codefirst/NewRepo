using Mediachase.BusinessFoundation.Common;
using System.ComponentModel;

namespace TRM.Shared.Constants
{
    public class Enums
    {
        public enum eCustomerStatus
        {
            Created = 1,
            CreatedWithWarning = 2,
            Updated = 3,
            UpdatedWithWarning = 4,
            Error = 5
        }

        public enum CacheLevel
        {
            None,
            AnonymousOnly,
            AllUsers
        }

        public enum FullCheckoutLoggingLevel
        {
            Minimum = 1,
            Cookies = 2,
            All = 5
        }
        public enum SecurityQuestionType
        {
            [MetaEnumItemName("None")] None = int.MaxValue
        }
        public enum BullionCustomerTypeEnum
        {
            [MetaEnumItemName("None")] None = int.MaxValue
        }

        public enum EmailSettingsForPensionCustomerEnum
        {
            [MetaEnumItemName("CustomerOnly")] CustomerOnly = 1,
            [MetaEnumItemName("SchemeAdminsOnly")] SchemeAdminsOnly = 2,
            [MetaEnumItemName("CustomerAndSchemeAdmins")] CustomerAndSchemeAdmins = 3
        }

        public enum eOrderSendStatus
        {
            [Description("Invalid")]
            Invalid = 0,
            [Description("Pending")]
            Pending = 1,
            [Description("Retrieved By iCore")]
            RetrievedByiCore = 2,
            [Description("Error Sending")]
            ErrorSending = 3,
            [Description("Invalid Line Items")]
            InvalidLineItems = 4,
            [Description("Invalid Delivery Address")]
            InvalidDeliveryAddress = 5,
            [Description("Invalid Payment")]
            InvalidPayment = 6,
            [Description("Serialization Failed")]
            SerializationFailed = 7,
            [Description("Error")]
            Error = 8,
            [Description("Saved in iCore Node")]
            SavedIniCoreNode = 9,
            [Description("Sent to AX")]
            SentToAX = 10,
            [Description("Failed in AX")]
            FailedInAX = 11,
            [Description("Partially Failed in AX")]
            PartiallyFailedInAX = 12,
            [Description("Duplicate Order")]
            DuplicateOrder = 99
        }
        public enum eContactUpdateStatus
        {
            [Description("Pending")]
            Pending = 0,
            [Description("Ignore")]
            Ignore = 1,
            [Description("Invalid Address")]
            InvalidAddress = 2,
            [Description("Inserted Successfully")]
            InsertedInObs = 3,
            [Description("Updated Successfully")]
            UpdatedInObs = 4,
            [Description("Error Sending")]
            ErrorSending = 5,
            [Description("Error")]
            Error = 6,
            [Description("Failed in iCore")]
            FailedIniCore = 7,
            [Description("Retrieved By iCore")]
            RetrievedByiCore = 8,
            [Description("Saved in iCore Node")]
            SavedIniCoreNode = 9,
            [Description("Sent to AX")]
            SentToAX = 10,
            [Description("Failed in AX")]
            FailedInAX = 11
        }

        public enum eCreditPaymentSendStatus
        {
            [Description("Pending")]
            Pending = 0,
            [Description("No Billing Address")]
            NoBillingAddress = 1,
            [Description("Failed in iCore")]
            FailedIniCore = 2,
            [Description("Retrieved By iCore")]
            RetrievedByiCore = 3,
            [Description("Saved in iCore Node")]
            SavedIniCoreNode = 4,
            [Description("Sent to AX")]
            SentToAX = 5,
            [Description("Failed in AX")]
            FailedInAX = 6,
            [Description("Failed to Retrieve")]
            FailedToRetrieve = 7,
            [Description("Customer Record Not Found")]
            CustomerRecordNotFound = 8
        }

        public enum eXmlUpdateStatus
        {
            [Description("Pending")]
            Pending,
            [Description("Success")]
            Success,
            [Description("Failed in Epi")]
            FailedInEpi,
            [Description("Customer Record Not Found")]
            CustomerRecordNotFound,
            [Description("Entry Record Not Found")]
            EntryRecordNotFound,
            [Description("Transaction History Record Not Updated")]
            TransactionHistoryRecordNotUpdated,
            [Description("Entry Record Not Bullion")]
            EntryRecordNotBullion,
        }

        public enum eMailExportStatus
        {
            [Description("Pending")]
            Pending = 0,
            [Description("Failed to Retrieve")]
            FailedToRetrieve = 1,
            [Description("Failed in iCore")]
            FailedIniCore = 2,
            [Description("Retrieved By iCore")]
            RetrievedByiCore = 3,
            [Description("Saved in iCore Node")]
            SavedIniCoreNode = 4,
            [Description("Email Sent")]
            EmailSent = 5,
            [Description("Failed to Send")]
            FailedToSend = 6,
            [Description("Failed to update")]
            FailedToUpdate = 7
        }

        public enum eRecurrenceType
        {
            [Description("Optional")]
            Optional = 1,
            [Description("Mandatory")]
            Mandatory = 2
        }

        public enum CanBePersonalised
        {
            No = 0,
            Yes = 1
        }
        public enum PersonalisationType
        {
            NA = 0,
            Artwork = 1,
            Lettering = 2
        }

        public enum BullionCustomerType
        {
            [Description("Standard")]
            Standard = 1,
            [Description("SIPP Customer")]
            SIPPCustomer = 2,
            [Description("SIPP Provider")]
            SIPPProvider = 3,
        }

        public enum TrmPaymentType
        {
            Purchase,
            AccountTopUp,
            BullionWalletTopUp
        }
        public enum LinkItemType
        {
            Internal = 0,
            External = 1,
            Download = 2,
        }

        public enum LtApplicationStatus
        {
            [Description("Not-Applied (PM only account)")]
            NotApplied = 0,
            [Description("Trustee Account")]
            TrusteeAccount = 1,
            [Description("Beneficiary Approved")]
            BeneficiaryApproved = 1,
            [Description("Beneficiary Pending")]
            BeneficiaryPending = 2,
            [Description("Beneficiary In-Complete")]
            BeneficiaryInComplete = 3
        }

        public enum LtAccountType
        {
            [Description("Parent")]
            Parent = 0,
            [Description("Guardian")]
            Guardian = 1,
            [Description("Grandparent")]
            Grandparent = 2,
            [Description("Other")]
            Other = 3
        }

        public enum LtInvestmentOption
        {
            [Description("Monthly")]
            Monthly = 0,
            [Description("Lumpsum")]
            Lumpsum = 1
        }

        public enum LtPaymentOption
        {
            [Description("Bank transfer")]
            BankTransfer = 0,
            [Description("Direct Debit")]
            DirectDebit = 1
        }

        public enum CustomGroupsEnum
        {
            Customer = Mediachase.Commerce.Customers.CustomerContact.eContactGroup.Customer,
            Partner = Mediachase.Commerce.Customers.CustomerContact.eContactGroup.Partner,
            Distributor = Mediachase.Commerce.Customers.CustomerContact.eContactGroup.Distributor,
            NoVat = 4
        }
    }
}