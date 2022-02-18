using System;
using System.Runtime.Serialization;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus.Configurator;
using TRM.Shared.Interfaces;

namespace EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays.Orders
{
    /// <summary>
    /// Represents Payment class for Mastercard.
    /// </summary>
    [Serializable]
    public class BarclaysCardPayment : Mediachase.Commerce.Orders.Payment, IAmTokenisablePayment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BarclaysCardPayment"/> class.
        /// </summary>
        public BarclaysCardPayment()
            : base(BarclaysCardPaymentMetaClass)
        {
            this.PaymentType = PaymentType.Other;
            ImplementationClass =
                this.GetType()
                    .AssemblyQualifiedName; // need to have assembly name in order to retreive the correct type in ClassInfo
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BarclaysCardPayment"/> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected BarclaysCardPayment(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.PaymentType = PaymentType.Other;
            ImplementationClass =
                this.GetType()
                    .AssemblyQualifiedName; // need to have assembly name in order to retreive the correct type in ClassInfo
        }

        private static MetaClass _MetaClass;

        public static MetaClass BarclaysCardPaymentMetaClass
        {
            get
            {
                if (_MetaClass == null)
                {
                    _MetaClass = MetaClass.Load(OrderContext.MetaDataContext, "BarclaysCardPayment");
                }

                return _MetaClass;
            }
        }

        public string MastercardOrderNumber
        {
            get { return GetString("MastercardOrderNumber"); }
            set { this["MastercardOrderNumber"] = value; }
        }

        public string MastercardSessionId
        {
            get { return GetString("MastercardSessionId"); }
            set { this["MastercardSessionId"] = value; }
        }

        public string MastercardNameOnCard
        {
            get { return GetString("MastercardNameOnCard"); }
            set { this["MastercardNameOnCard"] = value; }
        }

        public string MastercardToken
        {
            get { return GetString("MastercardToken"); }
            set { this["MastercardToken"] = value; }
        }

        public string MastercardAuthorizationCode
        {
            get { return GetString("MastercardAuthorizationCode"); }
            set { this["MastercardAuthorizationCode"] = value; }
        }

        public string MastercardCardType
        {
            get { return GetString("MastercardCardType"); }
            set { this["MastercardCardType"] = value; }
        }

        public string MastercardCardExpiry
        {
            get { return GetString("MastercardCardExpiry"); }
            set { this["MastercardCardExpiry"] = value; }
        }

        public string MastercardCardNumber
        {
            get { return GetString("MastercardCardNumber"); }
            set { this["MastercardCardNumber"] = value; }
        }

        public string Mastercard3DSStatus
        {
            get { return GetString("Mastercard3DSStatus"); }
            set { this["Mastercard3DSStatus"] = value; }
        }

        public string Mastercard3DSEci
        {
            get { return GetString("Mastercard3DSEci"); }
            set { this["Mastercard3DSEci"] = value; }
        }

        public string Mastercard3DSSid
        {
            get { return GetString("Mastercard3DSSid"); }
            set { this["Mastercard3DSSid"] = value; }
        }

        public string Mastercard3DSVavid
        {
            get { return GetString("Mastercard3DSVavid"); }
            set { this["Mastercard3DSVavid"] = value; }
        }

        public string Mastercard3DSVtSid
        {
            get { return GetString("Mastercard3DSVtSid"); }
            set { this["Mastercard3DSVtSid"] = value; }
        }

        public string TokenisedCardNumber
        {
            get { return GetString("TokenisedCardNumber"); }
            set { this["TokenisedCardNumber"] = value; }
        }


        public string SessionJsUrl
        {
            get { return GetString("MastercardSessionJsUrl"); }
            set { this["MastercardSessionJsUrl"] = value; }
        }

        public bool IsAmexPayment
        {
            get { return GetBool("IsAmexPayment"); }
            set { this["IsAmexPayment"] = value; }
        }

        public bool SaveCardDetails
        {
            get { return GetBool("SaveCardDetails"); }
            set { this["SaveCardDetails"] = value; }
        }

        public string Mastercard3DSecureId
        {
            get { return GetString("Mastercard3DSecureId"); }
            set { this["Mastercard3DSecureId"] = value; }
        }

        public string CaptureStatus
        {
            get { return GetString("CaptureStatus"); }
            set { this["CaptureStatus"] = value; }
        }

        public string CaptureTotalAuthorizedAmount
        {
            get { return GetString("CaptureTotalAuthorizedAmount"); }
            set { this["CaptureTotalAuthorizedAmount"] = value; }
        }

        public string CaptureTotalCapturedAmount
        {
            get { return GetString("CaptureTotalCapturedAmount"); }
            set { this["CaptureTotalCapturedAmount"] = value; }
        }

        public string CaptureAcquirerMessage
        {
            get { return GetString("CaptureAcquirerMessage"); }
            set { this["CaptureAcquirerMessage"] = value; }
        }

        public string CaptureAuthorizationCode
        {
            get { return GetString("CaptureAuthorizationCode"); }
            set { this["CaptureAuthorizationCode"] = value; }
        }

        public string CaptureTransactionReceipt
        {
            get { return GetString("CaptureTransactionReceipt"); }
            set { this["CaptureTransactionReceipt"] = value; }
        }

    }
}
