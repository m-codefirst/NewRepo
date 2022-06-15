using Vattenfall.Domain.Core.Editor.Enums;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations
{
    public interface IPageLayoutValidator
    {
        Styles.PageLayoutStyle ValidatorForPageLayoutStyle { get; }
        PageLayoutValidationResult Validate(IPageLayout page);
    }
}
