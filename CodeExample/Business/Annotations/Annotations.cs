using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace TRM.Web.Business.Annotations
{
    public class MustBeTrue : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return value is bool && (bool) value;
        }
    }

    [System.Web.Mvc.Bind(Include = "Day,Month,Year")]
    public class Date
    {
        public Date() : this(System.DateTime.MinValue) { }
        public Date(DateTime date)
        {
            Year = date.Year;
            Month = date.Month;
            Day = date.Day;
        }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        //[Required]
        public DateTime? DateTime
        {
            get
            {
                DateTime date;
                if (!System.DateTime.TryParseExact(
                    string.Format("{0}/{1}/{2}", Year, Month, Day),
                    "yyyy/M/d",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out date))
                    return null;
                else
                    return date;
            }
        }
    }

    public class MustBeDate : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            bool result = false;
            if (value == null)
                return result;

            Date toValidate = value as Date;

            if (toValidate == null)
                throw new ArgumentException("value is an invalid or is an unexpected type");

            //DateTime returns null when date cannot be constructed
            if (toValidate.DateTime != null)
            {
                result = (toValidate.DateTime != System.DateTime.MinValue) && (toValidate.DateTime != System.DateTime.MaxValue);
            }

            return result;
        }
    }

    public class MustBe18 : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            bool result = false;
            if (value == null)
                return result;

            Date toValidate = value as Date;

            if (toValidate == null)
                throw new ArgumentException("value is an invalid or is an unexpected type");

            if (toValidate.DateTime != null)
            {
                result = (DateTime.Now.Year - toValidate.DateTime?.Year) >= 18;
            }

            return result;
        }
    }
}