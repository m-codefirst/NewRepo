using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Vattenfall.Domain.Web.Blocks;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Core.Editor
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TitleValidationForUrlAttribute : ValidationAttribute
    {
        private const string ErrorMessage = "Title or Header not found with the given URL please enter manually.";
        public TitleValidationForUrlAttribute() { }

        protected override ValidationResult IsValid(object value, ValidationContext validateionContext)
        {

            IContent contentData = null;
            if (value != null)
            {
                var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
                contentData = urlResolver.Route(new UrlBuilder((Url)value));
            }

            var block = validateionContext.ObjectInstance as MoreInfoBlock;
            if (block != null)
            {
                if (!string.IsNullOrEmpty(block.Title)) return ValidationResult.Success;

                var pageData = contentData as WebBasePage;
                if (pageData != null && !string.IsNullOrEmpty(pageData.Header))
                {
                    block.Title = contentData != null ? pageData.Header : string.Empty;
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult(ErrorMessage);
        }
    }
}