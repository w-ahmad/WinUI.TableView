using Microsoft.UI.Xaml.Data;
using System;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides helper methods for applying value converters to data bindings.
/// </summary>
internal static class BindingHelper
{
    /// <summary>
    /// Applies the specified binding's value converter to a given value and returns the converted result.
    /// </summary>
    /// <remarks>If the binding does not specify a converter, the method returns the input value
    /// unchanged.</remarks>
    /// <param name="Binding">The binding whose converter will be used to convert the value. Can be null if no conversion is required.</param>
    /// <param name="value">The value to be converted using the binding's converter.</param>
    /// <param name="targetType">The target type to convert the value to. If null, defaults to object.</param>
    /// <returns>The converted value if a converter is present; otherwise, the original value.</returns>
    public static object? ApplyConverter(Binding? Binding, object? value, Type? targetType = null)
    {
        targetType ??= typeof(object);

        if (Binding?.Converter is { } converter)
        {
            value = converter.Convert(
                value,
                targetType,
                Binding.ConverterParameter,
                Binding.ConverterLanguage);
        }

        return value;
    }
}