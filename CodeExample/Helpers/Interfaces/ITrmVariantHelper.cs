using EPiServer.Core;
using Hephaestus.CMS.ViewModels;
using System.Collections.Generic;
using System.Web;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers.Interfaces
{
    public interface ITrmVariantHelper
    {
        IPageViewModel<TrmVariant, ILayoutModel, EntryPartialViewModel> CreatePageViewModel(
            IPageViewModel<TrmVariant, ILayoutModel, EntryPartialViewModel> model, TrmVariant trmVariant);

        VariantCarouselBlockViewModel CreatePageViewModel(VariantCarouselBlock currentBlock);

        IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> CreatePageViewModel(
            IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model,
            TrmVariant trmVariant, VariantViewModel viewModel, StartPage startPage, HttpRequestBase request);

        void ModifyLayout(ILayoutModel layoutModel, IContent catalogContent);

        QualityStandardDto UpdateQualityStandard(string quality, string pureMetalType);

        Dictionary<string, int> ImageDispalySizes(int lg, int md, int sm, int xs);
    }
}