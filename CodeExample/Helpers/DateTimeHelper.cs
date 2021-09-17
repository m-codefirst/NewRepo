using System;
using System.Collections.Generic;
using System.Linq;

namespace TRM.Web.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime KycNotificationDate => new DateTime(2020, 1, 1);

        public static IEnumerable<DateTime> GetDatesInRange(DateTime fromDate, DateTime toDate)
        {
            var result = Enumerable.Range(0, toDate.Subtract(fromDate).Days + 1)
                .Select(day => fromDate.AddDays(day));

            return result;
        }
    }
}