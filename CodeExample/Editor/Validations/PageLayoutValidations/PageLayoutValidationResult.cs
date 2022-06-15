using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations
{
    [ExcludeFromCodeCoverage]
    public class PageLayoutValidationResult
    {
        public PageLayoutValidationResult(bool validated, string message)
        {
            Validated = validated;
            Message = message;
        }

        public PageLayoutValidationResult(bool validated)
        {
            Validated = validated;
            Message = string.Empty;
        }

        public bool Validated { get; }
        public string Message { get; }
    }
}
