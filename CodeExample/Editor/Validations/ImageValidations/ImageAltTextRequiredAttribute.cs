using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Vattenfall.Domain.Core.Media;

namespace Vattenfall.Domain.Core.Editor.Validations.ImageValidations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ImageAltTextRequiredAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var contentLink = value as ContentReference;

            // Allow null
            if (contentLink == null)
            {
                return ValidationResult.Success;
            }

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            // Could not get the content, an image is required.
            if (!contentLoader.TryGet(contentLink, out VattenfallBaseImage image))
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }

            if (!string.IsNullOrEmpty(image.AltText))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        public override string FormatErrorMessage(string name)
        {
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = "{0} must be an image and Alt Text is required.";
            }

            return string.Format(CultureInfo.InvariantCulture, ErrorMessageString, name);
        }
    }
}