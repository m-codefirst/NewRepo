using EPiServer.Core;
using EPiServer.Validation;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vattenfall.Domain.Core.Helpers;
using Vattenfall.Domain.Web.Blocks;

namespace Vattenfall.Domain.Core.Editor.Validations
{
    [ExcludeFromCodeCoverage]
    public class LightboxBlockValidation : IValidate<LightboxBlock>
    {
        public IEnumerable<ValidationError> Validate(LightboxBlock instance)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(instance?.LinkText))
                errors.Add(GenerateErrorMessage(PropertyNameHelper.GetName<LightboxBlock>(x => x.LinkText), true));

            if (instance.LightBoxStyle == Enums.Styles.LightBoxStyle.ImageAndLinkButton)
            {
                if (ContentReference.IsNullOrEmpty(instance?.ImageBlock?.Image))
                    errors.Add(GenerateErrorMessage(PropertyNameHelper.GetName<VattenfallImageBlock>(x => x.Image)));
                
                if (string.IsNullOrWhiteSpace(instance?.ImageBlock?.Alt))
                    errors.Add(GenerateErrorMessage(PropertyNameHelper.GetName<VattenfallImageBlock>(x => x.Alt)));
             
                if (string.IsNullOrWhiteSpace(instance?.ImageBlock?.Title))
                    errors.Add(GenerateErrorMessage(PropertyNameHelper.GetName<VattenfallImageBlock>(x => x.Title)));
            }

            return errors;
        }

        private ValidationError GenerateErrorMessage(string propertyName, bool isLinkText = false)
        {
            var defaultErrorMessage = $"The [{propertyName}] field must not be empty";
            var errorMessage = isLinkText ? defaultErrorMessage : $"{defaultErrorMessage} when LightBox style is Image with link button selected";
            return new ValidationError
            {
                PropertyName = propertyName,
                RelatedProperties = new[] { PropertyNameHelper.GetName<LightboxBlock>(x => x.LightBoxStyle) },
                ErrorMessage = errorMessage,
                Severity = ValidationErrorSeverity.Error
            };
        }
    }
}