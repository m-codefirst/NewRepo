using System;

namespace TRM.Web.Extentions
{
    public static class NumberExtensions
    {
        public static decimal RoundDown(this decimal number, int totalDecimalPlaces)
        {
            if (totalDecimalPlaces <= 0) return Math.Floor(number);
            var power = (decimal)Math.Pow(10, totalDecimalPlaces);
            return Math.Floor(number * power) / power;
        }
    }
}