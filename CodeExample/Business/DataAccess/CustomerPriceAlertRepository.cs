using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.CustomerPriceAlert;

namespace TRM.Web.Business.DataAccess
{
    [ServiceConfiguration(typeof(ICustomerPriceAlertRepository))]
    public class CustomerPriceAlertRepository : DbContextDisposable<CustomerPriceAlertDbContext>, ICustomerPriceAlertRepository
    {
        private readonly CustomerPriceAlertDbContext _context;

        public CustomerPriceAlertRepository(CustomerPriceAlertDbContext context)
        {
            _context = context;
        }
        public IEnumerable<CustomerPriceAlert> GetCustomerAlertsByCustomer(Guid customerId)
        {
            return _context.CustomerPriceAlerts.Where(x => x.CustomerId == customerId);
        }

        public CustomerPriceAlert GetCustomerPriceAlert(Guid alertGuid)
        {
            return _context.CustomerPriceAlerts.FirstOrDefault(x => x.CustomerId == alertGuid);
        }

        public CustomerPriceAlert GetCustomerPriceAlert(Guid contactId, Enums.AlertType alertType, decimal priceAlert)
        {
            return _context.CustomerPriceAlerts
                .FirstOrDefault(x => x.CustomerId == contactId &&
                                     x.AlertAtPrice == priceAlert &&
                                     x.Type == alertType);
        }

        public void Delete(Guid id)
        {
            var alert = GetCustomerPriceAlert(id);
            if (alert == null) return;

            _context.CustomerPriceAlerts.Remove(alert);
            _context.SaveChanges();
        }

        public bool DeleteMany(List<Guid> ids)
        {
            var deleteQuery = @"DELETE FROM custom_CustomerPriceAlert WHERE Id IN(" +
                              string.Join(",", ids.Select(x => $"'{x.ToString()}'")) + ")";
            var noOfRowDeleted = _context.Database.ExecuteSqlCommand(deleteQuery);
            return noOfRowDeleted > 0;
        }

        public void Add(CustomerPriceAlert alert)
        {
            _context.CustomerPriceAlerts.Add(alert);
            _context.SaveChanges();
        }

        public IEnumerable<CustomerPriceAlert> GetCustomerPriceAlertWillBeMail(Dictionary<string, decimal> indicativeGoldPrices)
        {
            //Scan all customer alert records based on type
            // type = higher -> get all records = current price >= alert price
            // type = lower -> get all records = current price <= alert price

            var goldPriceGbp = indicativeGoldPrices.TryGet("GBP");
            var goldPriceUsd = indicativeGoldPrices.TryGet("USD");
            var goldPriceEur = indicativeGoldPrices.TryGet("EUR");

            return _context.CustomerPriceAlerts.AsNoTracking()
                .Where(x =>
                        (x.Currency == "GBP" && ((x.Type == Enums.AlertType.Higher && goldPriceGbp >= x.AlertAtPrice) ||
                                                (x.Type == Enums.AlertType.Lower && goldPriceGbp <= x.AlertAtPrice))) ||
                        (x.Currency == "USD" && ((x.Type == Enums.AlertType.Higher && goldPriceUsd >= x.AlertAtPrice) ||
                                                 (x.Type == Enums.AlertType.Lower && goldPriceUsd <= x.AlertAtPrice))) ||
                        (x.Currency == "EUR" && ((x.Type == Enums.AlertType.Higher && goldPriceEur >= x.AlertAtPrice) ||
                                                 (x.Type == Enums.AlertType.Lower && goldPriceEur <= x.AlertAtPrice)))
                        );
        }
    }
}