using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using TRM.Web.Models.DDS;
using TRM.Web.Models.EntityFramework;
using TRM.Web.Models.EntityFramework.MetalPrice;

namespace TRM.Web.Business.DataAccess
{
    public abstract class PampMetalSyncRepositoryBase<T> : DbContextDisposable<PampMetalPriceSyncDbContext>
        where T : class, IPampMetalPriceEntity, new()
    {
        private const string CacheMasterKey = "PampMetalSyncRepositoryBase_MasterCacheKey";
        private const string CachePrefix = "PampMetalSyncRepositoryBase_CachePrefix";

        private static object lockObj = new object();

        private Injected<ISynchronizedObjectInstanceCache> Cache { get; set; }

        protected virtual int GetBatchSize()
        {
            return 100;
        }

        public abstract Guid Insert(T pampMetalPrice);

        public abstract IQueryable<T> GetList();

        public virtual int BulkInsert(IEnumerable<T> prices)
        {
            return context.BulkInsert(prices, GetBatchSize()).Result;
        }

        protected virtual TimeSpan GetCacheTime()
        {
            return TimeSpan.FromDays(1);
        }

        protected virtual string GetTodayCacheKey()
        {
            return $"{CachePrefix}_{DateTime.Now.ToString("MMddyyyy")}";
        }

        public virtual IEnumerable<T> GetTodayItems()
        {
            string todayCacheKey = GetTodayCacheKey();

            var currentPrices = Cache.Service.Get<List<T>>(todayCacheKey, ReadStrategy.Immediate);

            if (currentPrices == null || !currentPrices.Any())
            {
                lock (lockObj)
                {
                    currentPrices = Cache.Service.Get<List<T>>(todayCacheKey, ReadStrategy.Immediate);
                    if (currentPrices == null || !currentPrices.Any())
                    {
                        var now = DateTime.UtcNow;
                        currentPrices = GetList().Where(x => DbFunctions.TruncateTime(x.CreatedDate) == now.Date)
                            .OrderByDescending(x => x.CreatedDate).ToList();

                        Cache.Service.Insert(todayCacheKey, currentPrices, new CacheEvictionPolicy(
                            GetCacheTime(),
                            CacheTimeoutType.Absolute,
                            new string[] { },
                            new[] { CacheMasterKey }
                        ));
                    }
                }
            }

            return currentPrices;
        }
    }
}