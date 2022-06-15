using EPiServer.Core;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EPiServer.SpecializedProperties;

namespace Vattenfall.Domain.Core.Editor.Validations
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class MinElementItemsAttribute : ValidationAttribute
    {
        private readonly int _min;

        public MinElementItemsAttribute(int min)
        {
            this._min = min;
        }

        public override bool IsValid(object value)
        {
            if ((value != null) && !(value is ContentArea || value is LinkItemCollection))
            {
                throw new ValidationException("ContentAreaMinItemsAttribute is intended only for use with ContentArea or LinkItem Collection properties");
            }

            switch (value)
            {
                case LinkItemCollection linkItemCollection:
                    {
                        if (linkItemCollection.Count < _min)
                        {
                            ErrorMessage = $"is restricted to a minimum of {_min} item{(_min.Equals(1) ? String.Empty : "s")}";
                            return false;
                        }

                        break;
                    }
                case ContentArea contentArea:
                    {
                        // Get all items or none if null
                        var allItems = contentArea?.Items ?? Enumerable.Empty<ContentAreaItem>();

                        // Count the unique personalisation group names, replacing empty ones (items which aren't personalised) with a unique name
                        var i = 0;
                        var minNumberOfItemsShown = allItems.Select(x => string.IsNullOrEmpty(x.ContentGroup) ? (i++).ToString() : x.ContentGroup).Distinct().Count();

                        if (minNumberOfItemsShown < _min)
                        {
                            ErrorMessage = $"is restricted to a minimum of {_min} item{(_min.Equals(1) ? String.Empty : "s")}";
                            return false;
                        }

                        break;
                    }
            }


            return true;
        }

        protected override System.ComponentModel.DataAnnotations.ValidationResult IsValid(object value, System.ComponentModel.DataAnnotations.ValidationContext validationContext)
        {
            var result = base.IsValid(value, validationContext);
            if (result != null && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                result.ErrorMessage = string.Format("Field '{0}' {1}", validationContext.DisplayName, ErrorMessage);
            }
            return result;
        }
    }
}
