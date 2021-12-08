using System;
using System.ComponentModel;

namespace TRM.Shared.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescriptionAttribute(this Enum value)
        {
            if (value == null)
            {
                return null;
            }

            var field = value.GetType().GetField(value.ToString());
            if (field == null)
            {
                return string.Empty;
            }

            var attributes = (DescriptionAttribute[])field
                .GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }
}