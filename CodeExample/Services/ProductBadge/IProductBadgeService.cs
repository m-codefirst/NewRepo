using EPiServer.Core;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Services.ProductBadge
{
    public interface IProductBadgeService
    {
        ContentReference GetBadgeForProduct(TrmVariant variant);
    }
}