using System;

namespace TRM.Web.Extentions
{
    public static class DateTimeExt
    {
        public static string ToUnixTimeString(this DateTime date)
        {
            var nx = new DateTime(1970, 1, 1, 0, 0, 0, 0); // UNIX epoch date
            var ts = date - nx; // UtcNow, because timestamp is in GMT
            var d = ((int)ts.TotalSeconds).ToString();
            return d;
        }

        /// <summary>
        /// Common date format in UK, ex 1st January 2018
        /// </summary>
        public static string ToCommonUKFormat(this DateTime date)
        {
            return string.Format(date.ToString("d{0} MMMM yyyy"), date.Day.GetOrdinalSuffix());
        }
    }
}