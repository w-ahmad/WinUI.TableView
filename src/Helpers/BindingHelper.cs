using Microsoft.UI.Xaml.Data;
using System;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides helper methods for applying value converters to data bindings.
/// </summary>
internal static class BindingHelper
{
    /// <summary>
    /// Converts the specified value using the binding's value converter, if one is present.
    /// </summary>
    /// <remarks>If the binding does not specify a converter, the method returns the input value unchanged.</remarks>
    /// <param name="binding">The binding whose converter will be used to convert the value. Can be null if no conversion is required.</param>
    /// <param name="value">The value to be converted using the binding's converter.</param>
    /// <param name="targetType">The target type to convert the value to. If null, defaults to object.</param>
    /// <returns>The converted value if a converter is present; otherwise, the original value.</returns>
    public static object? ConvertValue(Binding? binding, object? value, Type? targetType = null)
    {
        if (binding?.Converter is { } converter)
        {
            targetType ??= typeof(object);
            value = converter.Convert(
                value,
                targetType,
                binding.ConverterParameter,
                binding.ConverterLanguage);
        }

        return value;
    }

    /// <summary>
    /// Converts back the specified value using the binding's value converter, if one is present.
    /// </summary>
    /// <remarks>If the binding does not specify a converter, the method returns the input value unchanged.</remarks>
    /// <param name="binding">The binding whose converter will be used to convert back the value. Can be null if no conversion is required.</param>
    /// <param name="value">The value to be converted using the binding's converter.</param>
    /// <param name="targetType">The target type to convert back the value to. If null, defaults to object.</param>
    /// <returns>The converted value if a converter is present; otherwise, the original value.</returns>
    public static object? ConvertBackValue(Binding? binding, object? value, Type? targetType = null)
    {
        if (binding?.Converter is { } converter)
        {
            targetType ??= typeof(object);
            value = converter.ConvertBack(
                value,
                targetType,
                binding.ConverterParameter,
                binding.ConverterLanguage);
        }

        return value;
    }
}