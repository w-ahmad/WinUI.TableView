using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides helper methods for working with index ranges in a TableView.
/// </summary>
internal static class IndexRangeHelper
{
    /// <summary>
    /// Gets a list of contiguous index ranges from a collection of indexes.
    /// </summary>
    /// <param name="indexes">The collection of indexes to process.</param>
    /// <returns>A list of contiguous index ranges.</returns>
    public static List<ItemIndexRange> GetRanges(IEnumerable<int> indexes)
    {
        var sorted = indexes.Order().ToArray();

        if (sorted.Length == 0)
            return [];

        List<ItemIndexRange> ranges = [];

        var first = sorted[0];
        var previous = sorted[0];

        for (var i = 1; i < sorted.Length; i++)
        {
            if (sorted[i] == previous + 1)
            {
                previous = sorted[i];
                continue;
            }

            ranges.Add(new ItemIndexRange(first, (uint)(previous - first + 1)));

            first = previous = sorted[i];
        }

        ranges.Add(new ItemIndexRange(first, (uint)(previous - first + 1)));

        return ranges;
    }
}
