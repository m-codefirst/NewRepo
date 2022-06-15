using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EPiServer.Validation;
using Vattenfall.Domain.Web.Blocks;

namespace NUON.Business
{
    public class UspBlockValidator : IValidate<UspBlock>
    {
        public IEnumerable<ValidationError> Validate(UspBlock uspBlock)
        {
            if(uspBlock.Description != null)
            {
                // remove html tags like <strong> etc
                string content = Regex.Replace(uspBlock.Description.ToString(), "<.*?>", String.Empty);

                if (!string.IsNullOrEmpty(content) && (content.Length < 90 || content.Length > 270))
                {
                    return new[]
                    {
                        new ValidationError()
                        {
                            ErrorMessage = "The bodytext is " + content.Length +
                                           " characters and should be empty or between 90 and 270 characters",
                            PropertyName = "Bodytext",
                            Severity = ValidationErrorSeverity.Error,
                            ValidationType = ValidationErrorType.Unspecified
                        }
                    };
                }
            }

            return new ValidationError[0];
        }
    }

    public class UspSingleBlockValidator : IValidate<UspSingle>
    {
        public IEnumerable<ValidationError> Validate(UspSingle uspBlock)
        {
            var content = uspBlock.Description;

            if(string.IsNullOrEmpty(content))
            {
                return new[]
                {
                    new ValidationError()
                    {
                        ErrorMessage = "The Description is required",
                        PropertyName = "Description",
                        Severity = ValidationErrorSeverity.Error,
                        ValidationType = ValidationErrorType.Unspecified
                    }
                };
            }

            if ((content.Length < 10 || content.Length > 75))
            {
                return new[] { new ValidationError() {
                    ErrorMessage = "The description is " + content.Length + " characters and should be between 10 and 75 characters",
                    PropertyName = "Description",
                    Severity = ValidationErrorSeverity.Error,
                    ValidationType = ValidationErrorType.Unspecified
                } };
            }

            return new ValidationError[0];
        }
    }
}