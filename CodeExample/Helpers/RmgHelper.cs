using System;
using System.Data.Entity.Migrations;
using System.Linq;
using EPiServer;
using EPiServer.Business.Commerce.Payment.Mastercard.Mastercard;
using EPiServer.Cms.Shell;
using EPiServer.Core;
using EPiServer.Web;
using Hephaestus.CMS.Extensions;
using Mediachase.Commerce.Orders.Dto;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Constants;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.DTOs.RMG;
using TRM.Web.Models.EntityFramework.RmgOrders;
using TRM.Web.Models.Interfaces.Rmg;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.RMG;
using TRM.Shared.Extensions;

namespace TRM.Web.Helpers
{
    public class RmgHelper : IAmRmgHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IAmAPaymentMethodHelper _paymentMethodHelper;

        public RmgHelper(IContentLoader contentLoader,  IAmAPaymentMethodHelper paymentMethodHelper)
        {
            _contentLoader = contentLoader;
            _paymentMethodHelper = paymentMethodHelper;
        }
        public RmgBuyViewModel GetBuyViewModel(RmgBuyBlock block)
        {
            var vm = new RmgBuyViewModel { ThisBlock = block };


            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;

            var rmgSettings = startPage as IRmgSettings;

            if (rmgSettings == null) return vm;

            vm.MaxAmount = rmgSettings.RmgMaxAmount;
            vm.MinAmount = rmgSettings.RmgMinAmount;
            vm.WalletMinLength = rmgSettings.RmgMinWalletIdLength;
            vm.WalletMaxLength = rmgSettings.RmgMaxWalletIdLength;
            vm.CheckoutUrl = rmgSettings.RmgCheckOutPage.GetExternalUrl_V2() + "buy";

            return vm;

        }

        public RmgOrderSummary CreateOrder(decimal amount, string wallet)
        {
            var buy = new RmgOrderSummary() { Amount = amount };

            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage) as StartPage;

            var rmgSettings = startPage as IRmgSettings;

            if (rmgSettings == null) return buy;

