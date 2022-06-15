
using System;
using EPiServer.Core;
using System.Collections.Generic;
using System.Linq;
using Vattenfall.Domain.Core.Constants;
using Vattenfall.Domain.Core.Editor.Validations.PageLayoutValidations;
using Vattenfall.Domain.Web.Pages;

namespace Vattenfall.Domain.Web.Services.Analytics
{
    public class PageLayoutValidationProvider : IPageLayoutValidationProvider
    {
        private readonly IEnumerable<IPageLayoutValidator> _pageSpecificValidators;

        public PageLayoutValidationProvider(IEnumerable<IPageLayoutValidator> pageSpecificValidators)
        {
            _pageSpecificValidators = pageSpecificValidators;
        }

        public PageLayoutValidationResult Validate(IContent content)
        {
            //no need to validate
            if (!(content is IPageLayout pageLayout)) return new PageLayoutValidationResult(true);
            var validator = _pageSpecificValidators.FirstOrDefault(v => v.ValidatorForPageLayoutStyle == pageLayout.PageLayoutStyle);

            //no need for validation
            if (validator == null) return new PageLayoutValidationResult(true);

            //validate
            return validator.Validate(pageLayout);
        }
    }
}
