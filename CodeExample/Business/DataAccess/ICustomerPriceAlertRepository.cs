using System;
using System.Collections.Generic;
using TRM.Web.Constants;
using TRM.Web.Models.EntityFramework.CustomerPriceAlert;

namespace TRM.Web.Business.DataAccess
{
    public interface ICustomerPriceAlertRepository
    {
        IEnumerable<CustomerPriceAlert> GetCustomerPriceAlertWillBeMail(
            Dictionary<string, decimal> indicativeGoldPrices);
        IEnumerable<CustomerPriceAlert> GetCustomerAlertsByCustomer(Guid customerId);
        CustomerPriceAlert GetCustomerPriceAlert(Guid alertGuid);
        CustomerPriceAlert GetCustomerPriceAlert(Guid contactId, Enums.AlertType alertType, decimal priceAlert);
        void Delete(Guid id);
        bool DeleteMany(List<Guid> ids);
        void Add(CustomerPriceAlert alert);
    }
}