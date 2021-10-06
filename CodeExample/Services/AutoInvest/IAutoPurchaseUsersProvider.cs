using System;
using System.Collections.Generic;
using TRM.Web.Models.EntityFramework.CustomerContactContext;

namespace TRM.Web.Services.AutoInvest
{
    public interface IAutoPurchaseUsersProvider
    {
        IEnumerable<cls_Contact> GetUsers(DateTime fromDate, DateTime toDate);
    }
}