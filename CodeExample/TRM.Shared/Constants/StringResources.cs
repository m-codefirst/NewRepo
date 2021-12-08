namespace TRM.Shared.Constants
{
    public class StringResources
    {
        public const string AddressIncomplete = "/nvc/validation/addressincomplete";

        #region Credit Payment Message

        public const string PaymentErrorsCreditPaymentPurchaseOrderExists = "/nvc/paymenterrors/creditpayment/purchaseorderexists";
        public const string PaymentErrorsCreditPaymentCardNotFound = "/nvc/paymenterrors/creditpayment/cartnotfound";
        public const string PaymentErrorsCreditPaymentInsufficientFunds = "/nvc/paymenterrors/creditpayment/insufficientfunds";
        public const string PaymentErrorsCreditPaymentError = "/nvc/paymenterrors/creditpayment/error";

        #endregion

        #region Mastercard
        public const string MastercardAddFundsManually = "/nvc/payment/mastercard/addfundsmanually";
        public const string MastercardAddFundsManuallyDescription = "/nvc/payment/mastercard/addfundsmanuallydescription";
        public const string MastercardChooseACard = "/nvc/payment/mastercard/chooseacard";
        public const string MastercardNewCard = "/nvc/payment/mastercard/newcard";
        public const string MastercardNewAmexCard = "/nvc/payment/mastercard/newamexcard";
        public const string MastercardNewCardUsa = "/nvc/payment/mastercard/newcardusa";
        public const string MastercardNewAmexCardUsa = "/nvc/payment/mastercard/newamexcardusa";
        public const string MastercardCardNumber = "/nvc/payment/mastercard/cardnumber";
        public const string MastercardMandatoryFieldAsterix = "/nvc/payment/mastercard/mandatoryfieldasterix";
        public const string MastercardCardNumberPlaceholder = "/nvc/payment/mastercard/cardnumberplaceholder";
        public const string MastercardEndDate = "/nvc/payment/mastercard/enddate";
        public const string MastercardMonthPlaceholder = "/nvc/payment/mastercard/monthplaceholder";
        public const string MastercardYearPlaceholder = "/nvc/payment/mastercard/yearplaceholder";
        public const string MastercardNameOnCard = "/nvc/payment/mastercard/nameoncard";
        public const string MastercardNameOnCardSuffix = "/nvc/payment/mastercard/nameoncardsuffix";
        public const string MastercardNameOnCardPlaceholder = "/nvc/payment/mastercard/nameoncardplaceholder";
        public const string MastercardCcvNumber = "/nvc/payment/mastercard/ccvnumber";
        public const string MastercardCcvPlaceHolder = "/nvc/payment/mastercard/ccvplaceholder";
        public const string MastercardCcvHelpMessage = "/nvc/payment/mastercard/ccvhelpmessage";
        public const string MastercardCcvHelpMessageAmex = "/nvc/payment/mastercard/ccvhelpmessageamex";
        public const string MastercardSaveCard = "/nvc/payment/mastercard/savecard";
        public const string MastercardRemoveCard = "/nvc/payment/mastercard/removecard";
        public const string MastercardEnding = "/nvc/payment/mastercard/ending";
        public const string PaymentErrorsMastercardError = "/nvc/paymenterrors/mastercard/{0}";
        public const string InvalidAmountOfFunds = "/nvc/payment/mastercard/invalidamount";
        public const string CreditCardErrorMessage = "/nvc/payment/mastercard/creditcarderrormessage";
        public const string MastercardExpiryDate  = "/nvc/payment/mastercard/expirydate ";
        #endregion

        public const string PaymentError = "/nvc/paymenterrors/paymenterror";

        #region Wallet Payment
        public const string PaymentErrorsWalletPaymentCardNotFound = "/nvc/paymenterrors/walletpayment/cartnotfound";
        public const string PaymentErrorsWalletPaymentInsufficientFunds = "/nvc/paymenterrors/walletpayment/insufficientfunds";
        public const string PaymentErrorsWalletPaymentError = "/nvc/paymenterrors/walletpayment/error";
        #endregion

        #region AddFund Errors

        public const string InvalidSession = "/nvc/bullion/addfunds/error/invalidsession";
        public const string DisallowsAmountOfFunds = "/nvc/bullion/addfunds/error/DisallowsAmountOfFunds";

        #endregion

        public const string RecaptchaRequiredError = "/nvc/recaptcha/requirederror";
    }
}
