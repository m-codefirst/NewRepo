using System;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.EntityFramework.EmailBackInStock;

namespace TRM.Web.Services
{
    public interface IBackInStockService
    {
        Guid? SignupEmailBackInStock(BackInStockSubscription subscription);
        bool IsSignupExisted(string email, string variantCode);
        string NotifyForAvailableVariants();
        bool CanSignupEmailForVariant(TrmVariant variant);
    }
}
