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
    public class MaxElementItemsAttribute : ValidationAttribute
    {
        private readonly int _max;

        public MaxElementItemsAttribute(int max)
        {
            this._max = max;
        }

        public override bool IsValid(object value)
        {
            if ((value != null) && !(value is ContentArea || value is LinkItemCollection))
            {
                throw new ValidationException("ContentAreaMaxItemsAttribute is intended only for use with ContentArea or LinkItem Collection properties");
            }

            switch (value)
            {
                case LinkItemCollection linkItemCollection:
                    {
                        if (linkItemCollection.Count > _max)
                        {
                            ErrorMessage = $"is restricted to a maximum of {_max} item{(_max.Equals(1) ? String.Empty : "s")}";
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
                        var maxNumberOfItemsShown = allItems.Select(x => string.IsNullOrEmpty(x.ContentGroup) ? (i++).ToString() : x.ContentGroup).Distinct().Count();

                        if (maxNumberOfItemsShown > _max)
                        {
                            ErrorMessage = $"is restricted to a maximum of {_max} item{(_max.Equals(1) ? String.Empty : "s")}";
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
