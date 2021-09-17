using System;
using EPiServer;
using EPiServer.Business.Commerce.Payment.Mastercard.Mastercard;
using EPiServer.Commerce.Order;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Newtonsoft.Json;
using TRM.Shared.Constants;
using TRM.Shared.DataAccess;
using TRM.Shared.Models.DTOs;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Business.Email;
using TRM.Web.Extentions;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Bullion;
using TRM.Web.Models.Pages;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.ThirdPartyTransactions
{
    [ScheduledPlugIn(
        DisplayName = "Third Party Transactions", 
        Description = "Update the status of third party transactions that are stuck on pending status")]
    public class ThirdPartyTransactions : ScheduledJobBase
    {
        private const string DefaultIpAddress = "ThirdPartyTransactions.ScheduledJob";

        private readonly IThirdPartyTransactionRepository _thirdPartyTransactionRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IBullionAccountPaymentsHelper _bullionAccountPaymentsHelper;
        private readonly IBullionEmailHelper _bullionEmailHelper;
        private readonly IAccountCreditPaymentHelper _accountCreditPaymentHelper;
        private readonly IAmCartHelper _cartHelper;
        private readonly IAmOrderHelper _orderHelper;
        private readonly IContentLoader _contentLoader;
        private readonly ICurrentMarket _currentMarket;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public ThirdPartyTransactions(IThirdPartyTransactionRepository thirdPartyTransactionRepository,
            IOrderRepository orderRepository, IBullionAccountPaymentsHelper bullionAccountPaymentsHelper, 
            IBullionEmailHelper bullionEmailHelper, IAccountCreditPaymentHelper accountCreditPaymentHelper, 
            IAmCartHelper cartHelper, IAmOrderHelper orderHelper, IContentLoader contentLoader, ICurrentMarket currentMarket)
        {
            _thirdPartyTransactionRepository = thirdPartyTransactionRepository;
            _orderRepository = orderRepository;
            _bullionAccountPaymentsHelper = bullionAccountPaymentsHelper;
            _bullionEmailHelper = bullionEmailHelper;
            _accountCreditPaymentHelper = accountCreditPaymentHelper;
            _cartHelper = cartHelper;
            _orderHelper = orderHelper;
            _contentLoader = contentLoader;
            _currentMarket = currentMarket;
        }

        public override string Execute()
        {
            try
            {
                OnStatusChanged($"Starting execution of {this.GetType()}");

                var transactions = _thirdPartyTransactionRepository.GetPendingTransactions();

                foreach (var transaction in transactions)
                {
                    if (transaction.TransactionType == TransactionType.Mastercard)
                    {
                        ProcessMastercardTransaction(transaction);
                    }

                    if (transaction.TransactionStatus != ThirdPartyTransactionStatus.Success &&
                        transaction.TransactionDate < DateTime.Now.AddDays(-5))
                    {
                        transaction.TransactionStatus = ThirdPartyTransactionStatus.Error;
                    }
                }

                _thirdPartyTransactionRepository.BulkUpdateTransactions(transactions);

                return "";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        private void ProcessMastercardTransaction(ThirdPartyTransaction transaction)
        {
            var paymentDto = JsonConvert.DeserializeObject<ManualPaymentDto>(transaction.TransactionPayloadJson);
            if (paymentDto == null)
            {
                transaction.TransactionStatus = ThirdPartyTransactionStatus.Error;
                return;
            }

            var gateway = new MastercardPaymentGateway();

            var mastercardOrder = gateway.RetrieveOrder(paymentDto);

            if (mastercardOrder.Result != "SUCCESS")
            {
                transaction.TransactionStatus = ThirdPartyTransactionStatus.Error;
                return;
            }

            var customer = CustomerContext.Current.GetContactById(paymentDto.CustomerId);

            if (paymentDto.MastercardCardNumber == null) paymentDto.MastercardCardNumber = string.Empty;

            switch (paymentDto.PaymentType)
            {
                case Enums.TrmPaymentType.Purchase:
                    var cart = _orderRepository.LoadCart<ICart>(paymentDto.CustomerId, "Default", _currentMarket);
                    if (cart == null)
                    {
                        //already converted to a purchase order therefore just set to successful
                        transaction.TransactionStatus = ThirdPartyTransactionStatus.Success;
                        transaction.TransactionPayloadJson = string.Empty;
                        return;
                    }

                    var startPage = this.GetAppropriateStartPageForSiteSpecificProperties();
                    var checkoutPage = _contentLoader.Get<CheckoutPage>(startPage?.CheckoutPage);

                    if (checkoutPage != null)
                    {
                        var po = _cartHelper.ConvertToPurchaseOrder(cart, checkoutPage.OrderNumberPrefix);
                        var poVm = _cartHelper.GetPurchaseOrderViewModel(po);
                        _orderHelper.SaveSalesOrder(poVm, customer.UserId, DefaultIpAddress);
                    }

                    transaction.TransactionStatus = ThirdPartyTransactionStatus.Success;
                    transaction.TransactionPayloadJson = string.Empty;

                    break;

                case Enums.TrmPaymentType.AccountTopUp:
                    _accountCreditPaymentHelper.CreateCreditPayment(paymentDto, customer);

                    transaction.TransactionStatus = ThirdPartyTransactionStatus.Success;
                    transaction.TransactionPayloadJson = string.Empty;
                    break;

                case Enums.TrmPaymentType.BullionWalletTopUp:
                    var lastFour = paymentDto.MastercardCardNumber.PadLeft(4, '0');
                    lastFour = lastFour.Substring(lastFour.Length - 4, 4);

                    var exportTransactionId = _bullionAccountPaymentsHelper.CreateExportTransaction(paymentDto, null, lastFour);
                    _bullionAccountPaymentsHelper.CreateFundTransactionHistory(paymentDto, exportTransactionId);
                    _bullionAccountPaymentsHelper.UpdateCustomerBalances(customer, paymentDto.Amount);
                    _bullionEmailHelper.SendBullionAddFundsEmail(customer, paymentDto);

                    transaction.TransactionStatus = ThirdPartyTransactionStatus.Success;
                    transaction.TransactionPayloadJson = string.Empty;
                    break;
            }
        }
    }
}
