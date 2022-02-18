using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Web;
using com.TNSi.sdk.profiles;
using com.TNSi.sdk.services;
using com.TNSi.soap.api;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Catalog.Objects;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Website;
using Mediachase.Commerce.Website.Helpers;

namespace EPiServer.Business.Commerce.Payment.TNSi
{
    internal static class Utilities
    {
        private const string CurrentCartKey = "CurrentCart";
        private const string CurrentContextKey = "CurrentContext";
        public const string TNSiCookieKey = "Sample_TNSi_values";

        /// <summary>
        /// Get display name with current language
        /// </summary>
        /// <param name="item">The line item of oder</param>
        /// <param name="maxSize">The number of character to get display name</param>
        /// <returns>Display name with current language</returns>
        public static string GetDisplayNameOfCurrentLanguage(this LineItem item, int maxSize)
        {
            Entry entry = CatalogContext.Current.GetCatalogEntry(item.CatalogEntryId, new CatalogEntryResponseGroup(CatalogEntryResponseGroup.ResponseGroup.CatalogEntryFull));
            // if the entry is null (product is deleted), return item display name
            var displayName = (entry != null) ? StoreHelper.GetEntryDisplayName(entry).StripPreviewText(maxSize <= 0 ? 100 : maxSize) : item.DisplayName.StripPreviewText(maxSize <= 0 ? 100 : maxSize);

            return displayName;
        }

