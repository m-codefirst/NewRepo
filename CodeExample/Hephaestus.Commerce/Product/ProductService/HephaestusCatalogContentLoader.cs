using System.Collections.Generic;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Provider;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Objects;

namespace Hephaestus.Commerce.Product.ProductService
{
    public class HephaestusCatalogContentLoader : IAmCatalogContentLoader
    {
        //public IList<T> GetItems<T>(IList<ContentReference> contentLinks, string language) where T : CatalogContentBase
        //{
        //    return ServiceLocator.Current.GetInstance<CatalogContentLoader>().GetItems<T>(contentLinks, language);
        //}

        //public CatalogEntryDto GetEntryByCode(string code)
        //{
        //    //return ServiceLocator.Current.GetInstance<ICatalogSystem>().Load(code);
        //    return ServiceLocator.Current.GetInstance<ICatalogSystem>().GetCatalogEntryDto(code);
        //}
    }
}
