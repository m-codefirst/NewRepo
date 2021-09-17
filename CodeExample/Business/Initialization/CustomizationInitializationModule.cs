using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;
using System;
using System.Linq;
using Mediachase.Commerce.Customers;
using TRM.Shared.Models.DTOs;
using TRM.Web.Helpers;
using static TRM.Shared.Constants.Enums;
using static TRM.Shared.Constants.StringConstants;
using MetaClass = Mediachase.BusinessFoundation.Data.Meta.Management.MetaClass;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class CustomizationInitializationModule : IInitializableModule
    {
        private readonly Injected<IMetaFieldTypeHelper> _metaFieldTypeHelper;
        private bool _initialized;

        public void Initialize(InitializationEngine context)
        {
            if (_initialized) return;
            //Create custom meta enums
            CreateEnumMetaFieldType();

            //Add custom fields for Contact class
            AddCustomFieldsForContact();

            //This will Add the NoVat ContactGroup
            AddContactGroup(CustomGroups.NoVat);

            //Add fields credit card class
            AddCustomFieldsForCreditCart();

            //Add fields for purchase order class
            AddCustomFieldsForPurchaseOrder();

            //Add custom fields for shopping cart class
            AddCustomFieldsForShoppingCart();

            //Add fields to purchase order Line item
            AddCustomFieldsForLineItem();

            //Add fields to Order Form Ex 
            AddCustomFieldsForOrderFormEx();

            //Add fields to Order Form Ex 
            AddCustomFieldsForShipmentEx();

            //Add fields to Address 
            AddCustomFieldsForAddressClass();

            // Add fields to Organization
            AddCustomFieldsForOrganization();

            _initialized = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
            if (!_initialized) return;

            _initialized = false;
        }

        private void AddCustomFieldsForContact()
        {
            //OBS Account Number
            AddCustomField(CustomFields.ContactClassName, CustomFields.ObsAccountNumber, MetaFieldType.Text, string.Empty);

            //D2C Customer Type
            AddCustomField(CustomFields.ContactClassName, CustomFields.D2CCustomerType, MetaFieldType.Text, string.Empty);

            //Tax Group
            AddCustomField(CustomFields.ContactClassName, CustomFields.TaxGroup, MetaFieldType.Text, string.Empty);

            //Incl Tax
            AddCustomField(CustomFields.ContactClassName, CustomFields.InclTax, MetaFieldType.CheckboxBoolean, string.Empty);

            //Statement Preference
            AddCustomField(CustomFields.ContactClassName, CustomFields.StatementPreference, MetaFieldType.Text, string.Empty);

            //Dlv Mode
            AddCustomField(CustomFields.ContactClassName, CustomFields.DlvMode, MetaFieldType.Text, string.Empty);

            //Department
            AddCustomField(CustomFields.ContactClassName, CustomFields.Department, MetaFieldType.Text, string.Empty);

            //Cust Classification Id
            AddCustomField(CustomFields.ContactClassName, CustomFields.CustClassificationId, MetaFieldType.Text, string.Empty);

            //Market Id
            AddCustomField(CustomFields.ContactClassName, CustomFields.MarketIdFieldName, MetaFieldType.Text, string.Empty);

            //Credit Limit from AX
            AddCustomField(CustomFields.ContactClassName, CustomFields.CreditLimitFieldName, MetaFieldType.Currency, string.Empty);

            //Bullion Customer Effective balance
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionCustomerEffectiveBalance, MetaFieldType.Currency, string.Empty);

            //Bullion Customer Available To Spend
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionCustomerAvailableToSpend, MetaFieldType.Currency, string.Empty);

            //Bullion Customer Available To Withdraw
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionCustomerAvailableToWithdraw, MetaFieldType.Currency, string.Empty);

            //Customer Lifetime Value - (Value of all orders from customer)
            AddCustomField(CustomFields.ContactClassName, CustomFields.LifetimeValue, MetaFieldType.Currency, string.Empty);

            //Credit Limit Used from AX
            AddCustomField(CustomFields.ContactClassName, CustomFields.CreditUsedFieldName, MetaFieldType.Currency, string.Empty);

            //Credit Limit Used updated by EPiServer 
            AddCustomField(CustomFields.ContactClassName, CustomFields.CreditUsedEPiFieldName, MetaFieldType.Currency, string.Empty);

            //Title
            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactTitleField, MetaFieldType.Text, string.Empty);

            //Contact Preferences By Email
            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactByEmail, MetaFieldType.CheckboxBoolean, "1");
            AddCustomField(CustomFields.ContactClassName, CustomFields.EmailConsentDateTime, MetaFieldType.DateTime, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.EmailConsentSource, MetaFieldType.Text, string.Empty);
            //Contact Preferences By Telephone
            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactByPhone, MetaFieldType.CheckboxBoolean, "1");
            AddCustomField(CustomFields.ContactClassName, CustomFields.TelephoneConsentDateTime, MetaFieldType.DateTime, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.TelephoneConsentSource, MetaFieldType.Text, string.Empty);
            //Contact Preferences By Post
            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactByPost, MetaFieldType.CheckboxBoolean, "1");
            AddCustomField(CustomFields.ContactClassName, CustomFields.PostConsentDateTime, MetaFieldType.DateTime, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.PostConsentSource, MetaFieldType.Text, string.Empty);

            //Customer Lockout
            AddCustomField(CustomFields.ContactClassName, CustomFields.CustomerLockedOut, MetaFieldType.CheckboxBoolean, "0");

            //Customer Migration
            AddCustomField(CustomFields.ContactClassName, CustomFields.Activated, MetaFieldType.CheckboxBoolean, "1");

            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPaymentOrderNumber, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPaymentSessionId, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPayment3DsId, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPaymentNameOnCard, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPaymentMethodId, MetaFieldType.Guid, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPaymentIsAmex, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPaymentAmount, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountPaymentPageUrl, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.AccPaymentOrderErrorMessage, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountNextPaymentNo, MetaFieldType.Integer, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.MyAccountSaveCard, MetaFieldType.CheckboxBoolean, string.Empty);

            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionPaymentOrderNumber, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionPaymentObject, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionNextPaymentNo, MetaFieldType.Integer, string.Empty);

            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactUpdateStatus, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactUpdateErrorMessage, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactLastUpdatedInObs, MetaFieldType.DateTime, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.IsSiteCoreUser, MetaFieldType.CheckboxBoolean, "0");
            AddCustomField(CustomFields.ContactClassName, CustomFields.PurchaseOrderTempData, MetaFieldType.LongText, string.Empty);

            //Add Customer Contact fields to Contact
            AddCustomField(CustomFields.ContactClassName, CustomFields.ContactPersonId, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.Affix, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.Mobile, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.Telephone, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.SecondTelephone, MetaFieldType.Text, string.Empty);

            //Add field to flag Contact as Guest
            AddCustomField(CustomFields.ContactClassName, CustomFields.IsGuest, MetaFieldType.CheckboxBoolean, string.Empty);

            #region Bullion Metafields

            // Generic
            AddCustomField(CustomFields.ContactClassName, CustomFields.CustomerType, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionCustomerType, CustomMetaFieldTypeNames.BullionCustomerType, string.Empty, editable: true);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionObsAccountNumber, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionObsAccountLastUpdatedDate, MetaFieldType.DateTime, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionObsAccountUpdateStatus, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.SecondSurname, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.Gender, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.Balances, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionPremiumGroupInt, MetaFieldType.Integer, 0.ToString(), editable: true);
            AddCustomField(CustomFields.ContactClassName, CustomFields.CustomerPaymentReference, MetaFieldType.Text, string.Empty);

            // Restrictions
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionUnableToPurchaseBullion, MetaFieldType.CheckboxBoolean, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionUnableToSellorDeliverFromVault, MetaFieldType.CheckboxBoolean, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionUnableToDepositViaCard, MetaFieldType.CheckboxBoolean, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionUnableToWithdraw, MetaFieldType.CheckboxBoolean, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionUnableToLogin, MetaFieldType.CheckboxBoolean, string.Empty);

            // KYC Fields
            AddCustomField(CustomFields.ContactClassName, CustomFields.BullionKycApiResponse, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.KycDate, MetaFieldType.DateTime, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.EnhancedCheckNeeded, MetaFieldType.CheckboxBoolean, "0");
            AddCustomField(CustomFields.ContactClassName, CustomFields.KycStatus, CustomMetaFieldTypeNames.AccountKycStatus, string.Empty, editable: true);
            AddCustomField(CustomFields.ContactClassName, CustomFields.KycStatusNotification, MetaFieldType.DateTime, string.Empty, editable: true);
            AddCustomField(CustomFields.ContactClassName, CustomFields.TotalSpendSinceKyc, MetaFieldType.Currency, string.Empty);

            // LT Fields
            AddCustomField(CustomFields.ContactClassName, CustomFields.AutoInvestApplicationDate, MetaFieldType.DateTime, string.Empty); //?
            AddCustomField(CustomFields.ContactClassName, CustomFields.AutoInvestProductCode, MetaFieldType.LongText, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.IsAutoInvest, MetaFieldType.CheckboxBoolean, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.AutoInvestDay, MetaFieldType.Integer, 0.ToString());
            AddCustomField(CustomFields.ContactClassName, CustomFields.AutoInvestAmount, MetaFieldType.Currency, 0.ToString());
            AddCustomField(CustomFields.ContactClassName, CustomFields.LastAutoInvestDate, MetaFieldType.DateTime, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.LastAutoInvestStatus, MetaFieldType.Integer, 0.ToString());

            // 2 Step Authentication Fields
            AddCustomField(CustomFields.ContactClassName, CustomFields.TwoStepAuthenticationQuestion, CustomMetaFieldTypeNames.SecurityQuestion, string.Empty, editable: true);
            AddCustomField(CustomFields.ContactClassName, CustomFields.TwoStepAuthenticationAnswer, MetaFieldType.LongText, string.Empty);

            AddCustomField(CustomFields.ContactClassName, CustomFields.ConsumerPayment, MetaFieldType.LongText, string.Empty);

            AddCustomField(CustomFields.ContactClassName, CustomFields.SendEmailTo, CustomMetaFieldTypeNames.PensionCustomerSendEmailTo, string.Empty, editable: true);
            #endregion

            //Impersonate fields
            AddCustomField(CustomFields.ContactClassName, CustomFields.TransactionPerformedBy, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.ContactClassName, CustomFields.TransactionPerformedByUserId, MetaFieldType.Text, string.Empty);
            
            // remove not used fields
            DeleteMetaField(CustomFields.ContactClassName, CustomFields.MigratedToEpiUser);
            DeleteMetaField(CustomFields.ContactClassName, CustomFields.ContactErrorEmailSent);
        }

        private void AddCustomFieldsForPurchaseOrder()
        {
            //To be removed once these changes have been made live 
            //Strings not constants to avoid confusion 
            DeleteCustomFieldForPurchaseOrder("SentSuccess");
            DeleteCustomFieldForPurchaseOrder("SendFail");

            //Add fields to Purchase Order
            AddCustomFieldForPurchaseOrder(CustomFields.ErrorEmailSent, CustomFields.ErrorEmailSent, 1, MetaDataType.Boolean);
            AddCustomFieldForPurchaseOrder(CustomFields.SendStatus, CustomFields.SendStatus, 1, MetaDataType.Integer);
            AddCustomFieldForPurchaseOrder(CustomFields.CustomerCode, CustomFields.CustomerCode, 20, MetaDataType.ShortString);
            AddCustomFieldForPurchaseOrder(CustomFields.PromotionCode, CustomFields.PromotionCode, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.GiftMessage, CustomFields.GiftMessage, 350, MetaDataType.LongString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.IsGiftOrder, CustomFields.IsGiftOrder, 1, MetaDataType.Boolean);
            AddCustomFieldForPurchaseOrder(CustomFields.ShipComplete, CustomFields.ShipComplete, 1, MetaDataType.Boolean);

            AddCustomFieldForPurchaseOrder(CustomFields.BullionPAMPRequestQuoteId, CustomFields.BullionPAMPRequestQuoteId, 255, MetaDataType.LongString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.BullionPAMPQuoteId, CustomFields.BullionPAMPQuoteId, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.BullionPAMPTradeReferenceId, CustomFields.BullionPAMPTradeReferenceId, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.BullionPAMPStatus, CustomFields.BullionPAMPStatus, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.ShippingVAT, CustomFields.ShippingVAT, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.ShippingVATCost, CustomFields.ShippingVATCost, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.Currency, CustomFields.Currency, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.Type, CustomFields.Type, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.BullionVATAmount, CustomFields.BullionVATAmount, 20, MetaDataType.ShortString, true);

            AddCustomFieldForPurchaseOrder(CustomFields.TransactionPerformedBy, CustomFields.TransactionPerformedBy, 20, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.TransactionPerformedByUserId, CustomFields.TransactionPerformedByUserId, 60, MetaDataType.ShortString, true);
            AddCustomFieldForPurchaseOrder(CustomFields.ExportTransactionId, CustomFields.ExportTransactionId, 350, MetaDataType.LongString, true);
        }

        private void AddCustomFieldsForOrderFormEx()
        {
            var mdContext = CatalogContext.MetaDataContext;
            const string orderNamespace = "Mediachase.Commerce.Orders.System";
            const string orderFormEx = "OrderFormEx";

            AddMetaFieldToClass(mdContext, orderNamespace, orderFormEx, CustomFields.BullionRequestQuotationId, MetaDataType.LongString, 255, true, false);
            AddMetaFieldToClass(mdContext, orderNamespace, orderFormEx, CustomFields.BullionQuotationTradeReferenceId, MetaDataType.LongString, 255, true, false);
            AddMetaFieldToClass(mdContext, orderNamespace, orderFormEx, CustomFields.BullionCustomerFullName, MetaDataType.LongString, 255, true, false, 9, 38);
            AddMetaFieldToClass(mdContext, orderNamespace, orderFormEx, CustomFields.BullionCustomerEmailAddress, MetaDataType.LongString, 255, true, false, 9, 38);
            AddMetaFieldToClass(mdContext, orderNamespace, orderFormEx, CustomFields.BullionCurrentCustomerOrganization, MetaDataType.LongString, 255, true, false, 9, 38);
            AddMetaFieldToClass(mdContext, orderNamespace, orderFormEx, CustomFields.IsByMintMarque, MetaDataType.Boolean, 1, true, false);
        }

        private void AddCustomFieldsForShipmentEx()
        {
            var mdContext = CatalogContext.MetaDataContext;
            const string orderNamespace = "Mediachase.Commerce.Orders.System";
            const string shipmentEx = "ShipmentEx";

            AddMetaFieldToClass(mdContext, orderNamespace, shipmentEx, CustomFields.ShippingBookingCode, MetaDataType.LongString, 255, true, false);
            AddMetaFieldToClass(mdContext, orderNamespace, shipmentEx, CustomFields.ShippingPrice, MetaDataType.ShortString, 20, true, false);
        }

        private void AddCustomFieldsForShoppingCart()
        {
            //Shopping cart
            AddCustomFieldForShoppingCart(CustomFields.PaymentErrorMessage, CustomFields.PaymentErrorMessage, 255, MetaDataType.ShortString);
            AddCustomFieldForShoppingCart(CustomFields.GuestContactId, CustomFields.GuestContactId, 255, MetaDataType.ShortString);
            AddCustomFieldForShoppingCart(CustomFields.DeliveryTempData, CustomFields.DeliveryTempData, int.MaxValue, MetaDataType.Text);
            AddCustomFieldForShoppingCart(CustomFields.UserRegistrationTempData, CustomFields.UserRegistrationTempData, int.MaxValue, MetaDataType.Text);
            AddCustomFieldForShoppingCart(CustomFields.BullionRegistrationTempData, CustomFields.BullionRegistrationTempData, int.MaxValue, MetaDataType.Text);
            AddCustomFieldForShoppingCart(CustomFields.IsForMixedCheckout, CustomFields.IsForMixedCheckout, 1, MetaDataType.Boolean);
            AddCustomFieldForShoppingCart(CustomFields.CouponCode, CustomFields.CouponCode, 255, MetaDataType.ShortString);

            CopyMetaFieldToMetaClass(CustomFields.GiftMessage, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.IsGiftOrder, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.Type, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.Currency, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.BullionPAMPRequestQuoteId, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.BullionPAMPQuoteId, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.BullionPAMPTradeReferenceId, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.BullionPAMPStatus, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.ShippingVAT, OrderContext.Current.ShoppingCartMetaClass.Name);
            CopyMetaFieldToMetaClass(CustomFields.ShippingVATCost, OrderContext.Current.ShoppingCartMetaClass.Name);
        }

        private void AddCustomFieldsForLineItem()
        {
            //Subscribed for optional continuity products 
            AddCustomLineItemField(CustomFields.SubscribedFieldName, CustomFields.SubscribedFieldName, 1, MetaDataType.Boolean);
            //Shipping Message for displaying after stock has been removed for shipping
            AddCustomLineItemField(CustomFields.ShippingMessageFieldName, CustomFields.ShippingMessageFieldName, 1, MetaDataType.LongString, true);
            //Mandatory flag for mandatory recurring payments
            AddCustomLineItemField(CustomFields.MandatoryFieldName, CustomFields.MandatoryFieldName, 1, MetaDataType.Boolean);
            // Storage Fields
            AddCustomLineItemField(CustomFields.CarriageCode, CustomFields.CarriageCode, 10, MetaDataType.ShortString);
            AddCustomLineItemField(CustomFields.LinePromotionCode, CustomFields.LinePromotionCode, 20, MetaDataType.ShortString, true);

            AddCustomLineItemField(CustomFields.BullionDeliver, CustomFields.BullionDeliver, 20, MetaDataType.Integer, true);
            AddCustomLineItemField(CustomFields.BullionVATStatus, CustomFields.BullionVATStatus, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionVATRate, CustomFields.BullionVATRate, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.PampMetalPricePerUnitWithoutPremium, CustomFields.PampMetalPricePerUnitWithoutPremium, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionAdjustedTotalPriceIncludePremiums, CustomFields.BullionAdjustedTotalPriceIncludePremiums, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionVATCost, CustomFields.BullionVATCost, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.LineItemEntryDiscountAmount, CustomFields.LineItemEntryDiscountAmount, 20, MetaDataType.ShortString, true);

            AddCustomLineItemField(CustomFields.BullionSignatureValueRequested, CustomFields.BullionSignatureValueRequested, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionWeight, CustomFields.BullionWeight, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionPAMPMetalCost, CustomFields.BullionPAMPMetalCost, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionBuyPremiumCost, CustomFields.BullionBuyPremiumCost, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionStoragePremiumInitialRate, CustomFields.BullionStoragePremiumInitialRate, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionStoragePremiumInitialPeriod, CustomFields.BullionStoragePremiumInitialPeriod, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.BullionStoragePremiumSubsequentRate, CustomFields.BullionStoragePremiumSubsequentRate, 20, MetaDataType.ShortString, true);
            AddCustomLineItemField(CustomFields.ExVatLineTotalMetalPrice, CustomFields.ExVatLineTotalMetalPrice, 20, MetaDataType.ShortString, true);

            AddCustomLineItemField(CustomFields.PersonalisationUniqueId, CustomFields.PersonalisationUniqueId, 50, MetaDataType.LongString, true);
            AddCustomLineItemField(CustomFields.PersonalisationImageId, CustomFields.PersonalisationImageId, 50, MetaDataType.LongString, true);
            AddCustomLineItemField(CustomFields.PersonalisationCharge, CustomFields.PersonalisationCharge, 50, MetaDataType.LongString, true);
            AddCustomLineItemField(CustomFields.PersonalisationType, CustomFields.PersonalisationType, 50, MetaDataType.LongString, true);
            AddCustomLineItemField(CustomFields.PersonalisationResponse, CustomFields.PersonalisationResponse, 50, MetaDataType.LongString, true);
            AddCustomLineItemField(CustomFields.PersonalisationDetails, CustomFields.PersonalisationDetails, 5000, MetaDataType.LongString, true);

        }

        private void AddCustomFieldsForCreditCart()
        {
            AddCustomField(CustomFields.CreditCardClassName, CustomFields.CreditCardToken, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.CreditCardClassName, CustomFields.CreditCardUseAmex, MetaFieldType.CheckboxBoolean, string.Empty);
            AddCustomField(CustomFields.CreditCardClassName, CustomFields.CreditCardOtherType, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.CreditCardClassName, CustomFields.CreditCardExpiry, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.CreditCardClassName, CustomFields.CreditCardNameOnCard, MetaFieldType.Text, string.Empty);
        }

        private void AddCustomFieldsForOrganization()
        {
            AddCustomField(OrganizationEntity.ClassName, CustomFields.CompanyName, MetaFieldType.Text, string.Empty);
            AddCustomField(OrganizationEntity.ClassName, CustomFields.CompanyPhone, MetaFieldType.Text, string.Empty);
            AddCustomField(OrganizationEntity.ClassName, CustomFields.SchemeType, MetaFieldType.Text, string.Empty);
            AddCustomField(OrganizationEntity.ClassName, CustomFields.HMRCTaxReference, MetaFieldType.Text, string.Empty);
        }

        private void AddCustomFieldsForAddressClass()
        {
            AddCustomField(CustomFields.AddressClassName, CustomFields.LocationIdFieldName, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.AddressClassName, CustomFields.CountyFieldName, MetaFieldType.Text, string.Empty);
            AddCustomField(CustomFields.AddressClassName, CustomFields.BullionKycAddress, MetaFieldType.CheckboxBoolean, string.Empty);
        }

        private void AddContactGroup(string groupName)
        {
            foreach (MetaFieldType fieldType in DataContext.Current.MetaModel.RegisteredTypes)
            {
                if (fieldType.Name != "ContactGroup") continue;

                if (!fieldType.EnumItems.Any(item => item.Name.Equals(groupName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MetaEnum.AddItem(fieldType, groupName, fieldType.EnumItems.Last().OrderId + 1);
                }
                break;
            }
        }

        #region Create custom meta enums and sync DDS to those enums

        private void CreateEnumMetaFieldType()
        {
            _metaFieldTypeHelper.Service.CreateEnumMetaFieldType(CustomMetaFieldTypeNames.SecurityQuestion, typeof(SecurityQuestionType));
            _metaFieldTypeHelper.Service.CreateEnumMetaFieldType(CustomMetaFieldTypeNames.BullionCustomerType, typeof(BullionCustomerTypeEnum));
            _metaFieldTypeHelper.Service.CreateEnumMetaFieldType(CustomMetaFieldTypeNames.PensionCustomerSendEmailTo, typeof(EmailSettingsForPensionCustomerEnum));
            _metaFieldTypeHelper.Service.CreateEnumMetaFieldType(CustomMetaFieldTypeNames.AccountKycStatus, typeof(AccountKycStatus));
        }

        #endregion

        #region Another way to add custom metafield

        private void AddMetaFieldToClass(MetaDataContext mdContext, string metaDataNamespace, string metaClassName,
            string metaFieldName, MetaDataType type, int length, bool allowNulls, bool cultureSpecific, int optScale = 2, int optPrecision = 18)
        {
            var metaField = CreateMetaField(mdContext, metaDataNamespace, metaFieldName, type, length, allowNulls,
                cultureSpecific, optScale, optPrecision);
            JoinField(mdContext, metaField, metaClassName);
        }

        private Mediachase.MetaDataPlus.Configurator.MetaField CreateMetaField(MetaDataContext mdContext, string metaDataNamespace, string metaFieldName, MetaDataType type, int length, bool allowNulls, bool cultureSpecific, int optScale = 2, int optPrecision = 18)
        {
            var metaField = Mediachase.MetaDataPlus.Configurator.MetaField.Load(mdContext, metaFieldName) ??
                    Mediachase.MetaDataPlus.Configurator.MetaField.Create(mdContext, metaDataNamespace, metaFieldName, metaFieldName, string.Empty, type, length, allowNulls, cultureSpecific, false, false);

            if (type != MetaDataType.Decimal) return metaField;

            metaField.Attributes[MetaFieldAttributeConstants.MdpPrecisionAttributeName] = optPrecision.ToString();
            metaField.Attributes[MetaFieldAttributeConstants.MdpScaleAttributeName] = optScale.ToString();
            return metaField;
        }

        private void JoinField(MetaDataContext mdContext, Mediachase.MetaDataPlus.Configurator.MetaField field, string metaClassName)
        {
            var cls = Mediachase.MetaDataPlus.Configurator.MetaClass.Load(mdContext, metaClassName);
            if (!MetaFieldIsNotConnected(field, cls)) return;

            cls.AddField(field);
        }

        private static bool MetaFieldIsNotConnected(Mediachase.MetaDataPlus.Configurator.MetaField field, Mediachase.MetaDataPlus.Configurator.MetaClass cls)
        {
            return cls != null && !cls.MetaFields.Contains(field);
        }

        #endregion

        #region Helper method: Add, Copy, Delete custom field

        private void CopyMetaFieldToMetaClass(string metaFieldName, string metaClassName)
        {
            var mdcontext = OrderContext.MetaDataContext;
            var metaClass = Mediachase.MetaDataPlus.Configurator.MetaClass.Load(mdcontext, metaClassName);
            if (metaClass == null) return;

            var metaField = Mediachase.MetaDataPlus.Configurator.MetaField.Load(mdcontext, metaFieldName);
            if (metaField == null) return;

            if (metaClass.MetaFields[metaFieldName] == null)
            {
                metaClass.AddField(metaField);
            }
        }

        private void DeleteMetaField(string metaClassName, string fieldName)
        {
            MetaClass metaClass = DataContext.Current.MetaModel.MetaClasses
                .Cast<MetaClass>()
                .FirstOrDefault(mc => mc.Name == metaClassName);

            metaClass?.DeleteMetaField(fieldName);
        }

        private void AddCustomField(string clsName, string fieldName, string metaFieldType, string defaultValue, bool? editable = null)
        {
            
            var customerMetadata = DataContext.Current.MetaModel.MetaClasses
              .Cast<MetaClass>()
              .First(mc => mc.Name == clsName);

            if (customerMetadata.Fields[fieldName] != null)
            {
                if (customerMetadata.Fields[fieldName].TypeName == metaFieldType)
                {
                    return;
                }

                customerMetadata.DeleteMetaField(customerMetadata.Fields[fieldName]);
            }

            var attributes = new AttributeCollection();
            if (metaFieldType == MetaFieldType.LongText) attributes.Add("LongText", "true");
            attributes.Add("Label", fieldName);
            attributes.Add("Description", string.IsNullOrWhiteSpace(defaultValue) ? fieldName : defaultValue);

            if (editable.HasValue && editable.Value)
            {
                attributes.Add("Editable", true);
            }

            customerMetadata.CreateMetaField(fieldName, fieldName, metaFieldType, true, defaultValue, attributes);
        }

        private void AddCustomLineItemField(string name, string displayName, int length, MetaDataType metaFieldType, bool allowNulls = false)
        {
            var lineItemMetaClass = OrderContext.Current.LineItemMetaClass;

            var mdcontext = OrderContext.MetaDataContext;
            var lineItemMetaField = lineItemMetaClass.MetaFields[name];

            if (lineItemMetaField != null)
            {
                if (lineItemMetaField.DataType == metaFieldType) return;
                lineItemMetaClass.DeleteField(lineItemMetaField);
                Mediachase.MetaDataPlus.Configurator.MetaField.Delete(mdcontext, lineItemMetaField.Id);
            }

            var metaNamespace = "Mediachase.Commerce.Orders.System.LineItem";
            var description = name + " on line item";

            var metaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(mdcontext, metaNamespace, name, displayName, description, metaFieldType, length, allowNulls, false, true, false);


            lineItemMetaClass.AddField(metaField);

        }

        private void AddCustomFieldForPurchaseOrder(string name, string displayName, int length, MetaDataType metaFieldType, bool allowNull = false)
        {
            var orderGroupMetaClass = OrderContext.Current.PurchaseOrderMetaClass;
            var mdcontext = OrderContext.MetaDataContext;
            var orderGroupMetaField = orderGroupMetaClass.MetaFields[name];

            if (orderGroupMetaField != null)
            {
                if (orderGroupMetaField.DataType == metaFieldType) return;
                orderGroupMetaClass.DeleteField(orderGroupMetaField);
                Mediachase.MetaDataPlus.Configurator.MetaField.Delete(mdcontext, orderGroupMetaField.Id);
            }

            var metaNamespace = "Mediachase.Commerce.Orders.System";
            var description = name + " on purchase order";

            var metaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(mdcontext, metaNamespace, name, displayName, description, metaFieldType, length, allowNull, false, true, false);

            orderGroupMetaClass.AddField(metaField);

        }

        private void DeleteCustomFieldForPurchaseOrder(string name)
        {
            var orderGroupMetaClass = OrderContext.Current.PurchaseOrderMetaClass;
            var mdcontext = OrderContext.MetaDataContext;
            var orderGroupMetaField = orderGroupMetaClass.MetaFields[name];

            if (orderGroupMetaField == null) return;

            orderGroupMetaClass.DeleteField(orderGroupMetaField);
            Mediachase.MetaDataPlus.Configurator.MetaField.Delete(mdcontext, orderGroupMetaField.Id);
        }

        private void AddCustomFieldForShoppingCart(string name, string displayName, int length, MetaDataType metaFieldType)
        {
            var orderGroupMetaClass = OrderContext.Current.ShoppingCartMetaClass;
            var mdcontext = OrderContext.MetaDataContext;
            var orderGroupMetaField = orderGroupMetaClass.MetaFields[name];

            if (orderGroupMetaField != null)
            {
                if (orderGroupMetaField.DataType == metaFieldType) return;
                orderGroupMetaClass.DeleteField(orderGroupMetaField);
                Mediachase.MetaDataPlus.Configurator.MetaField.Delete(mdcontext, orderGroupMetaField.Id);
            }

            var metaNamespace = "Mediachase.Commerce.Orders.System";
            var description = name + " on shopping cart";

            var metaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(mdcontext, metaNamespace, name, displayName, description, metaFieldType, length, true, false, true, false);

            orderGroupMetaClass.AddField(metaField);
        }

        #endregion
    }
}


