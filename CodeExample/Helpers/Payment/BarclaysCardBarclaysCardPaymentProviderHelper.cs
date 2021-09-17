using EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays;
using Mediachase.Commerce.Customers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Hephaestus.CMS.Extensions;
using Hephaestus.Commerce.Shared.Models;
using Mediachase.Commerce.Orders.Dto;
using TRM.Web.Extentions;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Models.ViewModels.Checkout;
using static TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Helpers.Payment
{
    public class BarclaysCardBarclaysCardPaymentProviderHelper : IBarclaysCardPaymentProviderHelper
    {
        private readonly CustomerContext _customerContext;
        private readonly IAmCartHelper _cartHelper;
        private readonly IAmAPaymentMethodHelper _paymentMethodHelper;

        public BarclaysCardBarclaysCardPaymentProviderHelper(CustomerContext customerContext, IAmCartHelper cartHelper,
            IAmAPaymentMethodHelper paymentMethodHelper)
        {
            _customerContext = customerContext;
            _cartHelper = cartHelper;
            _paymentMethodHelper = paymentMethodHelper;

        }

        public Dictionary<string, string> GenerateParameters(PaymentMethodDto.PaymentMethodRow payMethod,
            AddressModel billingAddress, AddressModel deliveryAddress)
        {
            var parameters = new Dictionary<string, string>();
            if (payMethod == null)
            {
                return parameters;
            }

            var paymentMethodParameters = payMethod.GetPaymentMethodParameterRows();
            var secureAcceptanceURL = paymentMethodParameters.FirstOrDefault(x => x.Parameter == BarclaysCardPaymentGateway.SAUrlParameter)
                ?.Value;
            var profileId = paymentMethodParameters.FirstOrDefault(x => x.Parameter == BarclaysCardPaymentGateway.ProfileID)?.Value;
            var accessKey = paymentMethodParameters.FirstOrDefault(x => x.Parameter == BarclaysCardPaymentGateway.AccessKey)?.Value;
            var secretKey = paymentMethodParameters.FirstOrDefault(x => x.Parameter == BarclaysCardPaymentGateway.SecretKey)?.Value;

            var mixedCart = _cartHelper.GetMixedLargeCart();

            parameters.Add(BarclaysPaymentProvider.RequestFields.ProfileId, @profileId);
            parameters.Add(BarclaysPaymentProvider.RequestFields.AccessKey, @accessKey);
            parameters.Add(BarclaysPaymentProvider.RequestFields.TransactionUuid, Security.GetUUID());
            parameters.Add(BarclaysPaymentProvider.RequestFields.SignedDateTime, Security.GetUTCDateTime());
            parameters.Add(BarclaysPaymentProvider.RequestFields.Locale, "en");
            parameters.Add(BarclaysPaymentProvider.RequestFields.TransactionType, BarclaysPaymentProvider.TrasactionTypes.Authorization);
            parameters.Add(BarclaysPaymentProvider.RequestFields.ReferenceNumber, Security.GetDateTimeAsReference());

            parameters.Add(BarclaysPaymentProvider.RequestFields.Amount, FormatDigitOnlyToString(mixedCart.FullBasketTotal));
            parameters.Add(BarclaysPaymentProvider.RequestFields.Currency, "GBP");
            parameters.Add(BarclaysPaymentProvider.RequestFields.PaymentMethod, BarclaysPaymentProvider.Card);

            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToForename,
                !string.IsNullOrEmpty(billingAddress.FirstName) ? billingAddress.FirstName : deliveryAddress.FirstName);
            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToSurname,
                !string.IsNullOrEmpty(billingAddress.LastName) ? billingAddress.LastName : deliveryAddress.LastName);
            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToEmail,
                !string.IsNullOrEmpty(billingAddress.Email) ? billingAddress.Email : deliveryAddress.Email);
            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToPhone,
                !string.IsNullOrEmpty(billingAddress.DaytimePhoneNumber)
                    ? billingAddress.DaytimePhoneNumber
                    : deliveryAddress.DaytimePhoneNumber);
            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToAddressLine1, billingAddress.Line1);
            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToAddressCity, billingAddress.City);
            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToAddressState, billingAddress.CountryRegion.Region);
            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToAddressCountry,
                billingAddress.CountryCode == "GBR" ? "GB" : billingAddress.CountryCode == "USA" ? "US" : billingAddress.CountryCode);

            parameters.Add(BarclaysPaymentProvider.RequestFields.BillToAddressPostalCode, billingAddress.PostalCode);
            parameters.Add(BarclaysPaymentProvider.RequestFields.OverrideCustomReceiptPage, GetReceiptURLForPayment());
            parameters.Add(BarclaysPaymentProvider.RequestFields.LineItemCount, CountLineItems(mixedCart.Shipments));
            var counterNumber = 0;

            IEnumerable<CartItemViewModel> cartItemViewModels = from shipment in mixedCart.Shipments
                                                                from item in shipment.CartItems
                                                                select item;
            
            foreach (var item in cartItemViewModels)
            {
                parameters.Add(string.Format(BarclaysPaymentProvider.RequestFields.ItemCode_FormatStringForNum, counterNumber), item.Code);
                parameters.Add(string.Format(BarclaysPaymentProvider.RequestFields.ItemUnitPrice_FormatStringForNum, counterNumber),
                    item.PlacedPriceDecimal.ToString("0.##"));
                parameters.Add(string.Format(BarclaysPaymentProvider.RequestFields.ItemName_FormatStringForNum, counterNumber),
                    item.DisplayName);
                parameters.Add(string.Format(BarclaysPaymentProvider.RequestFields.ItemSKU_FormatStringForNum, counterNumber), item.Code);
                parameters.Add(string.Format(BarclaysPaymentProvider.RequestFields.ItemQuantity_FormatStringForNum, counterNumber),
                    item.Quantity.ToString("0"));
                counterNumber++;
            }

            parameters.Add(BarclaysPaymentProvider.RequestFields.SignedFieldNames,
                string.Format(string.Join(",", parameters.Select(x => x.Key)) + ",{0}",
                    BarclaysPaymentProvider.RequestFields.SignedFieldNames));
            parameters.Add(BarclaysPaymentProvider.RequestFields.Signature, Security.Sign(parameters, secretKey));


            return parameters;
        }

        private string FormatDigitOnlyToString(string mixedString)
        {
            if (string.IsNullOrWhiteSpace(mixedString)) return mixedString;
            return Regex.Replace(mixedString, @"[^0-9.,]", string.Empty);
        }

        private string CountLineItems(List<ShipmentViewModel> shipments)
        {
            var numberOfItems = 0;
            foreach (var shipment in shipments)
            {
                numberOfItems += shipment.CartItems.Count();
            }

            return numberOfItems.ToString();
        }

        private string GetReceiptURLForPayment()
        {
            var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
            var url = startPage.BarclaysRedirectionPage.ContentExternalUrl(CultureInfo.CurrentCulture, true);
            return url;
        }
    }
}