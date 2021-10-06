using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Web.Constants;
using TRM.Web.Helpers;
using TRM.Web.Models.EntityFramework.CustomerContactContext;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoPurchaseUsersProvider : IAutoPurchaseUsersProvider
    {
        private readonly ICustomerContactDbContextFactory _customerContactDbContextFactory;

        public AutoPurchaseUsersProvider(ICustomerContactDbContextFactory customerContactDbContextFactory)
        {
            _customerContactDbContextFactory = customerContactDbContextFactory;
        }

        public IEnumerable<cls_Contact> GetUsers(DateTime fromDate, DateTime toDate)
        {
            var currentMonth = new DateTime(toDate.Year, toDate.Month, 1, 0, 0, 0);
            var datesInRange = DateTimeHelper.GetDatesInRange(fromDate, toDate).ToList();
            var investDays = datesInRange.Select(x => x.Day).ToList();

            using (var context = _customerContactDbContextFactory.CreateDbContext())
            {
                var result = context.cls_Contact
                    .Where(contact => !string.IsNullOrEmpty(contact.CustomerBullionObsAccountNumber))
                    .Where(contact => contact.IsAutoInvest.HasValue && contact.IsAutoInvest.Value)
                    .Where(contact => contact.LtAutoInvestDay.HasValue && investDays.Contains(contact.LtAutoInvestDay.Value))
                    .Where(contact => contact.LastAutoInvestDate == null || contact.LastAutoInvestDate < currentMonth)
                    .Where(contact => contact.LastAutoInvestDate == null || contact.LastAutoInvestDate < fromDate ||
                                      contact.LastAutoInvestStatus == (int) Enums.AutoInvestUpdateOrderStatus.Error)
                    .ToList();

                //result = FilterOutNewUsers(datesInRange, result);

                return result;
            }
        }


        ////In case of disallowed dates following scenario is possible. 
        //// on 2020.11.1 user creates application and set day to 30
        //// 29, 30 and 1 were disallowed dates 
        //// on 2020.11.2 we run the job and it is processing users from 29,30,1,2 - this user should not be processed because he created the application in November.
        //private static List<cls_Contact> FilterOutNewUsers(List<DateTime> datesInRange, List<cls_Contact> result)
        //{
        //    var datesDictionary = datesInRange
        //        .Select(x => new KeyValuePair<int, DateTime>(x.Day, x))
        //        .OrderByDescending(x => x.Value)
        //        .DistinctBy(x => x.Key)
        //        .ToDictionary(x => x.Key, x => x.Value);

        //    result = result.Where(contact =>
        //        contact.LtAutoInvestDay.HasValue && contact.LtApplicationDate.HasValue &&
        //        contact.LtApplicationDate.Value.Date <= datesDictionary[contact.LtAutoInvestDay.Value]).ToList();
        //    return result;
        //}
    }
}