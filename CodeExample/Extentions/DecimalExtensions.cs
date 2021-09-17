using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Mediachase.Commerce;

namespace TRM.Web.Extentions
{
    public static class DecimalExtensions
    {
        public static string ToCurrency(this decimal value, string currencyCode)
        {
            var customerCurrency = new Mediachase.Commerce.Currency(currencyCode);
            return new Money(value, customerCurrency).ToString();
        }
    }
}