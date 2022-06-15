using EPiServer.Core;
using EPiServer.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vattenfall.Domain.Web.Blocks;

namespace Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations
{
    [ExcludeFromCodeCoverage]
    public class ProductTileValidation : IValidate<ProductTileBlock>
    {
        public IEnumerable<ValidationError> Validate(ProductTileBlock instance)
        {
            CategoryList categoryList = (instance as ICategorizable).Category;
            if (categoryList == null || categoryList.Count == 0)
            {
                return new[]
                {
                    new ValidationError()
                    {
                        Severity = ValidationErrorSeverity.Error,
                        ErrorMessage = "Category is a required field",
                        ValidationType = ValidationErrorType.Unspecified,
                        PropertyName = "Category"
                    }
                };
            }
            return Enumerable.Empty<ValidationError>();
        }
    }
}
