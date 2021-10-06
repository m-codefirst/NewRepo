using System;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.IntegrationServices.Constants;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Models.Export;
using TRM.IntegrationServices.Models.Export.Payloads.DTOs;
using TRM.IntegrationServices.Models.Impersonation;
using TRM.Shared.Extensions;
using TRM.Shared.Helpers;
using StringConstants = TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Services
{
    public interface IExportTransactionService
    {
        bool CreateExportTransaction<T>(T payload, string contactId, ExportTransactionType transactionType)
            where T : IAmAnExportTransactionPayload;

        bool CreateExportTransaction<T>(T payload, string contactId, ExportTransactionType transactionType,
            out ExportTransaction savedExportTransaction) where T : IAmAnExportTransactionPayload;

        bool CreateExportTransaction<T>(T payload, string contactId, ExportTransactionType transactionType,
            out ExportTransaction savedExportTransaction, CustomerContact customerContact) where T : IAmAnExportTransactionPayload;

        bool UpdateExportTransaction<T>(string transactionId, T payload, string contactId,
            ExportTransactionType transactionType) where T : IAmAnExportTransactionPayload;

        bool AddOrUpdateExportTransaction(ExportTransaction exportTransaction);
    }

    [ServiceConfiguration(typeof(IExportTransactionService))]
    public class ExportTransactionService : IExportTransactionService
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ExportTransactionService));

        private readonly CustomerContext _customerContext;
        private readonly IUserService _userService;
        private readonly IImpersonationLogService _impersonationLogService;
        private readonly IExportTransactionsRepository _exportTransactionsRepository;

        public ExportTransactionService(
            CustomerContext customerContext,
            IUserService userService,
            IImpersonationLogService impersonationLogService,
            IExportTransactionsRepository exportTransactionsRepository)
        {
            _userService = userService;
            _customerContext = customerContext;
            _impersonationLogService = impersonationLogService;
            _exportTransactionsRepository = exportTransactionsRepository;
        }

        public bool CreateExportTransaction<T>(T payload, string contactId, ExportTransactionType transactionType) where T : IAmAnExportTransactionPayload
        {
            try
            {
                // BULL-2192 Minimise Bullion Customer updates to AX
                var exportWrapper = BuildExportTransactionWrapper(payload, contactId);
                var exportTransaction =
                    _exportTransactionsRepository.CreateExportTransaction(exportWrapper, contactId, transactionType);

                var isSuccess = _exportTransactionsRepository.AddTransactionForExport(exportTransaction);

                if (isSuccess)
                {
                    UserDetails userDetails;
                    if (_userService.IsImpersonating())
                        userDetails = UserDetails.ForImpersonator(CustomerContext.Current, RequestHelper.GetClientIpAddress());
                    else
                        userDetails = UserDetails.ForCustomer(CustomerContext.Current, RequestHelper.GetClientIpAddress());

                    var impersonationLog = ImpersonationLog.ForExportTransaction(
                        userDetails,
                        exportTransaction,
                        null);
                    _impersonationLogService.CreateLog(impersonationLog);
                }

                if (isSuccess && transactionType == ExportTransactionType.UpdateBullion)
                {
                    _exportTransactionsRepository.UpdateOtherBullionUserTransactionsToSuperseded(exportTransaction, contactId);
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return false;
            }
        }

        public bool CreateExportTransaction<T>(T payload, string contactId, ExportTransactionType transactionType, out ExportTransaction savedExportTransaction) where T : IAmAnExportTransactionPayload
        {
            return this.CreateExportTransaction(payload, contactId, transactionType, out savedExportTransaction, null);
        }
        public bool CreateExportTransaction<T>(T payload, string contactId, ExportTransactionType transactionType, out ExportTransaction savedExportTransaction, CustomerContact customerContact) where T : IAmAnExportTransactionPayload
        {
            try
            {
                var exportWrapper = BuildExportTransactionWrapper(payload, contactId, customerContact);
                var exportTransaction =
                    _exportTransactionsRepository.CreateExportTransaction(exportWrapper, contactId, transactionType);
                var isSuccess = _exportTransactionsRepository.AddTransactionForExport(exportTransaction);
                savedExportTransaction = _exportTransactionsRepository.GetTransactionForExport(exportTransaction.TransactionId);

                if (isSuccess && transactionType == ExportTransactionType.UpdateBullion)
                {
                    _exportTransactionsRepository.UpdateOtherBullionUserTransactionsToSuperseded(exportTransaction, contactId);
                }

                return isSuccess;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                savedExportTransaction = null;
                return false;
            }
        }

        public bool UpdateExportTransaction<T>(string transactionId, T payload, string contactId, ExportTransactionType transactionType) where T : IAmAnExportTransactionPayload
        {
            try
            {
                var matchTransaction = _exportTransactionsRepository.GetTransactionForExport(transactionId);
                if (matchTransaction == null) return false;

                var exportWrapper = BuildExportTransactionWrapper(payload, contactId);
                exportWrapper.Header.TransactionId = transactionId;

                var exportTransaction = _exportTransactionsRepository.CreateExportTransaction(exportWrapper, contactId, transactionType);
                matchTransaction.Payload = exportTransaction.Payload;

                return _exportTransactionsRepository.AddTransactionForExport(matchTransaction);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return false;
            }
        }

        public bool AddOrUpdateExportTransaction(ExportTransaction exportTransaction)
        {
            return _exportTransactionsRepository.AddTransactionForExport(exportTransaction);
        }

        /// <summary>
        /// Build export transaction wrapper with param as payload object
        /// </summary>
        /// <typeparam name="T">Must inherit IAmAnExportTransactionPayload</typeparam>
        /// <param name="payload">Payload object</param>
        /// <param name="contactId"></param>
        /// <returns></returns>
        private ExportTransactionWrapper<T> BuildExportTransactionWrapper<T>(T payload, string contactId) where T : IAmAnExportTransactionPayload
        {
            return this.BuildExportTransactionWrapper(payload, contactId, null);
        }
        private ExportTransactionWrapper<T> BuildExportTransactionWrapper<T>(T payload, string contactId, CustomerContact customerContact) where T : IAmAnExportTransactionPayload
        {
            var contact = customerContact ?? _customerContext.GetContactById(new Guid(contactId));
            if (contact == null) throw new ArgumentNullException(nameof(contact));

            var obsAccountNumber = contact.GetStringProperty(StringConstants.CustomFields.BullionObsAccountNumber);

            var impersonateUser = _userService.GetImpersonateUser(customerContact);
            return new ExportTransactionWrapper<T>()
            {
                Header = _exportTransactionsRepository.CreateExportTransactionHeader(impersonateUser, obsAccountNumber),
                Payload = payload
            };
        }
    }
}