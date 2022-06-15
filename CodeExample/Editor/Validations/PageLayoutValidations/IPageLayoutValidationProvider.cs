using EPiServer.Core;

namespace Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations
{
    public interface IPageLayoutValidationProvider
    {
        PageLayoutValidationResult Validate(IContent content);
    }
}
