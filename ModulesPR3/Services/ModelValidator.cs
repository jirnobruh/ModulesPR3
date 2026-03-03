using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ModulesPR3.Services
{
    public static class ModelValidator
    {
        public static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);

            Validator.TryValidateObject(model, context, results, true);

            return results;
        }
    }

}