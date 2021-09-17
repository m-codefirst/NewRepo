using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;

namespace Hephaestus.Commerce.Product.ProductService
{
    public class HephaestusReferenceConverter : IAmReferenceConverter
    {
        public ContentReference GetContentLink(string code, CatalogContentType contentType)
        {
            return ServiceLocator.Current.GetInstance<ReferenceConverter>().GetContentLink(code, contentType);
        }

        public ContentReference GetContentLink(int id, CatalogContentType contentType, int versionId)
        {
            return ServiceLocator.Current.GetInstance<ReferenceConverter>().GetContentLink(id, contentType, versionId);
        }

        public ContentReference GetContentLink(string catalogEntryId)
        {
            return ServiceLocator.Current.GetInstance<ReferenceConverter>().GetContentLink(catalogEntryId);
        }

        public ContentReference GetRootLink()
        {
            return ServiceLocator.Current.GetInstance<ReferenceConverter>().GetRootLink();
        }

        public int GetObjectId(ContentReference contentLink)
        {
            return ServiceLocator.Current.GetInstance<ReferenceConverter>().GetObjectId(contentLink);
        }

        //public IEnumerable<ContentReference> GetContentReferences(EntryContentBase entries)
        //{
        //    return entries.Entry.Select(entry => GetContentLink(entry.CatalogEntryId, CatalogContentType.CatalogEntry, 0));
        //}
    }



}
