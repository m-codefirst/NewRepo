using System.Collections.Generic;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Services.AutoInvest
{
    public interface IAutoInvestPurchaseService
    {
        AutoInvestUpdateOrderResponse UpdateOrder(string bullionObsAccountNumber, Dictionary<string, decimal> products);
    }
}