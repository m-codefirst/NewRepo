using System.Collections.Generic;
using Mediachase.Commerce.Customers;

namespace TRM.Web.Services.AutoInvest
{
    public interface IAutoInvestProductsService
    {
        List<AutoInvestProductDto> GetProducts(CustomerContact contact);

        List<int> GetAvailableInvestmentDates();
    }
}