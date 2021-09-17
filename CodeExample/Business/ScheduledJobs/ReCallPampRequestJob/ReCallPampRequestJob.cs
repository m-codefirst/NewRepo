using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using EPiServer.Commerce.Order;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;
using PricingAndTradingService.Models.APIResponse;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Models.Export.Payloads.DTOs;
using TRM.IntegrationServices.Models.Export.Payloads.PurchaseOrders;
using TRM.Shared.Extensions;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Helpers;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Models.ViewModels.Cart;
using TRM.Web.Services;
using TRM.Web.Services.Portfolio;
using StringConstants = TRM.IntegrationServices.Constants.StringConstants;

namespace TRM.Web.Business.ScheduledJobs.ReCallPampRequestJob
{
    [ScheduledPlugIn(
        DisplayName = "[Bullion] Re-call Pamp Request Job",
        Description = @"Re-call Pamp Request Job")]
    public class ReCallPampRequestJob : ScheduledJobBase
    {
        private bool _stopSignaled;

        private Lazy<IExportTransactionsRepository> ExportTransactionsRepository = new Lazy<IExportTransactionsRepository>(() => ServiceLocator.Current.GetInstance<IExportTransactionsRepository>());
        private Lazy<IAmBullionContactHelper> BullionContactHelper = new Lazy<IAmBullionContactHelper>(() => ServiceLocator.Current.GetInstance<IAmBullionContactHelper>());
        private Lazy<IBullionTradingService> BullionTradingService => new Lazy<IBullionTradingService>(() => ServiceLocator.Current.GetInstance<IBullionTradingService>());
        private Lazy<IBullionEmailHelper> BullionEmailHelper => new Lazy<IBullionEmailHelper>(() => ServiceLocator.Current.GetInstance<IBullionEmailHelper>());
        private Lazy<IGlobalPurchaseLimitService> GlobalPurchaseLimitService => new Lazy<IGlobalPurchaseLimitService>(() => ServiceLocator.Current.GetInstance<IGlobalPurchaseLimitService>());
        private Lazy<IBullionPortfolioService> BullionPortfolioService => new Lazy<IBullionPortfolioService>(() => ServiceLocator.Current.GetInstance<IBullionPortfolioService>());
        private Lazy<IAmTransactionHistoryHelper> TransactionHistoryHelper => new Lazy<IAmTransactionHistoryHelper>(() => ServiceLocator.Current.GetInstance<IAmTransactionHistoryHelper>());
        private Lazy<IAmCartHelper> CartHelper => new Lazy<IAmCartHelper>(() => ServiceLocator.Current.GetInstance<IAmCartHelper>());
        private Lazy<IAmOrderHelper> OrderHelper => new Lazy<IAmOrderHelper>(() => ServiceLocator.Current.GetInstance<IAmOrderHelper>());
        private Lazy<IOrderGroupCalculator> OrderGroupCalculator => new Lazy<IOrderGroupCalculator>(() => ServiceLocator.Current.GetInstance<IOrderGroupCalculator>());
        private Lazy<CustomerContext> CustomerContext => new Lazy<CustomerContext>(() => ServiceLocator.Current.GetInstance<CustomerContext>());

        private const string DefaultIpAddress = "ReCallPampRequestJob.ScheduledJob";

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public ReCallPampRequestJob()
        {
            IsStoppable = true;
        }

