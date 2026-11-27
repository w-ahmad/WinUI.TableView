namespace WinUI.TableView;

partial class CollectionView
{
    private static readonly object _unreadSortValue = new();

    /// <summary>
    /// Sorts <paramref name="items"/> by the sort descriptions, reading each item's sort values at most once
    /// instead of on every comparison.
    /// </summary>
    private void SortItems(List<object?> items)
    {
        var count = items.Count;
        var depth = SortDescriptions.Count;

        if (depth == 0 || count < 2) return;

        var descriptions = new SortDescription[depth];
        SortDescriptions.CopyTo(descriptions, 0);

        var first = descriptions[0];
        var sorted = new object?[count];
        items.CopyTo(sorted);

        // Array.Sort with keys runs the same algorithm as List.Sort(this), so equal items end up in the same order.
        if (depth == 1)
        {
            var keys = new object?[count];

            for (var index = 0; index < count; index++)
            {
                keys[index] = ReadSortValue(first, sorted[index]);
            }

            Array.Sort(keys, sorted, new SortKeyComparer(first));
        }
        else
        {
            var keys = new SortKey[count];

            for (var index = 0; index < count; index++)
            {
                keys[index] = new SortKey(ReadSortValue(first, sorted[index]), index);
            }

            Array.Sort(keys, sorted, new MultiSortKeyComparer(descriptions, items));
        }

        for (var index = 0; index < count; index++)
        {
            items[index] = sorted[index];
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

    private static int CompareSortValues(SortDescription description, object? x, object? y)
    {
        var cmp = description.Compare(x, y);
        return description.Direction is SortDirection.Ascending ? +cmp : -cmp;
    }

    // A class, so both sorts share one compiled Array.Sort instead of the runtime compiling another for a struct.
    private sealed class SortKey(object? value, int index)
    {
        public readonly object? Value = value;
        public readonly int Index = index;
    }

    private sealed class SortKeyComparer(SortDescription description) : IComparer<object?>
    {
        public int Compare(object? x, object? y) => CompareSortValues(description, x, y);
    }

    private sealed class MultiSortKeyComparer : IComparer<SortKey>
    {
        private readonly SortDescription[] _descriptions;
        private readonly List<object?> _items;
        private object?[]? _laterValues;

        public MultiSortKeyComparer(SortDescription[] descriptions, List<object?> items)
        {
            _descriptions = descriptions;
            _items = items;
        }

        public int Compare(SortKey x, SortKey y)
        {
            var cmp = CompareSortValues(_descriptions[0], x.Value, y.Value);

            for (var level = 1; cmp == 0 && level < _descriptions.Length; level++)
            {
                cmp = CompareSortValues(_descriptions[level], GetLaterValue(x.Index, level), GetLaterValue(y.Index, level));
            }

            return cmp;
        }

        // Read on first use, so later values are only read for items that tie on the earlier ones.
        private object? GetLaterValue(int index, int level)
        {
            var width = _descriptions.Length - 1;

            if (_laterValues is null)
            {
                _laterValues = new object?[(long)_items.Count * width];
                Array.Fill(_laterValues, _unreadSortValue);
            }

            var slot = (long)index * width + level - 1;
            var value = _laterValues[slot];

            if (ReferenceEquals(value, _unreadSortValue))
            {
                _laterValues[slot] = value = _descriptions[level].GetPropertyValue(_items[index]);
            }

            return value;
        }
    }
}
