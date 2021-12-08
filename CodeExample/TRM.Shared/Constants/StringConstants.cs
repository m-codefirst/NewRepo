namespace TRM.Shared.Constants
{
    public class StringConstants
    {
        #region CustomFields
        public static class CustomFields
        {
            public const string GiftMessage = "GiftMessage";
            public const string ObsAccountNumber = "ObsAccountNumber";
            public const string D2CCustomerType = "D2CCustomerType";
            public const string TaxGroup = "TaxGroup";
            public const string InclTax = "InclTax";
            public const string StatementPreference = "StatementPreference";
            public const string DlvMode = "DlvMode";
            public const string Department = "Department";
            public const string CustClassificationId = "CustClassificationId";
            public const string CreditLimitFieldName = "CreditLimit";
            public const string CreditUsedFieldName = "CreditUsed";
            public const string CreditUsedEPiFieldName = "CreditUsedEPi";
            public const string SubscribedFieldName = "Subscribed";
            public const string MandatoryFieldName = "Mandatory";
            public const string ContactClassName = "Contact";
            public const string CreditCardClassName = "CreditCard";
            public const string MarketIdFieldName = "CurrentMarketId";
            public const string ContactTitleField = "Title";
            public const string IsPensionSchemeAdmin = "IsPensionSchemeAdmin";
            public const string ContactByPhone = "ByPhone";
            public const string ContactByEmail = "ByEmail";
            public const string ContactByPost = "ByPost";
            public const string EmailConsentSource = "EmailConsentSource";
            public const string PostConsentSource = "PostConsentSource";
            public const string TelephoneConsentSource = "TelephoneConsentSource";

            public const string EmailConsentDateTime = "EmailConsentDateTime";
            public const string PostConsentDateTime = "PostConsentDateTime";
            public const string TelephoneConsentDateTime = "TelephoneConsentDateTime";

            public const string ContactUpdateStatus = "UpdateStatus";
            public const string ContactUpdateErrorMessage = "UpdateErrorMessage";
            public const string ContactErrorEmailSent = "ContactErrorEmailSent";
            public const string ContactLastUpdatedInObs = "LastUpdatedInObs";
            public const string CreditCardToken = "Token";
            public const string CreditCardExpiry = "CardExpiry";
            public const string CreditCardUseAmex = "AmexPayment";
            public const string CreditCardOtherType = "OtherCardType";
            public const string CreditCardNameOnCard = "CreditCardNameOnCard";
            public const string SendStatus = "SendStatus";
            public const string ErrorEmailSent = "ErrorEmailSent";
            public const string CustomerCode = "CustomerCode";
            public const string PromotionCode = "PromotionCode";
            public const string LinePromotionCode = "LinePromotionCode";
            public const string CarriageCode = "CarriageCode";
            public const string ActivationToken = "ActivationToken";
            public const string MigratedToEpiUser = "MigratedToEpiUser";
            public const string PaymentErrorMessage = "PaymentErrorMessage";
            public const string CustomerLockedOut = "CustomerLockedOut";
            public const string ShippingMessageFieldName = "ShippingMessage";
            public const string MyAccountPaymentOrderNumber = "myAccountPaymentOrderNumber";
            public const string MyAccountPaymentSessionId = "myAccountPaymentSessionId";
            public const string MyAccountPayment3DsId = "myAccountPayment3DsId";
            public const string MyAccountPaymentNameOnCard = "myAccountPaymentNameOnCard";
            public const string MyAccountPaymentMethodId = "myAccountPaymentMethodId";
            public const string MyAccountPaymentIsAmex = "myAccountPaymentIsAmex";
            public const string MyAccountPaymentAmount = "myAccountPaymentAmount";
            public const string MyAccountPaymentPageUrl = "myAccountPaymentPageUrl";
            public const string MyAccountNextPaymentNo = "MyAccountNextPaymentNo";
            public const string MyAccountSaveCard = "MyAccountSaveCard";
            public const string AccPaymentOrderErrorMessage = "accPaymentOrderErrorMessage";
            public const string MyAccountOrderStatus = "OrderStatus";
            public const string CompleteOrder = "Complete";
            public const string CancelOrder = "Cancelled";
            public const string Activated = "Activated";
            public const string AddressClassName = "Address";
            public const string LocationIdFieldName = "LocationId";
            public const string CountyFieldName = "County";
            public const string CompanyFieldName = "Company";
            public const string ContactPersonId = "ContactPersonId";
            public const string Affix = "Affix";
            public const string Mobile = "Mobile";
            public const string Telephone = "Telephone";
            public const string SecondTelephone = "SecondTelephone";
            public const string IsSiteCoreUser = "IsSiteCoreUser";
            public const string Modified = "Modified";
            public const string Created = "Created";
            public const string PurchaseOrderTempData = "PurchaseOrderTempData";
            public const string CustomerServiceActive = "CustomerServiceActive";
            public const string CustomerGroup = "CustomerGroup";
            public const string PostcodeDashes = "-------";
            public const string BeforeText = "Before";
            public const string AfterText = "After";
            public const string AfterPaymentText = "After payment";

            #region Bullion Metafields

            // Generic
            public const string CustomerType = "CustomerCustomerType";
            public const string BullionCustomerType = "BullionCustomerType";
            public const string BullionObsAccountNumber = "CustomerBullionObsAccountNumber";
            public const string BullionObsAccountLastUpdatedDate = "CustomerBullionObsAccountLastUpdatedDate";
            public const string BullionObsAccountUpdateStatus = "CustomerBullionObsAccountUpdateStatus";

            public const string BullionCustomerEffectiveBalance = "BullionCustomerEffectiveBalance";
            public const string BullionCustomerAvailableToSpend = "BullionCustomerAvailableToSpend";
            public const string BullionCustomerAvailableToWithdraw = "BullionCustomerAvailableToWithdraw";
            public const string LifetimeValue = "LifetimeValue";

            public const string BullionNextPaymentNo = "BullionNextPaymentNo";
            public const string BullionPaymentOrderNumber = "myAccountPaymentOrderNumber";
            public const string BullionPaymentObject = "myAccountPaymentObject";

            public const string BullionOrderStatus = "OrderStatus";

            public const string SecondSurname = "SecondSurname";
            public const string Gender = "Gender";
            public const string Balances = "Balances";
            public const string BullionPremiumGroupInt = "BullionPremiumGroupInt";
            public const string CustomerPaymentReference = "CustomerPaymentReference";

            // Restrictions
            public const string BullionUnableToPurchaseBullion = "BullionUnableToPurchaseBullion";
            public const string BullionUnableToSellorDeliverFromVault = "BullionUnableToSellorDeliverFromVault";
            public const string BullionUnableToDepositViaCard = "BullionUnableToDepositViaCard";
            public const string BullionUnableToWithdraw = "BullionUnableToWithdraw";
            public const string BullionUnableToLogin = "BullionUnableToLogin";

            // KYC Fields
            public const string BullionKycStatus = "BullionKYCStatus";
            public const string KycStatus = "KYCStatus";
            public const string KycNeeded = "KYCNeeded";
            public const string KycStatusNotification = "KYCStatusNotification";
            public const string TotalSpendSinceKyc = "TotalSpendSinceKyc";

            public const string BullionKycApiResponse = "BullionKYCApiResponse";
            public const string BullionKycAddress = "BullionKycAddress";

            // Auto Invest Fields
            public const string AutoInvestApplicationDate = "LtApplicationDate";
            public const string AutoInvestProductCode = "LtAutoInvestProductCode";
            public const string IsAutoInvest = "IsAutoInvest";
            public const string AutoInvestDay = "LtAutoInvestDay";
            public const string AutoInvestAmount = "LtAutoInvestAmountDecimal";
            public const string LastAutoInvestDate = "LastAutoInvestDate";
            public const string LastAutoInvestStatus = "LastAutoInvestStatus";
            public const string TrusteePaymentMethod = "TrusteePaymentMethod";

            // 2 Step Authentication Fields
            public const string TwoStepAuthenticationQuestion = "TwoStepAuthenticationQuestion";
            public const string TwoStepAuthenticationAnswer = "TwoStepAuthenticationAnswer";

            //Pension Customer
            public const string SendEmailTo = "SendEmailTo";

            #endregion

            public const string IsGuest = "IsGuest";
            public const string GuestContactId = "GuestContactId";
            public const string DeliveryTempData = "DeliveryTempData";
            public const string UserRegistrationTempData = "UserRegistrationTempData";
            public const string IsGiftOrder = "IsGiftOrder";
            public const string ShipComplete = "ShipComplete";
            public const string FullName = "FullName";
            public const string RegistrationSource = "RegistrationSource";
            public const string ContactId = "ContactId";
            public const string BullionRegistrationTempData = "BullionRegistrationTempData";
            public const string ConsumerPayment = "MixedCheckoutConsumerPayment";
            public const string TransactionPerformedBy = "TransactionPerformedBy";
            public const string TransactionPerformedByUserId = "TransactionPerformedByUserId";
            public const string ExportTransactionId = "ExportTransactionId";
            public const string IsForMixedCheckout = "IsForMixedCheckout";

            #region Order Group Metafield

            public const string OrderGroup = "OrderGroup";
            public const string Type = "Type";
            public const string Currency = "Currency";
            public const string BullionPAMPRequestQuoteId = "BullionPAMPRequestQuoteId";
            public const string BullionPAMPQuoteId = "BullionPAMPQuoteId";
            public const string BullionPAMPTradeReferenceId = "BullionPAMPTradeReferenceId";
            public const string BullionPAMPStatus = "BullionPAMPStatus";
            public const string ShippingVAT = "ShippingVAT";
            public const string ShippingVATCost = "ShippingVATCost";
            public const string BullionVATAmount = "BullionVATAmount";

            #endregion

            #region Order Line Item

            public const string BullionDeliver = "BullionDeliver";
            public const string BullionVATStatus = "BullionVATStatus";
            public const string BullionVATRate = "BullionVATRate";
            public const string BullionVATCost = "BullionVATCost";

            public const string BullionSignatureValueRequested = "BullionSignatureValueRequested";
            //Store total weight = weight per line item * quantity
            public const string BullionWeight = "BullionWeight";
            //Pamp Metal Price Per Unit Without Premium
            public const string BullionPAMPMetalCost = "BullionPAMPMetalCost";
            //Pamp Metal Price Per Unit Without Premium
            public const string PampMetalPricePerUnitWithoutPremium = "PampMetalPrice";
            //Store metal price per 1 OZ which get from Pamp
            public const string PampMetalPricePerOneOz = "BullionPampQuoteMetalPrice";
            //Store Total pamp metal price = PampMetalPricePerUnitWithoutPremium * LineItem's Quantity
            public const string ExVatLineTotalMetalPrice = "ExVatLineTotalMetalPrice";
            //Premium amount per one line item
            public const string BullionBuyPremiumCost = "BullionBuyPremiumCost";
            //Total line item discounted amount
            public const string LineItemEntryDiscountAmount = "LineItemEntryDiscountAmount";

            public const string BullionStoragePremiumInitialRate = "BullionStoragePremiumInitialRate";
            public const string BullionStoragePremiumInitialPeriod = "BullionStoragePremiumInitialPeriod";
            public const string BullionStoragePremiumSubsequentRate = "BullionStoragePremiumSubsequentRate";
            //
            // Bullion
            //
            // LineItem
            public const string BullionAdjustedTotalPrice = "BullionAdjustedTotalPrice";
            public const string BullionNeedAdjustTotalPrice = "BullionIsAjustedTotalPrice";

            // PurchaseOrder
            public const string BullionRequestQuotationId = "BullionRequestQuotationId";
            public const string BullionQuotationTradeReferenceId = "BullionQuotationTradeReferenceId";
            public const string BullionCustomerFullName = "BullionCustomerFullName";
            public const string BullionCustomerEmailAddress = "BullionCustomerEmailAddress";
            public const string BullionCurrentCustomerOrganization = "BullionCurrentCustomerOrganization";

            public const string IsByMintMarque = "IsByMintMarque";
            public const string UpdatedWithPampQuote = "UpdatedWithPampQuote";

            public const string BullionAdjustedTotalPriceIncludePremiums = "BullionAdjustedTotalPriceIncludePremiums";
            public const string BullionAdjustedInvestmentQuantity = "BullionAdjustedInvestmentQuantity";

            #endregion

            #region ShipmentEx

            public const string ShippingBookingCode = "ShippingBookingCode";
            public const string ShippingPrice = "ShippingPrice";

            #endregion

            #region Episerver Organisations

            public const string CompanyName = "CompanyName";
            public const string CompanyPhone = "CompanyPhone";
            public const string CompanyAddress = "CompanyAddress";
            public const string SchemeType = "SchemeType";
            public const string HMRCTaxReference = "HMRCTaxReference";

            #endregion

            public const string PersonalisationUniqueId = "PersonalisationUniqueId";
            public const string PersonalisationImageId = "PersonalisationImageId";
            public const string PersonalisationType = "LineItemPersonalisationType";
            public const string PersonalisationCharge = "LineItemPersonalisationCharge";
            public const string PersonalisationResponse = "PersonalisationResponse";
            public const string PersonalisationDetails = "PersonalisationDetails";
            public const string Customer3dsTransactionId = "Customer3dsTransactionId";
            public const string EnhancedCheckNeeded = "EnhancedCheckNeeded";
            public const string KycDate = "KycDate";
            public const string CouponCode = "CouponCode";
        }

        #region ConnectionStringNames
        public const string TrmCustomDatabaseName = "EcfSqlConnection";
        public const string BullionCustomDatabaseName = "EPiServerDB";
        #endregion

        public static class AddressFieldNames
        {
            public static string LocationId = "LocationId";
            public static string County = "County";
        }

        public static class CustomMetaFieldTypeNames
        {
            public static string SecurityQuestion = "SecurityQuestion";
            public static string BullionCustomerType = "BullionCustomerType";
            public static string PensionCustomerSendEmailTo = "PensionCustomerSendEmailTo";
            public static string AccountKycStatus = "AccountKycStatus";

            // Auto Invest TypeNames
            public static string BeneficiaryAppAccountType = "AccountKycStatus";
            public static string LtApplicationStatus = "AccountKycStatus";
            public static string TrusteePaymentMethod = "TrusteePaymentMethod";
        }

        #endregion

        #region Payments
        public static class Payments
        {
            public const string PaymentMethodId = "paymentMethod";
            public const string PaymentSessionId = "paymentSessionId";
            public const string PaymentNameOnCard = "nameOnCard";
            public const string CreditPayment = "CreditPayment";
            public const string Mastercard = "Mastercard";
            public const string SaveCard = "SaveCard";
            public const string SelectedCard = "SelectedCard";
            public const string WalletPayment = "WalletPayment";
            public const string BarclaysCard = "BarclaysCard";
        }

        #endregion

        #region CustomGroups
        public static class CustomGroups
        {
            public const string NoVat = "NoVat";

        }
        #endregion

        #region Default Values

        public static class DefaultValues
        {
            public const string CountryCode = "GBR";
            public const string CurrencyCode = "GBP";
            public const string PrintzwareErrorMessage = "ERROR - Missing JSON no response from Printzware";
        }

        #endregion

        #region Sql Queries

        public static class SqlQueries
        {
            public static string EpiserverPurchaseOrderQuery = "OrderGroupId IN (SELECT ObjectId FROM OrderGroup_PurchaseOrder po LEFT JOIN cls_Contact c ON OrderGroup.CustomerId = c.ContactId WHERE po.SendStatus < 3 AND (c.ObsAccountNumber <> NULL or c.ObsAccountNumber <>\'\'))";
        }
        #endregion

        #region Shipping Fields

        public static class ShippingFields
        {
            public const string MinOrderAmount = "MinOrderAmount";
            public const string MaxOrderAmount = "MaxOrderAmount";
            public const string AllItemsMustBeInStock = "AllItemsMustBeInStock";
            public const string CutOffTime = "CutOffTime";
            public const string AvailableMonday = "AvailableMonday";
            public const string AvailableTuesday = "AvailableTuesday";
            public const string AvailableWednesday = "AvailableWednesday";
            public const string AvailableThursday = "AvailableThursday";
            public const string AvailableFriday = "AvailableFriday";
            public const string AvailableSaturday = "AvailableSaturday";
            public const string AvailableSunday = "AvailableSunday";
            public const string ObsMethodName = "ObsMethodName";
            public const string ForGiftingOnly = "ForGiftingOnly";
            public const string MinWeight = "MinWeight";
            public const string MaxWeight = "MaxWeight";
        }

        public static class ShippingParameters
        {
            public const string UserKey = "UserKey";
            public const string ApiUrl = "ApiUrl";
        }

        #endregion

        #region Google

        public static class GoogleSearch
        {
            public static string Article = "GoogleSearchArticle";
        }

        #endregion

        public static string Customer = "Customer";
        public static string Sippssas = "SIPP/SSAS Provider";
        public static string RoyalMint = "Royal Mint";
        public const string MetapackShippingProvider = "MetapackShippingProvider";
        public const string Anonymous = "Anonymous";
        public const string Email = "Email";
        public const string DefaultRegisteredAddressName = "Registered";
        public const string HasAutomatedCredit = "HasAutomatedCredit";
        public const string DefaultVaultShippingAddressName = "VaultShippingAddress";
        public const string PreciousMetalsRegisteredAddressName = "PreciousMetalsRegistered";

        public static class RegistrationSource
        {
            public const string CheckoutPageSource = "Checkout page";
            public const string ConsumerRegistrationPage = "Registration page";
            public const string LtRegistrationPage = "Little Treasures registration page";
            public const string BullionRegistrationPage = "Bullion registration page";
            public const string ImportedFromGbi = "Imported from GBI";
            public const string ImportedFromAx = "Imported from Ax";
        }

        #region Customer Type
        public static class CustomerType
        {
            public const string Consumer = "Consumer";
            public const string Bullion = "Bullion";
            public const string ConsumerAndBullion = "Consumer and Bullion";
            public const string CustomerService = "CustomerService";
        }
        #endregion

        #region General 

        public const string DefaultIpAddress = "0.0.0.0";
        #endregion

        #region Barclays Payment Provider - Fields
        public static class BarclaysPaymentProvider
        {
            public const string Card = "card";
            public static class RequestFields
            {
                public const string ProfileId = "profile_id";
                public const string AccessKey = "access_key";
                public const string TransactionUuid = "transaction_uuid";
                public const string SignedFieldNames = "signed_field_names";
                public const string UnsignedFieldNames = "unsigned_field_names";
                public const string SignedDateTime = "signed_date_time";
                public const string Locale = "locale";
                public const string TransactionType = "transaction_type";
                public const string ReferenceNumber = "reference_number";
                public const string Amount = "amount";
                public const string Currency = "currency";
                public const string PaymentMethod = "payment_method";
                public const string BillToForename = "bill_to_forename";
                public const string BillToSurname = "bill_to_surname";
                public const string BillToEmail = "bill_to_email";
                public const string BillToPhone = "bill_to_phone";
                public const string BillToAddressLine1 = "bill_to_address_line1";
                public const string BillToAddressCity = "bill_to_address_city";
                public const string BillToAddressState = "bill_to_address_state";
                public const string BillToAddressCountry = "bill_to_address_country";
                public const string BillToAddressPostalCode = "bill_to_address_postal_code";
                public const string OverrideCustomReceiptPage = "override_custom_receipt_page";
                public const string LineItemCount = "line_item_count";
                // string.Format(Item_Code_FormatStringForNum, 1)
                public const string ItemCode_FormatStringForNum = "item_{0}_code";
                public const string ItemUnitPrice_FormatStringForNum = "item_{0}_unit_price";
                public const string ItemName_FormatStringForNum = "item_{0}_name";
                public const string ItemSKU_FormatStringForNum = "item_{0}_sku";
                public const string ItemQuantity_FormatStringForNum = "item_{0}_quantity";

                public const string Signature = "signature";
            }

            public static class TrasactionTypes
            {
                public const string Authorization = "authorization";
                public const string AuthorizationAndCreatePaymentTocken = "authorization,create_payment_token";
                public const string AuthorizationAndUpdatePaymentTocken = "authorization,update_payment_token,create_payment_token";
                public const string Sale = "sale";
                public const string SaleAndCreatePaymentToken = "sale,create_payment_token";
                public const string SaleAndUpdatePaymentToken = "sale,update_payment_token";
                public const string CreatePaymentToken = "create_payment_token";
                public const string UpdatePaymentToken = "update_payment_token";

            }
        }
        #endregion Barclays Payment Provider
    }
}
