using TRM.Shared.Extensions;
using Hephaestus.CMS.ViewModels;
using ILayoutModel = Hephaestus.CMS.ViewModels.ILayoutModel;
using TrmVariant = TRM.Web.Models.Catalog.TrmVariant;
using VariantViewModel = TRM.Web.Models.ViewModels.VariantViewModel;
using System.Linq;
using System.Text;
using TRM.ProductFeeds.Models;

namespace TRM.Web.Extentions
{
    public static class DataLayerExtensions
    {
        public static string GetImages(this IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model)
        {
            var isAnyImage = model?.ViewModel?.Images != null && model.ViewModel.Images.Any();
            var sbImages = new StringBuilder();
            if (isAnyImage)
            {
                foreach (var image in model.ViewModel.Images)
                {
                    sbImages.Append("\"");
                    sbImages.Append(image);
                    sbImages.Append("\"");
                    sbImages.Append(",");
                }
            }
            return sbImages.ToString();
        }
        public static string GetDescription(this IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model)
        {
            if (string.IsNullOrEmpty(model?.CurrentPage?.MetaDescription) &&
                (model?.CurrentPage?.Description == null || model.CurrentPage.Description.IsEmpty)) return string.Empty;

            var description = string.Empty;

            if (!string.IsNullOrEmpty(model?.CurrentPage?.MetaDescription))
                description = model.CurrentPage.MetaDescription;

            if (model?.CurrentPage?.Description != null && !model.CurrentPage.Description.IsEmpty)
                description = model.CurrentPage.Description.CleanXhtmlString().ToString();

            return description;
        }
        public static string GetItemCondition(this IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model)
        {
            if (model?.CurrentPage?.ConditionForProductFeed == null) return string.Empty;

            return model.CurrentPage.ConditionForProductFeed == Enumerations.GoogleProductFeedCondition.New ? "https://schema.org/NewCondition" : 
                model.CurrentPage.ConditionForProductFeed == Enumerations.GoogleProductFeedCondition.Refurbished ? "https://schema.org/RefurbishedCondition" : "https://schema.org/UsedCondition";
        }
        public static string GetIsInStock(this IPageViewModel<TrmVariant, ILayoutModel, VariantViewModel> model)
        {
            if (model?.CurrentPage?.AvailibilityForProductFeed == null) return string.Empty;

            return model.CurrentPage.AvailibilityForProductFeed == Enumerations.GoogleProductFeedStockAvailability.Instock ? "https://schema.org/InStock" : 
                model.CurrentPage.AvailibilityForProductFeed == Enumerations.GoogleProductFeedStockAvailability.PreOrder ? "https://schema.org/PreOrder" : "https://schema.org/OutOfStock";
        }
    }
}