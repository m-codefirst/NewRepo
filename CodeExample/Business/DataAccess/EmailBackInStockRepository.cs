using System;
using System.Data.Entity;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.EmailBackInStock;

namespace TRM.Web.Business.DataAccess
{
    public class EmailBackInStockRepository : DbContextDisposable<BackInStockSubscriptionContext>, IEmailBackInStockRepository
    {
        public Guid AddSignUpSubscription(BackInStockSubscription subscription)
        {
            context.BackInStockSubscriptions.Add(subscription);
            context.SaveChanges();
            return subscription.Id;
        }

        public DbSet<BackInStockSubscription> GetAll()
        {
            return context.BackInStockSubscriptions;
        }

        public int SaveChange()
        {
            return  context.SaveChanges();
        }
    }
}