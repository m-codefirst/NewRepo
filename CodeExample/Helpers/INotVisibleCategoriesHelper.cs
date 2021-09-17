using System.Collections.Generic;
using TRM.Web.Helpers.Find;
using TRM.Web.Models.Find;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public interface INotVisibleCategoriesHelper
    {
        void AddHelperCategoriesStringFacet(List<IAddCommerceSearchFacets> variantFacets);
        void CleanupAllCategoriesIdsFacet(CategoryViewModel viewModel);
        List<string> GetCategoriesNotVisibleInMenu(FindResults<IAmCommerceSearchable> variantResults);
    }
}