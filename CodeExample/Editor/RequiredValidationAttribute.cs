using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Vattenfall.Domain.Core.Editor
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequiredValidationAttribute : ValidationAttribute
    {
        public const string DefaultErrorMessage = "The {0} field must not be empty";
        public RequiredValidationAttribute() : base(DefaultErrorMessage) { }
        public override bool IsValid(object value)
        {
            return !string.IsNullOrEmpty(value?.ToString());
        }
    }
}
