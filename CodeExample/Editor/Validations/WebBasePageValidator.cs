using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EPiServer.Validation;
using Vattenfall.Domain.Web.Pages;

namespace NUON.Business
{
    [ExcludeFromCodeCoverage]
    public class WebBasePageValidator : IValidate<WebBasePage>
    {
        IEnumerable<ValidationError> IValidate<WebBasePage>.Validate(WebBasePage currentPage)
        {
            if (currentPage == null)
                return new ValidationError[0];

            if (currentPage.MetaTitle != null && (currentPage.MetaTitle.Length < 40 || currentPage.MetaTitle.Length > 51))
            {
                return new[] { new ValidationError() {
                    ErrorMessage = "The meta title is " + currentPage.MetaTitle.Length + " characters and should be between 40 and 51 characters",
                    PropertyName = "MetaTitle",
                    Severity = ValidationErrorSeverity.Info,
                    ValidationType = ValidationErrorType.Unspecified
                } };
            }
            if (currentPage.MetaDescription != null && (currentPage.MetaDescription.Length < 100 || currentPage.MetaDescription.Length > 127))
            {
                return new[] { new ValidationError() {
                    ErrorMessage = "The meta description is " + currentPage.MetaDescription.Length + " characters and should be between 100 and 127 characters",
                    PropertyName = "MetaDescription",
                    Severity = ValidationErrorSeverity.Info,
                    ValidationType = ValidationErrorType.Unspecified
                } };
            }

            return new ValidationError[0];
        }
    }
}