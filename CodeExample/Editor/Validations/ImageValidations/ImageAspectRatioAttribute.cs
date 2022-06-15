using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Vattenfall.Domain.Core.Media;

namespace Vattenfall.Domain.Core.Editor.Validations.ImageValidations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ImageAspectRatioAttribute : ValidationAttribute
    {
        public double[] AllowedRatios { get; set; }

        public ImageAspectRatioAttribute(params double[] allowedRatios)
        {
            AllowedRatios = allowedRatios;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var contentLink = value as ContentReference;

            // Allow null
            if (contentLink == null)
            {
                return ValidationResult.Success;
            }

            // No allowed aspect ratios defined
            if (!AllowedRatios.Any())
            {
                return ValidationResult.Success;
            }

            var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

            // Could not get the content, an image is required.
            if (!contentLoader.TryGet(contentLink, out VattenfallBaseImage image))
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }

            var aspectRatio = CalculateAspectRatio(image);

            // We can't calculate the aspect ration, give the editor the benefit of the doubt
            if (!aspectRatio.HasValue)
            {
                return ValidationResult.Success;
            }

            // The image is within acceptable tolerances of an allowed aspect ratio
            if (AllowedRatios.Any(x => Math.Abs(x - aspectRatio.Value) < 0.01))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        private static double? CalculateAspectRatio(VattenfallBaseImage image)
        {
            if (!image.Width.HasValue || image.Width == 0 || !image.Height.HasValue || image.Height == 0)
            {
                return null;
            }

            return (double)image.Width.Value / image.Height.Value;
        }

        public override string FormatErrorMessage(string name)
        {
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                ErrorMessage = "{0} must be an image and meet aspect ratio criteria.";
            }

            return string.Format(CultureInfo.InvariantCulture, ErrorMessageString, name);
        }
    }
}