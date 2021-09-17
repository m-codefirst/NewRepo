using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;

namespace Hephaestus.Commerce.Product.ProductMetaField.Service
{
    public interface IProductMetaFieldService
    {
        PropertyDataCollection ProductMetaField(EntryContentBase model);

        PropertyDataCollection ProductCompariableProperties(PropertyDataCollection propertyCollection, int metaClassId);

        PropertyDataCollection ProductProperties(PropertyDataCollection propertyCollection, int metaClassId);
    }
}
