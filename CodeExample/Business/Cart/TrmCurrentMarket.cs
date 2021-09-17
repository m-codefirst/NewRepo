using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using TRM.Shared.Helpers;
using TRM.Web.Constants;
using TRM.Web.Helpers;

namespace TRM.Web.Business.Cart
{
    public class TrmCurrentMarket : ICurrentMarket
    {
        private readonly CustomerContext _customerContext;
        private readonly IMarketService _marketService;
        private readonly IAmContactAuditHelper _contactAuditHelper;

        public TrmCurrentMarket(CustomerContext customerContext, IMarketService marketService, IAmContactAuditHelper contactAuditHelper)
        {
            _customerContext = customerContext;
            _marketService = marketService;
            _contactAuditHelper = contactAuditHelper;

        }

        public IMarket GetCurrentMarket()
        {
            var customer = _customerContext.CurrentContact;

            return customer?.Properties[Shared.Constants.StringConstants.CustomFields.MarketIdFieldName].Value != null ?
                _marketService.GetMarket(customer.Properties[Shared.Constants.StringConstants.CustomFields.MarketIdFieldName].Value.ToString()) :
                GetCurrentMarketFromCookie();
        }

        public void SetCurrentMarket(MarketId marketId)
        {
            var customer = _customerContext.CurrentContact;

            if (customer == null)
            {
                SetCurrentMarketCookie(marketId);
                return;
            }

            var currentMarket = customer.Properties[Shared.Constants.StringConstants.CustomFields.MarketIdFieldName].Value;

            if (currentMarket != null && currentMarket.ToString() == marketId.Value) return;

            customer.Properties[Shared.Constants.StringConstants.CustomFields.MarketIdFieldName].Value = marketId.Value;

            _contactAuditHelper.WriteCustomerAudit(customer, "Market Changed", $"From {currentMarket} to {marketId.Value}");

            customer.SaveChanges();
        }

        private IMarket GetCurrentMarketFromCookie()
        {
            var cookie = CookieHelper.GetBasicCookie(StringConstants.MarketCookieName);

            return cookie != null ?
                _marketService.GetMarket(cookie.Value) :
                _marketService.GetMarket(MarketId.Default);
        }

        private void SetCurrentMarketCookie(MarketId marketId)
        {
            var cookie = CookieHelper.GetBasicCookie(StringConstants.MarketCookieName);

            if (cookie == null || cookie.Value != marketId.Value)
            {
                CookieHelper.CreateBasicCookie(StringConstants.MarketCookieName, marketId.Value);
            }
        }
    }
}