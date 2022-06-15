using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EPiServer.ServiceLocation;
using EPiServer.Validation;
using Vattenfall.Domain.Core.Editor.Enums;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations
{
    [ExcludeFromCodeCoverage]
    public class GenericPageLayoutValidation : IValidate<GenericPage>
    {
        public IEnumerable<ValidationError> Validate(GenericPage instance)
        {
            if (instance.PageLayoutStyle == Styles.PageLayoutStyle.SelectLayout)
            {
                return new[]
                {
                    new ValidationError()
                    {
                        Severity = ValidationErrorSeverity.Error,
                        ErrorMessage = "Select a page layout",
                        ValidationType = ValidationErrorType.Unspecified,
                        PropertyName = "LayoutBlocks"
                    }
                };
            }

            var validationProvider = ServiceLocator.Current.GetInstance<IPageLayoutValidationProvider>();
            var result = validationProvider.Validate(instance);
            if (!result.Validated)
            {
                return new[]
                {
                    new ValidationError()
                    {
                        Severity = ValidationErrorSeverity.Error,
                        ErrorMessage = result.Message,
                        ValidationType = ValidationErrorType.Unspecified,
                        PropertyName = "LayoutBlocks"
                    }
                };
            }

            return Enumerable.Empty<ValidationError>();
        }
    }
}
