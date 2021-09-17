using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Models.DTOs;

namespace TRM.Web.Helpers.Bullion
{
    public interface IBullionVariantHelper
    {
        bool ShowBuyNow(PreciousMetalsVariantBase variant, StockSummaryDto stockSummary, bool stoppedTrading, bool isBullionAccount,bool unableToPurchaseBullion);
        bool ShowAddToBasket(StockSummaryDto stockSummary, bool stoppedTrading, bool isBullionAccount, bool unableToPurchaseBullion);
        bool ShowNotifyWhenInStock(PreciousMetalsVariantBase variant, StockSummaryDto stockSummary, bool stoppedTrading, bool isBullionAccount);
        bool ShowActiveYourBullionAccount();
        bool ShowUnableSellProductText(PreciousMetalsVariantBase signatureVariant, bool stoppedTrading, bool isBullionAccount);
        bool ShowNeedConfirmKyc(bool isBullionAccount);
        bool ShowCanNotPensionMessage(PreciousMetalsVariantBase signatureVariant, bool isSippCustomer, bool canAddToBasket);
        bool ShowUnableToPurchasePreciousMetals(bool isBullionAccount, bool unableToPurchaseBullion);

        bool ShowCannotHaveVariantsDelivered(bool isSippCustomer, PreciousMetalsVariantBase currentContent);
    }
}