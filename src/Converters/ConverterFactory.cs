using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.Converters
{
    /// <summary>
    /// Factory for creating value converters with intelligent fallback logic.
    /// 
    /// This factory handles three common binding scenarios in order of priority:
    /// 1. Custom converter provided - use it as-is
    /// 2. Expected value provided - create equality converter automatically  
    /// 3. Neither provided - return null for direct binding (no conversion)
    /// 
    /// IMPORTANT: Returning null means "no converter needed" not "no binding".
    /// The calling code should still create the binding but omit the converter.
    /// This allows direct bool-to-bool or bool-to-Visibility bindings to work
    /// without unnecessary conversion overhead.
    /// 
    /// Converters are cached by expected value to avoid creating duplicates,
    /// which improves performance when the same column configuration is used
    /// across multiple rows.
    /// </summary>
    public static class ConverterFactory
    {
        private static readonly ConcurrentDictionary<object, IValueConverter> _converterCache = new();

        /// <summary>
        /// Creates a converter for Visibility bindings with fallback logic.
        /// </summary>
        /// <param name="customConverter">User-provided converter (highest priority)</param>
        /// <param name="expectedValue">Value to compare against for equality conversion (medium priority)</param>
        /// <returns>
        /// - customConverter if provided
        /// - EqualityToVisibilityConverter if expectedValue is provided
        /// - null if neither provided (direct binding - WinUI converts bool to Visibility automatically)
        /// </returns>
        public static IValueConverter? CreateVisibilityConverter(IValueConverter? customConverter, object? expectedValue)
        {
            // Priority 1: Use custom converter if provided
            if (customConverter != null)
                return customConverter;

            // Priority 2: Create equality converter if expectedValue is provided
            if (expectedValue != null)
            {
                return _converterCache.GetOrAdd(
                                                $"Visibility_{expectedValue}",
                                                _ => new EqualityToVisibilityConverter { ExpectedValue = expectedValue }
                                               );
            }

            // Priority 3: No converter needed - direct binding (bool to Visibility conversion is automatic in WinUI)
            return null;
        }

        /// <summary>
        /// Creates a converter for boolean property bindings with fallback logic.
        /// </summary>
        /// <param name="customConverter">User-provided converter (highest priority)</param>
        /// <param name="expectedValue">Value to compare against for equality conversion (medium priority)</param>
        /// <returns>
        /// - customConverter if provided
        /// - EqualityToBooleanConverter if expectedValue is provided  
        /// - null if neither provided (direct binding - assumes source property is already boolean)
        /// </returns>
        public static IValueConverter? CreateBooleanConverter(IValueConverter? customConverter, object? expectedValue)
        {
            // Priority 1: Use custom converter if provided
            if (customConverter != null)
                return customConverter;

            // Priority 2: Create equality converter if expectedValue is provided  
            if (expectedValue != null)
            {
                return _converterCache.GetOrAdd(
                                                $"Boolean_{expectedValue}",
                                                _ => new EqualityToBooleanConverter { ExpectedValue = expectedValue }
                                               );
            }

            // Priority 3: No converter needed - direct binding (assumes source is already boolean)
            return null;
        }
    }
}
