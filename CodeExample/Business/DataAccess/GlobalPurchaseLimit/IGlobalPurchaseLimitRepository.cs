using System;
using System.Collections.Generic;
using TRM.Web.Constants;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess.GlobalPurchaseLimit
{
    public interface IGlobalPurchaseLimitRepository
    {
        Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit AddOrUpdate(Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit globalPurchaseLimit);
        GlobalPurchaseLimitResult UpdateMetalPurchaseLimit(string metal, decimal amount, Enums.BullionTradeType bullionTradeType, DateTime checkDate);
        IEnumerable<Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit> Filter(Func<Models.EntityFramework.GlobalPurchaseLimits.GlobalPurchaseLimit, bool> where);
        bool PurchaseLimitExceeded(GlobalPurchaseLimitSetting pampMetal, out decimal remainingAmount);
        bool SignaturePurchaseLimitExceeded(GlobalPurchaseLimitSetting pampMetal, out decimal remainingAmount);
    }
}