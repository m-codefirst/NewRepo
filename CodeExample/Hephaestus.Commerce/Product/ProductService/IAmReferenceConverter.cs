using EPiServer.Core;
using Mediachase.Commerce.Catalog;

namespace Hephaestus.Commerce.Product.ProductService
{
    public interface IAmReferenceConverter
    {
        ContentReference GetContentLink(string code, CatalogContentType type);
        ContentReference GetContentLink(int id, CatalogContentType contentType, int versionId);
        ContentReference GetContentLink(string catalogEntryId);
        ContentReference GetRootLink();

        int GetObjectId(ContentReference contentLink);
        //IEnumerable<ContentReference> GetContentReferences(Entries entries);
    }
}
