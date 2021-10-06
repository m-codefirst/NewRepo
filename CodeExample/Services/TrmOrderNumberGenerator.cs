using System.Globalization;
using EPiServer.Commerce.Order;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.Web;
using TRM.Web.Models.Pages;

namespace TRM.Web.Services
{
    public class TrmOrderNumberGenerator : IOrderNumberGenerator
    {
        private readonly ContentLoader _contentLoader;

        public TrmOrderNumberGenerator(ContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public string GenerateOrderNumber(IOrderGroup orderGroup)
        {
            var startPage = _contentLoader.Get<PageData>(SiteDefinition.Current.StartPage.ToPageReference()) as StartPage;
            var checkoutPage = _contentLoader.Get<PageData>(startPage?.CheckoutPage) as CheckoutPage;
            if (checkoutPage == null) return orderGroup.OrderLink.OrderGroupId.ToString(CultureInfo.CurrentCulture);

            return string.Concat(checkoutPage.OrderNumberPrefix, orderGroup.OrderLink.OrderGroupId.ToString(CultureInfo.CurrentCulture));
        }
    }
}