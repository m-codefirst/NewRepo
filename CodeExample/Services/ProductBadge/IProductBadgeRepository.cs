using System.Collections.Generic;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Services.ProductBadge
{
    public interface IProductBadgeRepository
    {
        IEnumerable<TrmCategoryBase> GetAllCategoriesWithBadge();
    }
}