using Microsoft.UI.Xaml.Data;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Determines whether the given item index range is valid within the TableView.
    /// </summary>
    /// <param name="range">The ItemIndexRange to check.</param>
    /// <param name="tableView">The TableView to check against.</param>
    /// <returns>True if the item index range of TableView is valid; otherwise, false.</returns>
    public static bool IsValid(this ItemIndexRange range, TableView tableView)
    {
        return range.FirstIndex >= 0 && range.LastIndex < tableView?.Items.Count;
    }

    /// <summary>
    /// Determines whether the specified range completely contains another range.
    /// </summary>
    /// <param name="range">The range to check.</param>
    /// <param name="other">The range to check against.</param>
    /// <returns>True if the range completely contains the other range; otherwise, false.</returns>
    public static bool Contains(this ItemIndexRange range, ItemIndexRange other)
    {
        return other.FirstIndex >= range.FirstIndex && other.LastIndex <= range.LastIndex;
    }

    /// <summary>
    /// Subtracts another ItemIndexRange from the current range and returns the resulting range.
    /// </summary>
    /// <param name="range">The range to subtract from.</param>
    /// <param name="other">The range to subtract.</param>
    /// <returns>The resulting range after subtraction.</returns>
    public static IEnumerable<ItemIndexRange> Subtract(this ItemIndexRange range, ItemIndexRange other)
    {
        var start = range.FirstIndex;
        var end = start + (int)range.Length - 1;

        var otherStart = other.FirstIndex;
        var otherEnd = otherStart + (int)other.Length - 1;

        // No overlap.
        if (otherEnd < start || otherStart > end)
        {
            yield break;
        }

        // Left remainder.
        if (otherStart > start)
        {
            yield return new ItemIndexRange(start, (uint)(otherStart - start));
        }

        // Right remainder.
        if (otherEnd < end)
        {
            yield return new ItemIndexRange(otherEnd + 1, (uint)(end - otherEnd));
        }
    }
}
