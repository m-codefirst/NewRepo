using Hephaestus.CMS.ViewModels;
using System.Web;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers.Interfaces
{
    public interface ITrmCategoryHelper
    {
        IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> CreateViewModelForCategoryLandingPage(
            IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> model,
            TrmCategory trmCategory, HttpRequestBase request);

        IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> CreateViewModelForCategoryListingPage(
            IPageViewModel<TrmCategory, ILayoutModel, CategoryViewModel> model,
            TrmCategory trmCategory,
            CategoryViewModel viewModel, HttpRequestBase request);
    }
}