        /// <summary>
        /// Update display name with current language
        /// </summary>
        /// <param name="po">The purchase order</param>
        public static void UpdateDisplayNameWithCurrentLanguage(this PurchaseOrder po)
        {
            if (po != null)
            {
                if (po.OrderForms != null && po.OrderForms.Count > 0)
                {
                    foreach (OrderForm orderForm in po.OrderForms)
                    {
                        if (orderForm.LineItems != null && orderForm.LineItems.Count > 0)
                        {
                            foreach (LineItem item in orderForm.LineItems)
                            {
                                item.DisplayName = item.GetDisplayNameOfCurrentLanguage(100);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Encodes the cookie value.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string EncodeCookieValue(string type, string value)
        {
            if (value != null)
            {
                return value.Length.ToString(CultureInfo.InvariantCulture) + "|" + type + "|" + value;
            }
            return string.Empty;
        }
        
        /// <summary>
        /// Uses parameterized thread to update the cart instance id otherwise will get an "workflow already existed" exception.
        /// Passes the cart and the current HttpContext as parameter in call back function to be able to update the instance id and also can update the HttpContext.Current if needed.
        /// </summary>
        /// <param name="cart">The cart to update.</param>
        /// <remarks>
        /// This method is used internal for payment methods which has redirect standard for processing payment e.g: TNSi, DIBS
        /// </remarks>
        internal static void UpdateCartInstanceId(Cart cart)
        {
            ParameterizedThreadStart threadStart = UpdateCartCallbackFunction;
            var thread = new Thread(threadStart);
            var cartInfo = new Hashtable();
            cartInfo[CurrentCartKey] = cart;
            cartInfo[CurrentContextKey] = HttpContext.Current;
            thread.Start(cartInfo);
            thread.Join();
        }

        /// <summary>
        /// Callback function for updating the cart. Before accept all changes of the cart, update the HttpContext.Current if it is null somehow.
        /// </summary>
        /// <param name="cartArgs">The cart agruments for updating.</param>
        private static void UpdateCartCallbackFunction(object cartArgs)
        {
            var cartInfo = cartArgs as Hashtable;
            if (cartInfo == null || !cartInfo.ContainsKey(CurrentCartKey))
            {
                return;
            }

            var cart = cartInfo[CurrentCartKey] as Cart;
            if (cart != null)
            {
                cart.InstanceId = Guid.NewGuid();
                if (HttpContext.Current == null && cartInfo.ContainsKey(CurrentContextKey))
                {
                    HttpContext.Current = cartInfo[CurrentContextKey] as HttpContext;
                }
                try
                {
                    cart.AcceptChanges();
                }
                catch (System.Exception ex)
                {
                    ErrorManager.GenerateError(ex.Message);
                }
            }
        }

        public static string GetUrlValueFromStartPage(string propertyName)
        {
            var startPageData = DataFactory.Instance.GetPage(PageReference.StartPage);
            if (startPageData == null)
            {
                return PageReference.StartPage.GetFriendlyUrl();
            }

            string result = string.Empty;
            var property = startPageData.Property[propertyName];
            if (property != null && !property.IsNull)
            {
                if (property.PropertyValueType == typeof(PageReference))
                {
                    var propertyValue = property.Value as PageReference;
                    if (propertyValue != null)
                    {
                        result = propertyValue.GetFriendlyUrl();
                    }
                }
            }
            return string.IsNullOrEmpty(result) ? PageReference.StartPage.GetFriendlyUrl() : result;
        }

        /// <summary>
        /// Gets friendly url of the page.
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        /// <returns>The friendly url of page if UrlRewriteProvider.IsFurlEnabled</returns>
        public static string GetFriendlyUrl(this PageReference pageReference)
        {
            if (pageReference == null)
            {
                return string.Empty;
            }

            var page = DataFactory.Instance.GetPage(pageReference);

            if (UrlRewriteProvider.IsFurlEnabled)
            {
                var urlBuilder = new UrlBuilder(new Uri(page.LinkURL, UriKind.RelativeOrAbsolute));
                Global.UrlRewriteProvider.ConvertToExternal(urlBuilder, page.PageLink, System.Text.Encoding.UTF8);
                return urlBuilder.ToString();
            }
            else
            {
                return page.LinkURL;
            }
        }


        /// <summary>
        /// Gets the TNSi PaymentMethodDto's parameter (setting in CommerceManager of TNSi) value by name.
        /// </summary>
        /// <param name="paymentMethodDto">The payment method dto</param>
        /// <param name="name">Name of parameter</param>
        /// <returns>string.Empty if not found</returns>
        public static string GetParameterValueByName(this PaymentMethodDto paymentMethodDto, string name)
        {   
            var paramRow = paymentMethodDto.GetParameterByName(name);
            if (paramRow != null)
            {
                return paramRow.Value;
            }

            return string.Empty;
        }
        
        /// <summary>
        /// Gets the TNSi PaymentMethodDto's parameter (setting in CommerceManager of TNSi) by name.
        /// </summary>
        /// <param name="paymentMethodDto">The payment method dto</param>
        /// <param name="name">Name of parameter</param>
        /// <returns>null if not found</returns>
        public static PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(this PaymentMethodDto paymentMethodDto, string name)
        {
            var rowArray = (PaymentMethodDto.PaymentMethodParameterRow[])paymentMethodDto.PaymentMethodParameter.Select(string.Format("Parameter = '{0}'", name));
            if ((rowArray != null) && (rowArray.Length > 0))
            {
                return rowArray[0];
            }

            return null;
        }

        /// <summary>
        /// translate with languageKey under /Commerce/TNSiPayment/ in lang.xml
        /// </summary>
        /// <param name="languageKey"></param>
        /// <returns></returns>
        public static string Translate(this string languageKey)
        {
            return LocalizationService.GetString("/Commerce/TNSiPayment/" + languageKey);
        }

        /// <summary>
        /// Setup the TNSi API caller service, use the profile setting with pre-configured parameters
        /// </summary>
        /// <returns>null if any of APIUsername, APIPassword, APISignature, Environment is missing</returns>
        public static CallerServices GetTNSiAPICallerServices()
        {
            var TNSi = GetTNSiPaymentMethod();
            
            IAPIProfile profile = ProfileFactory.createSignatureAPIProfile();
            profile.APIUsername = TNSi.GetParameterValueByName(TNSiPaymentGateway.UserParameter);
            profile.APIPassword = TNSi.GetParameterValueByName(TNSiPaymentGateway.PasswordParameter);
            profile.APISignature = TNSi.GetParameterValueByName(TNSiPaymentGateway.APISisnatureParameter);
            profile.Environment = (TNSi.GetParameterValueByName(TNSiPaymentGateway.SandBoxParameter) == "1" ? "sandbox" : "live");
            
            if (   string.IsNullOrEmpty(profile.APIUsername)
                || string.IsNullOrEmpty(profile.APIPassword)
                || string.IsNullOrEmpty(profile.APISignature)
                || string.IsNullOrEmpty(profile.Environment)
                )
            {   
                return null;
            }

            CallerServices caller = new CallerServices();
            caller.APIProfile = profile;

            return caller;
        }

        /// <summary>
        /// Return the PaymentMethodDto of TNSi
        /// </summary>
        /// <returns>Return TNSi payment method</returns>
        public static PaymentMethodDto GetTNSiPaymentMethod()
        {
            PaymentMethodDto oTNSi = PaymentManager.GetPaymentMethodBySystemName("TNSi", SiteContext.Current.LanguageName);
            return oTNSi;
        }

        /// <summary>
        /// Convert value to TNSi amount type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="currencyId"></param>
        /// <returns>a basic amount type of TNSi, with 2 decimal digits value</returns>
        public static BasicAmountType ToTNSiAmount(this decimal value, CurrencyCodeType currencyId)
        {
            return new BasicAmountType() { Value = value.ToString("0.00", CultureInfo.InvariantCulture), currencyID = currencyId };
        }

        /// <summary>
        /// Check the TNSi API response for errors.
        /// </summary>
        /// <param name="abstractResponse">the TNSi API response</param>
        /// <returns>if abstractResponse.Ack is not Success or SuccessWithWarning, return the error message list(s). If everything OK, return string.empty</returns>
        public static string CheckErrors(this AbstractResponseType abstractResponse)
        {
            string errorList = string.Empty;

            // First, check the Obvious.  Make sure Ack is not Success
            if (abstractResponse.Ack != AckCodeType.Success && abstractResponse.Ack != AckCodeType.SuccessWithWarning)
            {
                errorList = string.Format("TNSi API {0}.{1}: [{2}] CorrelationID={3}.\n", 
                    abstractResponse.Version, abstractResponse.Build, 
                    abstractResponse.Ack.ToString(),
                    abstractResponse.CorrelationID 
                    // The value returned in CorrelationID is important for TNSi to determine the precise cause of any error you might encounter. 
                    // If you have to troubleshoot a problem with your requests, capture the value of CorrelationID so you can report it to TNSi.
                    );

                if (abstractResponse.Errors.Length > 0)
                {   
                    foreach (ErrorType error in abstractResponse.Errors)
                    {
                        errorList += string.Format("\n[{0}-{1}]: {2}.", error.SeverityCode.ToString(), error.ErrorCode, error.LongMessage);
                    }
                }
                else
                {
                    errorList += "Unknown error while calling TNSi API.";   // TODO: localize
                }
            }
            
            return errorList;
        }

        private static LocalizationService LocalizationService
        {
            get { return ServiceLocator.Current.GetInstance<LocalizationService>(); }
        }
        
        /// <summary>
        /// Strips a text to a given length without splitting the last word.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="maxLength">Max length of the text</param>
        /// <returns>A shortened version of the given string</returns>
        /// <remarks>Will return empty string if input is null or empty</remarks>
        public static string StripPreviewText(this string source, int maxLength)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            if (source.Length <= maxLength)
            {
                return source;
            }
            source = source.Substring(0, maxLength);
            // The maximum number of characters to cut from the end of the string.
            var maxCharCut = (source.Length > 15 ? 15 : source.Length - 1);
            var previousWord = source.LastIndexOfAny(new char[] { ' ', '.', ',', '!', '?' }, source.Length - 1, maxCharCut);
            if (previousWord >= 0)
            {
                source = source.Substring(0, previousWord);
            }
            return source + " ...";
        }
    }
}
