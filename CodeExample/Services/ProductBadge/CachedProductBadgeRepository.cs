using System;
using System.Collections.Generic;
using EPiServer.Framework.Cache;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Services.ProductBadge
{
    public class CachedProductBadgeRepository : IProductBadgeRepository
    {
        private readonly IProductBadgeRepository productBadgeRepository;
        private const string cacheKey = "CachedProductBadgeRepository";

        public CachedProductBadgeRepository(IProductBadgeRepository productBadgeRepository)
        {
            this.productBadgeRepository = productBadgeRepository;
        }
        
        public IEnumerable<TrmCategoryBase> GetAllCategoriesWithBadge()
        {
            var fromCache  = EPiServer.CacheManager.Get(cacheKey);
            if (fromCache != null)
            {
                return (IEnumerable<TrmCategoryBase>) fromCache;
            }

            var fromRepository = this.productBadgeRepository.GetAllCategoriesWithBadge();

            EPiServer.CacheManager.Insert(cacheKey, fromRepository, new CacheEvictionPolicy(TimeSpan.FromHours(24), CacheTimeoutType.Sliding));

            return fromRepository;
        }

        public static void InvalidateCache()
        {
            EPiServer.CacheManager.Remove(cacheKey);
        }
    }
}