using Mediachase.Commerce.Orders.Dto;
using TRM.Shared.Models.DTOs.Payments;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.DTOs.RMG;
using TRM.Web.Models.EntityFramework.RmgOrders;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.RMG;

namespace TRM.Web.Helpers
{
    public interface IAmRmgHelper
    {
        RmgBuyViewModel GetBuyViewModel(RmgBuyBlock block);
        RmgOrderSummary CreateOrder(decimal amount, string wallet);
        RmgOrderSummary GetOrderSummary(RmgOrder order);
        RmgOrder RetrieveOrder(int orderId);
        RmgOrder CreateOrder(RmgCheckoutStep1ViewModel viewModel);
        RmgOrder RetrieveOrder(string orderId);
        void SaveOrder(RmgOrder order);
        ManualPaymentDto Pay(RmgOrder order, RmgCheckoutPage currentPage);
        PaymentMethodDto.PaymentMethodRow GetPaymentProvider(RmgCheckoutPage currentPage);
        ManualPaymentDto Process3DsResponse(RmgCheckoutPage currentPage, RmgOrder order, string sid, string sessionId, string paRes);
    }
}
