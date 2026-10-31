using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace SakilaRentalApp.Utils
{
    /// <summary>
    /// Model Binder personalizado que normaliza el parseo de números decimales,
    /// permitiendo tanto punto (.) como coma (,) sin importar la cultura del cliente o servidor.
    /// Evita errores de ModelState inválido al enviar datos desde inputs HTML5 type='number'.
    /// </summary>
    public class InvariantDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
            string? value = valueProviderResult.FirstValue;

            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.CompletedTask;
            }

            // Normalizar reemplazando comas por puntos
            string normalizedValue = value.Trim().Replace(',', '.');

            if (decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"El valor '{valueProviderResult.FirstValue}' no es un número decimal válido.");
            }

            return Task.CompletedTask;
        }
    }

    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?))
            {
                return new InvariantDecimalModelBinder();
            }
            return null;
        }
    }
}
