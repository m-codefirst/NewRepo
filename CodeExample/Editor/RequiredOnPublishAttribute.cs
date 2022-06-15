using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Vattenfall.Domain.Core.Editor
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequiredOnPublishAttribute : ValidationAttribute
    {
        private string _errorMessage = "The {0} {1} must not be empty";
        private const string _field = "field";
        private const string _fields = "fields";
        public string[] Properties { get; set; }
        public RequiredOnPublishAttribute() : base() { }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return new ValidationResult(string.Format(_errorMessage, string.Join(", ", Properties), IdentifyFieldOrFields(Properties)));

            var failedProperties = new List<string>();
            foreach (var property in Properties)
                if (!ValidateProperty(value, property)) failedProperties.Add(property);

            if (failedProperties != null && failedProperties.Any())
                return new ValidationResult(string.Format(_errorMessage, string.Join(", ", failedProperties), IdentifyFieldOrFields(failedProperties.ToArray())));

            return ValidationResult.Success;
        }

        private bool ValidateProperty(object value, string propertyName)
        {
            var result = value?.GetType()?
                .GetProperties()?
                .SingleOrDefault(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?
                .GetValue(value, null);

            if (string.IsNullOrEmpty(result?.ToString())) return false;

            return true;
        }

        private string IdentifyFieldOrFields(string[] properties)
        {
            if (properties != null && properties.Count() > 1) return _fields;

            return _field;
        }
    }
}