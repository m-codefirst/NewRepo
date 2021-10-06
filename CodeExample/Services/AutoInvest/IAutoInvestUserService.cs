using System.Collections.Generic;
using Mediachase.Commerce.Customers;

namespace TRM.Web.Services.AutoInvest
{
    public interface IAutoInvestUserService
    {
        CustomerContact SaveInvestmentOptions(CustomerContact contact, Dictionary<string, decimal> investments,
            int autoInvestDay);        
        Dictionary<string, decimal> GetInvestmentOptions(CustomerContact contact);
        void UpdateLastAutoInvestStatus(string contactUserId, Constants.Enums.AutoInvestUpdateOrderStatus status);
        CustomerContact StopAutoInvest(CustomerContact contact);
        int GetInvestmentDay(CustomerContact contact);
        bool IsAutoInvestActive(CustomerContact contact);
    }
}