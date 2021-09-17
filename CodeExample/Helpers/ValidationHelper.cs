using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TRM.Web.Helpers
{
    public class ValidationHelper : IAmValidationHelper
    {

        public bool IsValid<T>(T model, out string resultMsg)
        {

            var context = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(model, context, validationResults);
            resultMsg = string.Empty;
            if (isValid) return true;

            foreach (var validationResult in validationResults)
            {
                resultMsg += validationResult.ErrorMessage + "/r/n";

            }

            return false;

        }

        public bool IsValid<T>(T model)
        {
            string resultMsg;
            return IsValid(model, out resultMsg);
        }

    }
}