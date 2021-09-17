using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Mediachase.MetaDataPlus.Extensions;

namespace TRM.Web.Extentions
{
    public static class StringExtensions
    {
        public static T CastTo<T>(this object inputValue, T defaultValue)
        {
            if (inputValue == null) return defaultValue;
            return (T)Convert.ChangeType(inputValue, typeof(T));
        }

        private static readonly CultureInfo Culture = new CultureInfo("en-GB");
        private const string LetterPattern = @"^[A-Z]*$";
        public static string ToTitleCase(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return string.Empty;
            }

            var ti = Culture.TextInfo;
            return ti.ToTitleCase(str);
        }

        public static string ToLetterStandard(this string str)
        {
            var sb = new StringBuilder(str.Length);
            foreach (var letter in str.ToUpper())
            {
                if (Regex.IsMatch(letter.ToString(), LetterPattern, RegexOptions.IgnoreCase))
                {
                    sb.Append(letter);
                }
            }

            return sb.Length > 0 ? sb.ToString() : string.Empty;
        }

        public static string ToCssClassName(this string str)
        {
            str = str.Replace(" ", string.Empty);
            str = Regex.Replace(str, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", "-$0");
            return str.ToLowerInvariant();
        }

        public static bool IsLocalUrl(this string url, HttpRequestBase request)
        {
            Uri absoluteUri;
            return Uri.TryCreate(url, UriKind.Absolute, out absoluteUri) && String.Equals(request.Url.Host, absoluteUri.Host, StringComparison.OrdinalIgnoreCase);
        }

        public static T FromString<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static string EncodeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            var newValue = HttpUtility.UrlDecode(value);
            newValue = newValue.Replace(" ", "_");
            return HttpUtility.UrlEncode(newValue);
        }

        public static string DecodeValue(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            var newValue = value.Replace("_", " ");
            return HttpUtility.UrlDecode(newValue);
        }

        public static string EncodeTerm(this string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return string.Empty;
            }

            var rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(term.Replace(" ", string.Empty), "_");
        }

        public static string RemoveNonAzCharacters(this string value)
        {
            const string pattern = @"[^A-Za-z0-9]";
            var newValue = Regex.Replace(value, pattern, string.Empty).Trim();
            if (newValue.Length > 18)
            {
                newValue = newValue.Substring(0, 18);
            }
            return newValue.Trim();
        }

        public static IEnumerable<T> SplitValueTo<T>(this string stringValue, string separator = ",") where T : class, IConvertible
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }

            var parts = stringValue.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (parts == null || !parts.Any()) return null;

            return (IEnumerable<T>)parts.Select(x => (T)Convert.ChangeType(x, typeof(T))).GetEnumerator();
        }

        public static IEnumerable<decimal> SplitValueToDecimal(this string stringValue, string separator = ",")
        {
            var result = new List<decimal>();
            if (string.IsNullOrEmpty(stringValue)) return result;

            var numberStrings = stringValue.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            decimal number = 0;
            result.AddRange(from numberStr in numberStrings where decimal.TryParse(numberStr, out number) select number);

            return result;
        }

        public static string ToCurrencySymbol(this string ISOCurrency)
        {
            var region = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Select(x => new RegionInfo(x.LCID)).FirstOrDefault(p => p.ISOCurrencySymbol == ISOCurrency);
            return region?.CurrencySymbol ?? ISOCurrency;
        }

        public static decimal ToDecimal(this string strValue)
        {
            decimal value;
            return decimal.TryParse(strValue, out value) ? value : decimal.Zero;
        }

        public static bool TryParseDatetimeExact(this string strValue, string dateFormat, out DateTime outputDateTime)
        {
            return DateTime.TryParseExact(strValue,
                dateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out outputDateTime);
        }

        public static bool TryParseSqlDatetimeExact(this string strValue, out DateTime outputDateTime)
        {
            const string axDatetimeFormat = "yyyy-MM-ddTHH:mm:ss";
            var success = strValue.TryParseDatetimeExact(axDatetimeFormat, out outputDateTime);
            if (success) return true;

            const string sqlDateFormat = "yyyy-MM-dd";
            success = strValue.TryParseDatetimeExact(sqlDateFormat, out outputDateTime);
            if (success) return true;

            const string sqlDatetimeFormat = "yyyy-MM-dd HH:mm:ss";
            return strValue.TryParseDatetimeExact(sqlDatetimeFormat, out outputDateTime);
        }

        /// <summary>
        /// Parse string has format "yyyy-MM-dd HH:mm:ss" or "yyyy-MM-dd" to datetime.
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns>Return the Datetime.Now if the string has wrong format</returns>
        public static DateTime ToSqlDatetime(this string strValue)
        {
            DateTime outputDateTime;
            if (!strValue.TryParseSqlDatetimeExact(out outputDateTime))
            {
                outputDateTime = DateTime.Now;
            }

            return outputDateTime;
        }

        public static decimal ToDecimalExactCulture(this string strValue)
        {
            decimal value;

            // TryParseDecimalExactCulture contains buggy EPiServer code
            return strValue != null && strValue.TryParseDecimalExactCulture(CultureInfo.CurrentCulture, out value) ? value : decimal.Zero;
        }

        public static bool YesNoToBool(this string strValue)
        {
            if (strValue == null) return false;
            if (strValue.Equals("True", StringComparison.OrdinalIgnoreCase)) return true;
            if (strValue.Equals("Yes", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string GetOrderNumberFromEpiTransId(this string epiTransId)
        {
            if (string.IsNullOrEmpty(epiTransId)) return string.Empty;
            var firstMinusIndex = epiTransId.IndexOf('-');
            var lastMinusIndex = epiTransId.LastIndexOf('-');

            return lastMinusIndex < firstMinusIndex + 1 ? string.Empty : epiTransId.Substring(firstMinusIndex + 1, lastMinusIndex - firstMinusIndex - 1);
        }
        public static string ToSafe(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return value;
        }

        public static string StripHtmlTagsAndSpecialChars(this string text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            var result = Regex.Replace(text, @"\t|\n|\r", "");
            result = Regex.Replace(result, "<.*?>", string.Empty);
            result = Regex.Replace(result, "&nbsp;", " ");

            return result;
        }
    }
}