using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Logging;
using TRM.Web.Models.Catalog;
using StringExtensions = EPiServer.Find.Helpers.Text.StringExtensions;

namespace TRM.Web.Services.ProductBadge
{
    public class ProductBadgeRepository : IProductBadgeRepository
    {
        protected static readonly ILogger Logger = LogManager.GetLogger(typeof(ProductBadgeRepository));
        private readonly IClient findClient;

        public ProductBadgeRepository(IClient findClient)
        {
            this.findClient = findClient;
        }
 
        public IEnumerable<TrmCategoryBase> GetAllCategoriesWithBadge()
        {
            var search = findClient.Search<TrmCategoryBase>();

            search = search
                .ExcludeDeleted()
                .FilterOnReadAccess()
                .Filter(x => x.ProductBadgeContentReference.Exists());

            try
            {
                var results = search.Take(1000).GetContentResult();

                var withoutNulls = results
                    .Where(x => !StringExtensions.IsNullOrWhiteSpace(x.ProductBadgeContentReference));

                return withoutNulls;
            }
            catch (Exception e)
            {
                Logger.Error($"Cannot load data for {nameof(ProductBadgeRepository)}", e);
                return Enumerable.Empty<TrmCategoryBase>();
            }
        }
    }
}