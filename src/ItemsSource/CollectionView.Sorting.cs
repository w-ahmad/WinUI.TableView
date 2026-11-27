namespace WinUI.TableView;

partial class CollectionView
{
    /// <summary>
    /// Sorts <paramref name="items"/> by the sort descriptions, reading each item's sort values at most once
    /// instead of on every comparison.
    /// </summary>
    private void SortItems(List<object?> items)
    {
        var count = items.Count;
        var depth = SortDescriptions.Count;

        if (depth == 0 || count < 2) return;

        var comparers = new SortKeyComparer[depth];

        for (var level = 0; level < depth; level++)
        {
            comparers[level] = new SortKeyComparer(SortDescriptions[level]);
        }

        var sorted = new object?[count];
        items.CopyTo(sorted);

        SortRange(comparers, sorted, new object?[count], 0, count, 0);

        for (var index = 0; index < count; index++)
        {
            items[index] = sorted[index];
        }
    }

    // Sorts a range by one description, then each run of equal keys by the next, so later values are only read for ties.
    private static void SortRange(SortKeyComparer[] comparers, object?[] items, object?[] keys, int start, int length, int level)
    {
        var comparer = comparers[level];
        var end = start + length;

        for (var index = start; index < end; index++)
        {
            keys[index] = ReadSortValue(comparer.Description, items[index]);
        }

        // Array.Sort with keys runs the same algorithm as List.Sort, so one description orders equal items as before.
        Array.Sort(keys, items, start, length, comparer);

        if (level + 1 == comparers.Length) return;

        var runStart = start;

        for (var index = start + 1; index <= end; index++)
        {
            if (index < end && comparer.Compare(keys[runStart], keys[index]) == 0) continue;

            // The next level reuses the key slots of the run.
            if (index - runStart > 1)
            {
                SortRange(comparers, items, keys, runStart, index - runStart, level + 1);
            }

            runStart = index;
        }
    }

    private static object? ReadSortValue(SortDescription description, object? item)
    {
        try
        {
            return description.GetPropertyValue(item);
        }
        catch (Exception ex)
        {
            // Matches what a failing read inside a comparison throws from Array.Sort.
            throw new InvalidOperationException("Failed to compare two elements in the array.", ex);
        }
    }

    private sealed class SortKeyComparer(SortDescription description) : IComparer<object?>
    {
        public SortDescription Description => description;

        public int Compare(object? x, object? y)
        {
            var cmp = description.Compare(x, y);
            return description.Direction is SortDirection.Ascending ? +cmp : -cmp;
        }
    }
}
