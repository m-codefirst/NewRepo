using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Routing;
using Mediachase.Commerce.Customers;
using TRM.Web.Extentions;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Pages;

namespace TRM.Web.Helpers
{
    public class SiteHelper : ISiteHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly CustomerContext _customerContext;
        private readonly IContentRouteHelper _routeHelper;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public SiteHelper(IContentLoader contentLoader, CustomerContext customerContext, IContentRouteHelper routeHelper, IAmBullionContactHelper bullionContactHelper)
        {
            _contentLoader = contentLoader;
            _customerContext = customerContext;
            _routeHelper = routeHelper;
            _bullionContactHelper = bullionContactHelper;
        }

        public StartPage StartPage => _contentLoader.GetAppropriateStartPageForSiteSpecificProperties();

        public bool StopTrading => StartPage == null || StartPage.StopTrading;

        public TrmHeaderMessageBlock TrmHeaderMessageBlock
        {
            get
            {
                var customer = _customerContext.CurrentContact;
                var currentPage = _routeHelper.Content;

                var investPage = currentPage as IControlInvestmentPage;
                var consumerProduct = currentPage as TrmVariant;

                if (StopTrading
                    || investPage != null && investPage.IsInvestmentPage && customer != null && (_bullionContactHelper.HasFailedStage1(customer) || _bullionContactHelper.HasFailedStage2(customer))
                    || consumerProduct != null && consumerProduct.IsItemNotInGbpCurrency())
                {
                    var trmHeaderMessageBlockLink = StartPage?.TrmHeaderMessageBlock;

                    if (ContentReference.IsNullOrEmpty(trmHeaderMessageBlockLink)) return null;

                    var block = _contentLoader.Get<TrmHeaderMessageBlock>(trmHeaderMessageBlockLink);

                    return block;
                }

                return null;
            }
        }

        public TrmImageBlock GetTrmImageBlock(ContentReference imageLink)
        {
            return ContentReference.IsNullOrEmpty(imageLink) ? null : _contentLoader.Get<IContentData>(imageLink) as TrmImageBlock;
        }
    }
}