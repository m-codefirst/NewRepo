using EPiServer.Core;
using Mediachase.Commerce.Orders.Dto;
using System;
using System.Linq;

namespace EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays
{
    internal static class Utilities
    {
        public static string GenerateOrderIdFromTransactionId(string transactionId)
        {
            var defaultValue = DateTime.UtcNow.ToString("yyMMddhhmmss");
            var startPageData = DataFactory.Instance.GetPage(ContentReference.StartPage);
            if (startPageData == null)
            {
                return defaultValue;
            }

            var checkoutPage = startPageData.Property["CheckoutPage"];
            if (checkoutPage != null && !checkoutPage.IsNull)
            {
                var checkoutPageReference = checkoutPage.Value as PageReference;
                if (checkoutPageReference != null)
                {
                    var checkoutPageData = DataFactory.Instance.GetPage(checkoutPageReference);
                    if (checkoutPageData == null)
                    {
                        return defaultValue;
                    }
                    var prefixSetting = checkoutPageData.Property["OrderNumberPrefix"];
                    var prefix = string.Empty;
                    if (prefixSetting != null && !prefixSetting.IsNull)
                    {
                        prefix = prefixSetting.ToString();
                    }

                    string orderId = transactionId.Replace(prefix, string.Empty);

                    long test;
                    if (long.TryParse(orderId, out test))
                    {
                        defaultValue = string.Format("{0}{1}", orderId, DateTime.Now.ToString("hhmmssfff"));
                    }
                }
            }

            if (defaultValue.Length > 16)
            {
                defaultValue = defaultValue.Substring(0, 16);
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets friendly url of the page.
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        /// <returns>The friendly url of page if UrlRewriteProvider.IsFurlEnabled</returns>
     
        /// <summary>
        /// Gets the Mastercard PaymentMethodDto's parameter (setting in CommerceManager of Mastercard) by name.
        /// </summary>
        /// <param name="paymentMethodDto">The payment method dto</param>
        /// <param name="name">Name of parameter</param>
        /// <returns>null if not found</returns>
        public static PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(this PaymentMethodDto paymentMethodDto, string name)
        {
            var rowArray = (PaymentMethodDto.PaymentMethodParameterRow[])paymentMethodDto.PaymentMethodParameter.Select(string.Format("Parameter = '{0}'", name));
            if (rowArray.Any())
            {
                return rowArray[0];
            }

            return null;
        }
    }
}
