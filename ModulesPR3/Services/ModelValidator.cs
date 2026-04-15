using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ModulesPR3.Services
{
    public static class ModelValidator
    {
        /// <summary>
        /// Проверяет объект модели по правилам DataAnnotations и возвращает найденные ошибки валидации.
        /// </summary>
        /// <param name="model">Экземпляр модели, для которой выполняется проверка.</param>
        /// <returns>Список ошибок валидации; пустой список, если ошибок нет.</returns>
        public static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);

            Validator.TryValidateObject(model, context, results, true);

            return results;
        }
    }

}
