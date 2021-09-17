using System;
using System.Data.Entity;
using TRM.Web.Models.EntityFramework.EmailBackInStock;

namespace TRM.Web.Business.DataAccess
{
    public interface IEmailBackInStockRepository: IDisposable
    {
        Guid AddSignUpSubscription(BackInStockSubscription subscription);
        DbSet<BackInStockSubscription> GetAll();
        int SaveChange();
    }
}
