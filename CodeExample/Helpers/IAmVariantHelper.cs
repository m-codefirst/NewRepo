using System.Web;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;

namespace TRM.Web.Helpers
{
    public interface IAmVariantHelper
    {
        NodeContent GetPrimaryCategory(VariationContent variant);
        Url GetCanonicalUrl(VariationContent variant, HttpRequestBase request);
    }
}