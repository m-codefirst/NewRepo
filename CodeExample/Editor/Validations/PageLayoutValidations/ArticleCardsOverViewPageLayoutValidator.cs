using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Vattenfall.Domain.Core.Editor.Enums;
using Vattenfall.Domain.Web.Blocks;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations
{
    public class ArticleCardsOverViewPageLayoutValidator : IPageLayoutValidator
    {
        public Styles.PageLayoutStyle ValidatorForPageLayoutStyle => Styles.PageLayoutStyle.ArticleCardsOverview;

        public PageLayoutValidationResult Validate(IPageLayout page)
        {
            if (page == null) return new PageLayoutValidationResult(false, "Page is null");
            if (page.PageLayoutStyle != Styles.PageLayoutStyle.ArticleCardsOverview) return new PageLayoutValidationResult(true);
            if (page.LayoutBlocks == null || !page.LayoutBlocks.FilteredItems.Any())
            {
                return new PageLayoutValidationResult(false, "Primary building blocks have not been added. The Article Cards Overview blocks is required");
            }

            if (page.LayoutBlocks.FilteredItems.Count() != 2)
            {
                return new PageLayoutValidationResult(false, "The Hero Inform Simple Block and Article Cards Overview Block are required.");
            }

            return new PageLayoutValidationResult(true);
        }
    }
}
