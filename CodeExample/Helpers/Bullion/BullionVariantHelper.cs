using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;
using TRM.Web.Services;

namespace TRM.Web.Helpers.Bullion
{
    [ServiceConfiguration(typeof(IBullionVariantHelper), Lifecycle = ServiceInstanceScope.Singleton)]
    public class BullionVariantHelper : IBullionVariantHelper
    {
        private readonly CustomerContext _customerContext;
        private readonly IKycHelper _kycHelper;
        private readonly IBackInStockService _backInStockService;
        private readonly IAmBullionContactHelper _bullionContactHelper;

        public BullionVariantHelper(
            CustomerContext customerContext,
            IKycHelper kycHelper,
            IBackInStockService backInStockService, IAmBullionContactHelper bullionContactHelper)
        {
            _customerContext = customerContext;
            _kycHelper = kycHelper;
            _backInStockService = backInStockService;
            _bullionContactHelper = bullionContactHelper;
        }
        public bool ShowBuyNow(PreciousMetalsVariantBase variant, StockSummaryDto stockSummary, bool stoppedTrading,
             bool isBullionAccount, bool unableToPurchaseBullion)
        {
            return ShowAddToBasket(stockSummary, stoppedTrading, isBullionAccount, unableToPurchaseBullion) && variant.CanBuyNow;
        }

        public bool ShowAddToBasket(StockSummaryDto stockSummary, bool stoppedTrading, bool isBullionAccount, bool unableToPurchaseBullion)
        {
            if (stoppedTrading || isBullionAccount == false || unableToPurchaseBullion) return false;
            
            return stockSummary.CanAddToBasket;
        }

        public bool ShowNotifyWhenInStock(PreciousMetalsVariantBase variant, StockSummaryDto stockSummary, bool stoppedTrading,
            bool isBullionAccount)
        {
            if (stoppedTrading || isBullionAccount == false) return false;

            if (stockSummary == null || stockSummary.NotAvailableForEwbis) return false;

            return _backInStockService.CanSignupEmailForVariant(variant);
        }

        public bool ShowActiveYourBullionAccount()
        {
            var contact = _customerContext.CurrentContact;

            if (contact == null) return false;

            return !_bullionContactHelper.IsBullionAccount(contact);
        }

        public bool ShowUnableSellProductText(PreciousMetalsVariantBase variant, bool stoppedTrading, bool isBullionAccount)
        {
            return ShowUnableSellProductMessage(variant, stoppedTrading, isBullionAccount);
        }

        public bool ShowNeedConfirmKyc(bool isBullionAccount)
        {
            return isBullionAccount && !_kycHelper.KycCanProceed(_customerContext.CurrentContact);
        }

        public bool ShowCanNotPensionMessage(PreciousMetalsVariantBase signatureVariant, bool isSippCustomer, bool canAddToBasket)
        {
            return canAddToBasket && isSippCustomer && !signatureVariant.CanPensionBuy;
        }

        public bool ShowUnableToPurchasePreciousMetals(bool isBullionAccount, bool unableToPurchaseBullion)
        {
            return isBullionAccount && unableToPurchaseBullion;
        }

        public bool ShowCannotHaveVariantsDelivered(bool isSippCustomer, PreciousMetalsVariantBase currentContent)
        {
            var physicalVariantBase = currentContent as PhysicalVariantBase;
            return isSippCustomer && physicalVariantBase != null && !physicalVariantBase.CanVault;
        }

        private bool ShowUnableSellProductMessage(PreciousMetalsVariantBase variant, bool stoppedTrading, bool isBullionAccount)
        {
            if (ShowActiveYourBullionAccount()) return false;

            //TODO: Need to implement later based on Holli's comment
            //The message you are displaying is when a customer viewing has restrictions set up at a product level,
            //this could be based to the country that they are assigned to e.g. can't sell gold to users in china, or SIPPS/SASS customers can not but platinum and silver 
            return false;
        }
    }
}