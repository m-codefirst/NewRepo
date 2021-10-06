using System.Linq;
using EPiServer.Core;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Services.ProductBadge
{
    public class ProductBadgeService : IProductBadgeService
    {
        private readonly IProductBadgeRepository productBadgeRepository;

        public ProductBadgeService(IProductBadgeRepository productBadgeRepository)
        {
            this.productBadgeRepository = productBadgeRepository;
        }

        public ContentReference GetBadgeForProduct(TrmVariant variant)
        {
            var categoriesWithBadges = productBadgeRepository.GetAllCategoriesWithBadge();

            var categoryWithBadge = categoriesWithBadges
                .FirstOrDefault(x => variant.AllCategoryIds.Contains(x.ContentLink.ID));

            return categoryWithBadge?.ProductBadge;
        }
    }
}