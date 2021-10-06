using System;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Services;
using TRM.Web.Helpers;

namespace TRM.Web.Services.CustomersExportTransaction
{
    [ServiceConfiguration(ServiceType = typeof(IBullionCustomerExportService))]
    [ServiceConfiguration(ServiceType = typeof(ICustomersExportTransactionService))]
    public class CustomersExportTransactionService : CustomersExportTransactionBase, ICustomersExportTransactionService
    {
        private readonly ExportTransactionService _exportTransactionService;

        private readonly ILogger _logger = LogManager.GetLogger(typeof(CustomersExportTransactionService));
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public CustomersExportTransactionService(IExportTransactionsRepository exportTransactionsRepository,
            ExportTransactionService exportTransactionService, IAmBullionContactHelper bullionContactHelper) : base(exportTransactionsRepository)
        {
            _exportTransactionService = exportTransactionService;
            _bullionContactHelper = bullionContactHelper;
        }

        public override bool SaveBullionCustomerExportTransaction(CustomerContact customerContact, IntegrationServices.Constants.ExportTransactionType exportTransactionType)
        {
            try
            {
                if (customerContact?.PrimaryKeyId == null) return false;

                if (_bullionContactHelper.IsPensionProvider(customerContact)) return false;

                if (!_bullionContactHelper.IsBullionAccount(customerContact)) return false;

                var customerExportTransaction = PopulateCustomerInfoDto(customerContact);

                return _exportTransactionService.CreateExportTransaction(customerExportTransaction, customerContact.PrimaryKeyId.Value.ToString(), exportTransactionType);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return false;
            }
        }
    }
}