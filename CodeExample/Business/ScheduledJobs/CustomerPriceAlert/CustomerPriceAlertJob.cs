using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Constants;
using TRM.Web.Extentions;
using TRM.Web.Facades;
using TRM.Web.Helpers;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.CustomerPriceAlert
{
    [ScheduledPlugIn(
        DisplayName = "Customer Price Alert Job", 
        Description = "Customer Price Alert Job")]
    public class CustomerPriceAlertJob : ScheduledJobBase
    {
        private bool _stopSignaled;

#pragma warning disable 649
        private readonly Injected<ICustomerPriceAlertRepository> _priceAlertRepository;
        private readonly Injected<PampMetalPriceSyncRepository> _pampMetalPriceSyncRepository;
        private readonly Injected<CustomerContextFacade> _customerContextFacade;
        private readonly Injected<IEmailHelper> _emailHelper;
        private readonly Injected<IAmBullionContactHelper> _bullionContactHelper;
#pragma warning restore 649

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public CustomerPriceAlertJob()
        {
            IsStoppable = true;
        }

        /// <summary>
        /// Called when a user clicks on Stop for a manually started job, or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }

        /// <summary>
        /// Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log and visible from admin mode</returns>
        public override string Execute()
        {
            try
            {
                //Call OnStatusChanged to periodically notify progress of job for manually started jobs
                OnStatusChanged("Starting execution of a scheduler to check each alert against the current indicative gold price in the given currency");

                var msg = ScanToCheckAlertPrice();

                //For long running jobs periodically check if stop is signaled and if so stop execution
                return _stopSignaled ? "Stop of job was called" : msg;
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        private string ScanToCheckAlertPrice()
        {
            //Query current indicative price
            var indicativeGoldPrices = _pampMetalPriceSyncRepository.Service
                .GetList()
                .OrderByDescending(x => x.CreatedDate)
                .Take(6) // One raw json generates 6 records in pamMetalSync table, 2 records for each currencies
                .Where(x => x.CustomerBuy) // Only get sell prices
                .ToDictionary(x => x.Currency, y => y.GoldPrice);

            //Scan all customer alert records based on type
            // type = higher -> get all records = current price >= alert price
            // type = lower -> get all records = current price <= alert price

            var pricesNeedToBeAlert =
                _priceAlertRepository.Service.GetCustomerPriceAlertWillBeMail(indicativeGoldPrices);

            var pricesAlertWillBeDelete = new List<Guid>();
            // foreach in the match records
            foreach (var customerPriceAlert in pricesNeedToBeAlert)
            {
                var customerContact = _customerContextFacade.Service.GetContactById(customerPriceAlert.CustomerId);

                string emailErrorMessage;
                var emailTemplatePage =
                    customerPriceAlert.Type.Equals(Enums.AlertType.Higher) ? 
                    _emailHelper.Service.GetBullionEmailPage(StringConstants.BullionEmailCategories.HigherPriceAlertEmail, out emailErrorMessage) 
                    : _emailHelper.Service.GetBullionEmailPage(StringConstants.BullionEmailCategories.LowerPriceAlertEmail, out emailErrorMessage);

                if(emailTemplatePage == null) continue;
                var customerFullName = _bullionContactHelper.Service.GetFullname(customerContact);
                if (_emailHelper.Service.SendPriceAlertEmail(emailTemplatePage,
                    customerContact, customerPriceAlert.AlertAtPrice.FormatPrice(customerPriceAlert.Currency),
                    indicativeGoldPrices.TryGet(customerPriceAlert.Currency).FormatPrice(customerPriceAlert.Currency), customerFullName))
                {
                    pricesAlertWillBeDelete.Add(customerPriceAlert.Id);
                }
            }
            
            // Bulk delete records in case send mail successfully
            if (pricesAlertWillBeDelete.Any())
            {
                _priceAlertRepository.Service.DeleteMany(pricesAlertWillBeDelete);
                return $"{pricesAlertWillBeDelete.Count} price alert emails sent to customers";
            }

            return $"There is not any price alert email to send.";
        }
    }
}