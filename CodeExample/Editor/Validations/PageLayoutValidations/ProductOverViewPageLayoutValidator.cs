using System.Linq;
using EPiServer.Core;
using Vattenfall.Domain.Core.Editor.Enums;
using Vattenfall.Domain.Web.Blocks;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations
{
    public class ProductOverViewPageLayoutValidator : IPageLayoutValidator
    {
        public Styles.PageLayoutStyle ValidatorForPageLayoutStyle => Styles.PageLayoutStyle.ProductOverview;
        public PageLayoutValidationResult Validate(IPageLayout page)
        {
            if (page == null) return new PageLayoutValidationResult(false, "Page is null");
            if (page.PageLayoutStyle != Styles.PageLayoutStyle.ProductOverview) return new PageLayoutValidationResult(true);
            if (page.LayoutBlocks == null || !page.LayoutBlocks.FilteredItems.Any())
            {
                return new PageLayoutValidationResult(false, "Primary building blocks have not been added. The Hero Inspire and Product Tiles Overview blocks are required");
            }

            if (page.LayoutBlocks.FilteredItems.Count() != 2)
            {
                return new PageLayoutValidationResult(false, "The Hero Inspire and Product Tiles Overview block are only required.");
            }

            return new PageLayoutValidationResult(true);
        }
    }
}