        public override string Execute()
        {
            try
            {
                OnStatusChanged($"Starting execution of {GetType()}");

                var exportTransactions = ExportTransactionsRepository.Value.GetRetryWithPampPurchaseOrdersExportTransaction(new string[] { });

                if (exportTransactions == null || !exportTransactions.Any()) return "There are no export transactions need to re-call Pamp Request";

                var successfulExportTransactions = new List<IntegrationServices.Models.Export.ExportTransaction>();
                var rejectedExportTransactions = new List<IntegrationServices.Models.Export.ExportTransaction>();

                foreach (var exportTransaction in exportTransactions)
                {
                    var purchaseOrdersPayload = ExportTransactionsRepository.Value.DeserializePayload(exportTransaction.Payload, typeof(ExportTransactionWrapper<PurchaseOrdersExportTransactionPayload>)) as ExportTransactionWrapper<PurchaseOrdersExportTransactionPayload>;

                    if (purchaseOrdersPayload == null) continue;

                    var orderId = purchaseOrdersPayload.Payload.OrderHeader.OrderRef;

                    var po = GetPurchaseOrderByOrderNumber(orderId);

                    var finishQuoteResult = RetrySendFinishPampQuoteRequest(po);
                    if (finishQuoteResult?.Result == null) continue;

                    if (finishQuoteResult.Result.Success)
                    {
                        successfulExportTransactions.Add(exportTransaction);
                        ExecutePurchaseFlowWhenQuoteSuccess(po, finishQuoteResult);
                        continue;
                    }

                    if (finishQuoteResult.Result.Success == false && finishQuoteResult.Result.ExecuteOnQuoteStatus == PricingAndTradingService.Models.Constants.PampFinishQuoteStatus.Rejected)
                    {
                        rejectedExportTransactions.Add(exportTransaction);
                        ExecutePurchaseFlowWhenQuoteRejected(po);
                    }
                }

                if (successfulExportTransactions.Any()) UpdateExportTransactionWhenQuoteSuccess(successfulExportTransactions);

                if (rejectedExportTransactions.Any()) DeleteExportTransactionWhenQuoteRejected(rejectedExportTransactions);

                return _stopSignaled ? "Stop of job was called"
                    : BuildTheMessage(exportTransactions, successfulExportTransactions, rejectedExportTransactions);
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }

        private void UpdateExportTransactionWhenQuoteSuccess(List<IntegrationServices.Models.Export.ExportTransaction> successfulExportTransactions)
        {
            ExportTransactionsRepository.Value.UpdateStatusExportTransactions(successfulExportTransactions.Select(x => x.TransactionId), StringConstants.AxIntegrationStatus.NotSentToICore);
        }

        //If re-call = rejected, 
        //1. delete from Export Txn
        private void DeleteExportTransactionWhenQuoteRejected(List<IntegrationServices.Models.Export.ExportTransaction> rejectedExportTransactions)
        {
            ExportTransactionsRepository.Value.DeleteExportTransactions(rejectedExportTransactions.Select(x => x.TransactionId));
        }

        //If re-call = rejected, 
        //1. update purchase order properties
        //2. notify customer via email
        private void ExecutePurchaseFlowWhenQuoteRejected(IPurchaseOrder po)
        {
            //Update purchase order properties
            OrderHelper.Value.UpdatePurchaseOrderWhenPampQuoteRejected(po);
            //Send mail notify
            BullionEmailHelper.Value.SendCancelPurchaseOrderEmail(PopulateCancelPurchaseOrders(po));
        }

        private CancelPurchaseOrderModel PopulateCancelPurchaseOrders(IPurchaseOrder purchaseOrder)
        {
            return new CancelPurchaseOrderModel
            {
                CustomerId = purchaseOrder.CustomerId,
                PurchaseNumber = purchaseOrder.OrderNumber,
                PurchaseDate = purchaseOrder.Created.ToString("yyyy/MM/dd HH:mm:ss")
            };
        }

        //if re-call = success, 
        //1. update po
        //2. Update global purchase limit
        //3. Update portfolio
        //4. Log transaction history
        private void ExecutePurchaseFlowWhenQuoteSuccess(IPurchaseOrder po, ExecuteResponse finishQuoteResult)
        {
            //Update Po
            OrderHelper.Value.UpdatePurchaseOrderWhenPampQuoteSuccess(po, finishQuoteResult);
            //update balance
            var totals = OrderGroupCalculator.Value.GetOrderGroupTotals(po);
            BullionContactHelper.Value.UpdateBalances(CustomerContext.Value.CurrentContact, -totals.Total.Amount, -totals.Total.Amount, -totals.Total.Amount);
            //Update global purchase limit
            GlobalPurchaseLimitService.Value.UpdateGlobalPurchaseLimits(po);
            //Update portfolio
            BullionPortfolioService.Value.CreatePortfolioContentsWhenPurchase(po);
            //Log transaction history
            TransactionHistoryHelper.Value.LogThePurchaseTransaction(po, DefaultIpAddress);
            //Send email confirmation
            var purchaseOrder = CartHelper.Value.GetPurchaseOrderViewModel(po);
            BullionEmailHelper.Value.SendBullionOrderConfirmationEmail(purchaseOrder.ConvertToInvestmentPurchaseOrder());
        }

        private string BuildTheMessage(
            IEnumerable<IntegrationServices.Models.Export.ExportTransaction> exportTransactions,
            IEnumerable<IntegrationServices.Models.Export.ExportTransaction> successfulExportTransactions,
            IEnumerable<IntegrationServices.Models.Export.ExportTransaction> rejectedExportTransactions)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{exportTransactions.Count()} Purchase Orders need re-call PAMP");
            stringBuilder.AppendLine($"{successfulExportTransactions.Count()} Successful Purchase Orders");
            stringBuilder.AppendLine($"{rejectedExportTransactions.Count()} Rejected Purchase Orders");

            return stringBuilder.ToString();
        }

        private static IPurchaseOrder GetPurchaseOrderByOrderNumber(string orderId)
        {
            var parameters = new OrderSearchParameters
            {
                SqlMetaWhereClause = $"Meta.TrackingNumber = '{orderId}'"
            };

            var options = new OrderSearchOptions
            {
                CacheResults = false,
                RecordsToRetrieve = 1,
                StartingRecord = 0,
                Classes = new StringCollection { "PurchaseOrder" },
                Namespace = "Mediachase.Commerce.Orders"
            };

            var po = OrderContext.Current.Search<PurchaseOrder>(parameters, options).FirstOrDefault();
            return po;
        }

        private ExecuteResponse RetrySendFinishPampQuoteRequest(IPurchaseOrder po)
        {
            var quotRequestDtoId = po?.GetStringProperty(Shared.Constants.StringConstants.CustomFields.BullionPAMPRequestQuoteId);

            if (string.IsNullOrEmpty(quotRequestDtoId)) return null;

            return BullionTradingService.Value.FinishQuoteRequest(Guid.Parse(quotRequestDtoId));
        }
    }
}