using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for the ItemIndexRange type.
/// </summary>
public static class ItemIndexRangeExtensions
{
    /// <summary>
    /// Determines whether a specified index is within the range.
    /// </summary>
    /// <param name="range">The ItemIndexRange to check.</param>
    /// <param name="index">The index to check.</param>
    /// <returns>True if the index is within the range; otherwise, false.</returns>
    public static bool IsInRange(this ItemIndexRange range, int index)
    {
        return index >= range.FirstIndex && index <= range.LastIndex;
    }
}
