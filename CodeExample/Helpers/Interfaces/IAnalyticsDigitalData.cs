using Hephaestus.CMS.ViewModels;
using Mediachase.Commerce.Customers;
using System.Threading.Tasks;
using TRM.Web.AnalyticsDataLayer;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.DataLayer;
using TRM.Web.Models.ViewModels;
using TRM.Web.Models.ViewModels.Bullion;
using TRM.Web.Models.ViewModels.Cart;

namespace TRM.Web.Helpers.Interfaces
{
    public interface IAnalyticsDigitalData
    {
        AnalyticsDigitalDataModel GenerateMeasuringViewProductDetailJson(IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model);
        AnalyticsDigitalDataModel GenerateMeasuringProductImpressionsJson(IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> model);
        DataLayerVariantSchema GenerateVariantSchema(EntryPartialViewModel entryPartialViewModel, int? index = null);
        Task<AnalyticsDigitalDataModel> GetPageJsonAsync();
        Task<AnalyticsDigitalDataModel> GetCustomerJsonAsync();
        Task<AnalyticsDigitalDataModel> GetMarketJsonAsync();
        Task<AnalyticsDigitalDataModel> GetEcommerceJsonAsync();
        CheckoutModel GenerateCartInfo(LargeOrderGroupViewModel largeOrderGroupViewModel, CustomerContact customerContact, int step);
        CheckoutModel GenerateCartInfo(MixedOrderGroupViewModel mixedOrderGroupViewModel, CustomerContact customerContact, int step);
        void UpdateEcommerceAnalyticsDataOnConfirmation(OrderConfirmModel model);
        CheckoutModel GenerateSellBackInfo(SellBullionDefaultLandingViewModel viewModel, CustomerContact customerContact, int step, decimal sellPremiumValue);
    }
}