            buy.PremiumPercentage = rmgSettings.RmgPremiumPercentage;
            buy.PremiumAmount = Math.Round(buy.Amount * buy.PremiumPercentage,2);
            buy.WalletId = wallet;
            buy.FirstPage = startPage.RmgFirstPage?.GetExternalUrl_V2() ?? startPage.ToExternalUrl();
            buy.MinAmount = startPage.RmgMinAmount;
            buy.MaxAmount = startPage.RmgMaxAmount;
            buy.CheckoutUrl = rmgSettings.RmgCheckOutPage.GetExternalUrl_V2() + "buy";
            return buy;

        }

        public RmgOrderSummary GetOrderSummary(RmgOrder order)
        {
            var summary = new RmgOrderSummary();

            summary.Amount = order.Amount;
            summary.PremiumAmount = order.PremiumAmount;
            summary.PremiumPercentage = order.PremiumPercentage;
            summary.WalletId = order.WalletId;

            return summary;
        }

        public RmgOrder RetrieveOrder(int id)
        {
            using (var db = new RmgOrdersContext(Shared.Constants.StringConstants.TrmCustomDatabaseName))
            {
                return db.RmgOrders.SingleOrDefault(x => x.Id == id);
            }
        }

        public RmgOrder RetrieveOrder(string orderId)
        {
            using (var db = new RmgOrdersContext(Shared.Constants.StringConstants.TrmCustomDatabaseName))
            {
                return db.RmgOrders.SingleOrDefault(x => x.OrderUid == orderId);
            }
        }
        public void SaveOrder(RmgOrder order)
        {
            using (var db = new RmgOrdersContext(Shared.Constants.StringConstants.TrmCustomDatabaseName))
            {
                order.Modified = DateTime.Now.ToUniversalTime();
                db.RmgOrders.AddOrUpdate(order);
                db.SaveChanges();
            }
        }

        public ManualPaymentDto Pay(RmgOrder order, RmgCheckoutPage currentPage)
        {

            var dto = new ManualPaymentDto();
            if (order.TransactionStatus == Enums.eGoldOrderStatus.Confirmed)
            {
                dto.Message = "This order has already been confirmed.";
                dto.PaymentSuccessful = false;
                return dto;
            }

            var orderId = order.OrderNumber;
            var paymentMethod = GetPaymentProvider(currentPage);

            var responseUrl = UriSupport.AbsoluteUrlBySettings(currentPage.ToExternalUrl());
            responseUrl = responseUrl.Replace("?epieditmode=true", string.Empty);
            responseUrl = UriUtil.Combine(responseUrl, "Payment3DsResult");
            responseUrl = UriUtil.AddQueryString(responseUrl, "ouid", order.OrderUid);

            dto = new ManualPaymentDto
            {
                OrderNumber = orderId,
                TransactionId = orderId,
                CurrencyCode = "GBP",
                IsAmexPayment = false,
                Amount = order.Total,
                MastercardSessionId = order.PaymentSessionId,
                ResponseUrl = responseUrl,
                PaymentMethodId = paymentMethod.PaymentMethodId

            };

            dto.BillingAddress = new Address
            {
                city = order.City,
                street = order.Address1,
                street2 = order.Address2,
                postcodeZip = order.Postcode,
                country = order.Country
            };

            var message = string.Empty;
            var gateway = new MastercardPaymentGateway();

            dto.PaymentSuccessful = gateway.ProcessNonCartPayment(dto, ref message);

            order.Payment3DsId = dto.Mastercard3DSecureId;
            order.NameOnCard = dto.NameOnCard;
            order.PaymentMethodId = dto.PaymentMethodId.ToString();
            order.PaymentIsAmex = "false";
            order.CaptureStatus = dto.CaptureStatus;
            order.CaptureTotalAuthorizedAmount = dto.CaptureTotalAuthorizedAmount;
            order.CaptureTotalCapturedAmount = dto.CaptureTotalCapturedAmount;
            order.CaptureAcquirerMessage = dto.CaptureAcquirerMessage;
            order.CaptureTransactionReceipt = dto.CaptureTransactionReceipt;
            order.CaptureAuthorizationCode = dto.AuthorizationCode;

            if (dto.PaymentSuccessful)
            {

                if (string.IsNullOrWhiteSpace(dto.AuthorizationCode)) return dto;

                order.AuthorisationCode = dto.AuthorizationCode;
                order.TransactionStatus = Enums.eGoldOrderStatus.Confirmed;

            }
            else
            {
                dto.Message = message;
                order.TransactionStatus = Enums.eGoldOrderStatus.Unsuccessfull;
                order.PaymentErrorMessage = message;
            }

            return dto;
        }

        public PaymentMethodDto.PaymentMethodRow GetPaymentProvider(RmgCheckoutPage currentPage)
        {
            var paymentProviders = _paymentMethodHelper.GetAvailablePaymentMethods();

            if (paymentProviders == null || !paymentProviders.Any()) return null;

            return paymentProviders.FirstOrDefault(p => p.Name == currentPage.PaymentProviderName);
        }

        public ManualPaymentDto Process3DsResponse(RmgCheckoutPage currentPage, RmgOrder order, string sid,
            string sessionId, string paRes)
        {
            var gateway = new MastercardPaymentGateway();
            var paymentMethod = GetPaymentProvider(currentPage);

            var paymentDto = new ManualPaymentDto
            {
                BillingAddress = new Address
                {
                    city = order.City,
                    country = order.Country,
                    street = order.Address1,
                    street2 = order.Address2,
                    postcodeZip = order.Postcode
                },
                OrderNumber = order.OrderNumber,
                TransactionId = sid,
                IsAmexPayment = false,
                Amount = order.Total,
                CurrencyCode = "GBP",
                SaveCard = false,
                PaymentMethodId = paymentMethod.PaymentMethodId,
                Mastercard3DsSid = order.Payment3DSSid
            };


            var errors = string.Empty;

            if (gateway.ProcessNonCart3Ds(sid, sessionId, paRes, paymentDto, ref errors))
            {
                order.Payment3DSEci = paymentDto.Mastercard3DsEci;
                order.Payment3DSSid = paymentDto.Mastercard3DsSid;
                order.Payment3DSVavid = paymentDto.Mastercard3DsVavid;
                order.Payment3DSVtSid = paymentDto.Mastercard3DsVtSid;
                order.AuthorisationCode = paymentDto.AuthorizationCode;
                order.CaptureStatus = paymentDto.CaptureStatus;
                order.CaptureTotalAuthorizedAmount = paymentDto.CaptureTotalAuthorizedAmount;
                order.CaptureTotalCapturedAmount = paymentDto.CaptureTotalCapturedAmount;
                order.CaptureAcquirerMessage = paymentDto.CaptureAcquirerMessage;
                order.CaptureAuthorizationCode = paymentDto.CaptureAuthorizationCode;
                order.CaptureTransactionReceipt = paymentDto.CaptureTransactionReceipt;
                order.NameOnCard = paymentDto.NameOnCard;
                SaveOrder(order);
            }

            paymentDto.Message = errors;
            return paymentDto;
        }

        public RmgOrder CreateOrder(RmgCheckoutStep1ViewModel viewModel)
        {
            var order = new RmgOrder()
            {
                CreatedDate = DateTime.Now.ToUniversalTime(),
                WalletId = viewModel.OrderSummary.WalletId,
                Amount = viewModel.OrderSummary.Amount,
                PremiumAmount = viewModel.OrderSummary.PremiumAmount,
                PremiumPercentage = viewModel.OrderSummary.PremiumPercentage,
                Total = viewModel.OrderSummary.Total,
                TransactionStatus = Enums.eGoldOrderStatus.Pending,
                Title = viewModel.Title,
                FirstName = viewModel.FirstName,
                LastName = viewModel.LastName,
                EmailAddress = viewModel.EmailAddress,
                MobileTelephone = viewModel.Telephone,
                MarketingEmails = viewModel.ByEmail,
                MarketingPost = viewModel.ByPost,
                MarketingTelephone = viewModel.ByTelephone,
                OrderUid = Guid.NewGuid().ToString()
            };

            return order;
        }
    }
}