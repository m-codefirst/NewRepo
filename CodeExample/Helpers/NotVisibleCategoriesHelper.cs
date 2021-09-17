using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using TRM.Web.Extentions;
using TRM.Web.Helpers.Find;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.Find;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public class NotVisibleCategoriesHelper : INotVisibleCategoriesHelper
    {
        private readonly IContentLoader _contentLoader;
        
        readonly TrmFacetBlock categoriesStringFacet = new TrmFacetBlock
        { Name = "CategoriesString", Term = "CategoriesString", Description = "", ViewAllLink = "" };

        public NotVisibleCategoriesHelper(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public void AddHelperCategoriesStringFacet(List<IAddCommerceSearchFacets> variantFacets)
        {
            variantFacets.Add(categoriesStringFacet);
        }

        public void CleanupAllCategoriesIdsFacet(CategoryViewModel viewModel)
        {
            viewModel.Filters.EntryFacets = viewModel.Filters.EntryFacets.Where(x => x.Name != categoriesStringFacet.Name).ToList();
        }

        public List<string> GetCategoriesNotVisibleInMenu(FindResults<IAmCommerceSearchable> variantResults)
        {
            var categoriesStringSearchFacet = variantResults.Facets.FirstOrDefault(x => x.Value == categoriesStringFacet.Name);
            var categories =
                categoriesStringSearchFacet?.Terms.SelectMany(x => x.Term.Split('|')).Distinct()
                    .Select(contentId =>
                    {
                        _contentLoader.TryGet<TrmCategoryBase>(new ContentReference(int.Parse(contentId), "CatalogContent"),
                               out TrmCategoryBase content);
                        return content;
                    }).Where(x => x != null) ??
                Enumerable.Empty<TrmCategoryBase>();
            var toExclude = categories.Where(x => !x.VisibleInLeftMenu).Select(x => x.DisplayName).ToList();
            var encoded = toExclude.Select(StringExtensions.EncodeValue).ToList();

            return encoded;
        }
    }
}