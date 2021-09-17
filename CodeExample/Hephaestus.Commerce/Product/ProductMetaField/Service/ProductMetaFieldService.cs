using System.Linq;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.MetaDataPlus.Configurator;

namespace Hephaestus.Commerce.Product.ProductMetaField.Service
{
    [ServiceConfiguration(typeof(IProductMetaFieldService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ProductMetaFieldService : IProductMetaFieldService
    {
        private const string IsUsedForCompare = "useincomparing";
        private const string ManufacturerLinkName = "manufacturer link";
        public PropertyDataCollection ProductMetaField(EntryContentBase model)
        {
            return model.Property;
        }

        public PropertyDataCollection ProductCompariableProperties(PropertyDataCollection propertyCollection, int metaClassId)
        {
            var resultCollection = new PropertyDataCollection();
            var metaClass = MetaClass.Load(CatalogContext.MetaDataContext, metaClassId);
            var metaFields = metaClass.GetAllMetaFields();

            foreach (var field in metaFields.Where(
                        mf => mf.Attributes.Count > 0 && mf.Attributes[IsUsedForCompare].ToLower() == "true").OrderBy(o=> o.Name))
            {
                
                var property =
                        propertyCollection.FirstOrDefault(
                            p =>
                                p != null && !p.IsNull && !string.IsNullOrEmpty(p.Name) && p.Value != null &&
                                !string.IsNullOrEmpty(p.Value.ToString()) && p.Name == field.Name);

                if (property != null)
                {
                    resultCollection.Add(property);
                }
            }

            return resultCollection;
        }

        public PropertyDataCollection ProductProperties(PropertyDataCollection propertyCollection, int metaClassId)
        {
            var resultCollection = new PropertyDataCollection();
            var metaClass = MetaClass.Load(CatalogContext.MetaDataContext, metaClassId);
            var metaFields = metaClass.GetAllMetaFields().ToList();

            var manufacturerLink = metaFields.FirstOrDefault(x => x.FriendlyName.ToLower().Equals(ManufacturerLinkName));

            foreach (var field in metaFields.Where(
                        mf => mf.Attributes.Count > 0).OrderBy(o => o.Name))
            {

                var property =
                        propertyCollection.FirstOrDefault(
                            p =>
                                p != null && !p.IsNull && !string.IsNullOrEmpty(p.Name) && p.Value != null &&
                                !string.IsNullOrEmpty(p.Value.ToString()) && p.Name == field.Name);

                if (property != null)
                {
                    resultCollection.Add(property);
                }
            }

            if (manufacturerLink != null)
            {
                resultCollection.Add(propertyCollection.FirstOrDefault(p => p.Name == manufacturerLink.Name));
            }

            return resultCollection;
        }
    }
}
