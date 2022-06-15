using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EPiServer.Core;
using EPiServer.Validation;
using Vattenfall.Domain.Core.Helpers;
using Vattenfall.Domain.Web.Blocks;
using static Vattenfall.Domain.Core.Editor.Enums.Styles;

namespace Vattenfall.Domain.Core.Editor.Validations
{
    [ExcludeFromCodeCoverage]
    public class CallToActionZipBlockValidation : IValidate<CallToActionZipBlock>
    {
        public IEnumerable<ValidationError> Validate(CallToActionZipBlock instance)
        {
            var errors = new List<ValidationError>();

            if (instance.ZipCodeStyle == ZipCodeStyle.Input)
            {
                if (string.IsNullOrEmpty(instance.CtaButtonText))
                    errors.Add(GenerateErrorMessage(PropertyNameHelper.GetName<CallToActionZipBlock>(x => x.CtaButtonText), "text"));

                if (ContentReference.IsNullOrEmpty(instance.FormActionUrl))
                    errors.Add(GenerateErrorMessage(PropertyNameHelper.GetName<CallToActionZipBlock>(x => x.FormActionUrl), "link"));

            }
            return errors;
        }

        private ValidationError GenerateErrorMessage(string propertyName, string linkType)
        {
            return new ValidationError
            {
                PropertyName = propertyName,
                RelatedProperties = new[] { PropertyNameHelper.GetName<CallToActionZipBlock>(x => x.ZipCodeStyle) },
                ErrorMessage = $"The Primary CTA button {linkType} field must not be empty when CTA Block Type is [Input]",
                Severity = ValidationErrorSeverity.Error
            };
        }
    }
}