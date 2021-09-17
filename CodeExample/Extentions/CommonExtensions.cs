﻿using System;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;

namespace TRM.Web.Extentions
{
    public static class CommonExtensions
    {
        public static bool IsNullOrEmpty(this Guid guid)
        {
            return (guid == Guid.Empty);
        }

        public static string GetOrdinalSuffix(this int num)
        {
            if (num <= 0) return string.Empty;

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return "th";
            }

            switch (num % 10)
            {
                case 1:
                    return "st";
                case 2:
                    return "nd";
                case 3:
                    return "rd";
                default:
                    return "th";
            }
        }

        public static string FormatPrice(this decimal value, string currencyCode)
        {
            return new Money(value, currencyCode).ToString();
        }
    }
}