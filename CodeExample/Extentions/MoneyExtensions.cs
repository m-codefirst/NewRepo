using System;
using Mediachase.Commerce;

namespace TRM.Web.Extentions
{
    public static class MoneyExtensions
    {
        public static string ToCurrencyString(this Money value)
        {
            var defaultFormatted = value.ToString();

            if (value.Amount >= 10 || value.Amount <= -10)
            {
                return defaultFormatted;
            }

            string result = InsertLeadingZero(value, defaultFormatted);

            return result;
        }

        private static string InsertLeadingZero(Money value, string defaultFormatted)
        {
            var separator = value.Currency.Format.CurrencyDecimalSeparator;
            var parts = defaultFormatted.Split(new[] {separator}, StringSplitOptions.None);

            var firstPart = parts[0];
            firstPart = firstPart.Insert(firstPart.Length - 1, "0");

            parts[0] = firstPart;
            var result = string.Join(separator, parts);
            return result;
        }
    }
}