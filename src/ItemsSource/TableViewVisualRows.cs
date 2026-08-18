using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WinUI.TableView;

/// <summary>
/// The flattened sequence of rows the row host displays: group header rows interleaved with the data rows they
/// cover, with the contents of collapsed groups left out.
/// </summary>
/// <remarks>
/// This is the boundary between the two index spaces the control works in. An <em>item index</em> is a position
/// in the item view and is what cell slots, selection ranges, the clipboard and automation mean by "row". A
/// <em>visual index</em> is a position in this sequence and is what the layout, the repeater, scrolling and hit
/// testing use. Conversion is a binary search over a run list whose length is proportional to the number of
/// visible groups, and is a single comparison when nothing is grouped.
/// <para>
/// Exposes the non-generic <see cref="IList"/> plus <see cref="INotifyCollectionChanged"/> because that is the
/// shape <c>ItemsSourceView</c> recognises on both Windows App SDK and Uno.
/// </para>
/// </remarks>
internal sealed class TableViewVisualRows : IList, INotifyCollectionChanged
{
    private readonly CollectionView _collectionView;
    private readonly Func<TableViewGroup, bool> _isCollapsed;
    private readonly List<Run> _runs = [];
    private int _count;
    private bool _hasGroups;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewVisualRows"/> class.
    /// </summary>
    /// <param name="collectionView">The item view to project.</param>
    /// <param name="isCollapsed">Decides whether a group's contents are hidden.</param>
    public TableViewVisualRows(CollectionView collectionView, Func<TableViewGroup, bool> isCollapsed)
    {
        _collectionView = collectionView;
        _isCollapsed = isCollapsed;

        Rebuild();
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether any group header rows are present.
    /// </summary>
    public bool HasGroups => _hasGroups;

    /// <inheritdoc/>
    public object? this[int index]
    {
        get
        {
            var run = FindRunByVisualIndex(index);

            if (run < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var entry = _runs[run];

            return entry.GroupIndex >= 0
                ? _collectionView.Groups[entry.GroupIndex]
                : _collectionView[entry.ItemStart + (index - entry.VisualStart)];
        }
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns the item index the given visual index maps to, or -1 when it is a group header row.
    /// </summary>
    public int GetItemIndex(int visualIndex)
    {
        var run = FindRunByVisualIndex(visualIndex);

        if (run < 0)
        {
            return -1;
        }

        var entry = _runs[run];

        return entry.GroupIndex >= 0 ? -1 : entry.ItemStart + (visualIndex - entry.VisualStart);
    }

    /// <summary>
    /// Returns the visual index the given item index maps to, or -1 when the item is inside a collapsed group.
    /// </summary>
    public int GetVisualIndex(int itemIndex)
    {
        if (itemIndex < 0)
        {
            return -1;
        }

        // Data runs are ordered by item index, so this is the mirror of FindRunByVisualIndex.
        var low = 0;
        var high = _runs.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var entry = _runs[middle];

            if (entry.GroupIndex >= 0 || itemIndex < entry.ItemStart)
            {
                // Header runs carry the item index of the row that follows them, which lets the search skip
                // past them in the right direction.
                if (entry.ItemStart > itemIndex)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }
            else if (itemIndex >= entry.ItemStart + entry.Length)
            {
                low = middle + 1;
            }
            else
            {
                return entry.VisualStart + (itemIndex - entry.ItemStart);
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the group whose header sits at the given visual index, or <see langword="null"/> for a data row.
    /// </summary>
    public TableViewGroup? GetGroup(int visualIndex)
    {
        var run = FindRunByVisualIndex(visualIndex);

        if (run < 0)
        {
            return null;
        }

        var entry = _runs[run];

        return entry.GroupIndex >= 0 ? _collectionView.Groups[entry.GroupIndex] : null;
    }

    /// <summary>
    /// Rebuilds the run list from the current groups and collapse state, and reports a reset.
    /// </summary>
    public void Reset()
    {
        Rebuild();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Reprojects after an item was inserted into the item view.
    /// </summary>
    /// <remarks>
    /// Raises a single-item add when the projection grew by exactly one row, so the host realizes one row rather
    /// than re-realizing the viewport. When the insertion also created group header rows the shape changed by
    /// more than one row and a reset is the honest report.
    /// </remarks>
    public void OnItemInserted(int itemIndex)
    {
        var previousCount = _count;

        Rebuild();

        var visualIndex = GetVisualIndex(itemIndex);

        if (_count == previousCount + 1 && visualIndex >= 0)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, this[visualIndex], visualIndex));
        }
        else
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Reprojects after an item was removed from the item view.
    /// </summary>
    /// <param name="visualIndex">The visual index the removed row occupied, captured before the removal.</param>
    /// <param name="item">The removed item.</param>
    public void OnItemRemoved(int visualIndex, object? item)
    {
        var previousCount = _count;

        Rebuild();

        if (_count == previousCount - 1 && visualIndex >= 0)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, item, visualIndex));
        }
        else
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Rebuilds the run list. Costs one pass over the groups, never over the items.
    /// </summary>
    private void Rebuild()
    {
        _runs.Clear();
        _count = 0;
        _hasGroups = false;

        var itemCount = _collectionView.Count;
        var groups = _collectionView.Groups;

        if (groups.Count is 0)
        {
            if (itemCount > 0)
            {
                AddDataRun(0, itemCount);
            }

            return;
        }

        var nextItem = 0;
        var hiddenUntil = -1;

        for (var g = 0; g < groups.Count; g++)
        {
            var group = groups[g];

            // Groups nested inside a collapsed group contribute neither a header nor their items.
            if (group.FirstItemIndex < hiddenUntil)
            {
                continue;
            }

            if (group.FirstItemIndex > nextItem)
            {
                AddDataRun(nextItem, group.FirstItemIndex - nextItem);
            }

            nextItem = group.FirstItemIndex;
            AddHeaderRun(g, nextItem);

            if (_isCollapsed(group))
            {
                hiddenUntil = group.LastItemIndex + 1;
                nextItem = hiddenUntil;
            }
        }

        if (itemCount > nextItem)
        {
            AddDataRun(nextItem, itemCount - nextItem);
        }
    }

    private void AddDataRun(int itemStart, int length)
    {
        _runs.Add(new Run(_count, itemStart, length, -1));
        _count += length;
    }

    private void AddHeaderRun(int groupIndex, int itemStart)
    {
        _runs.Add(new Run(_count, itemStart, 1, groupIndex));
        _count++;
        _hasGroups = true;
    }

    /// <summary>
    /// Returns the index of the run containing <paramref name="visualIndex"/>, or -1 when out of range.
    /// </summary>
    private int FindRunByVisualIndex(int visualIndex)
    {
        if (visualIndex < 0 || visualIndex >= _count)
        {
            return -1;
        }

        var low = 0;
        var high = _runs.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var entry = _runs[middle];

            if (visualIndex < entry.VisualStart)
            {
                high = middle - 1;
            }
            else if (visualIndex >= entry.VisualStart + entry.Length)
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        return -1;
    }

    /// <summary>
    /// A stretch of consecutive visual rows: either one group header, or a span of data rows.
    /// </summary>
    private readonly struct Run(int visualStart, int itemStart, int length, int groupIndex)
    {
        /// <summary>The first visual index this run covers.</summary>
        public int VisualStart { get; } = visualStart;

        /// <summary>For a data run, the first item index; for a header, the item index the header precedes.</summary>
        public int ItemStart { get; } = itemStart;

        /// <summary>The number of visual rows in this run; always 1 for a header.</summary>
        public int Length { get; } = length;

        /// <summary>The index into the view's groups for a header run, or -1 for a data run.</summary>
        public int GroupIndex { get; } = groupIndex;
    }

    // Mutation and the rest of IList are not supported: this is a projection.

    /// <inheritdoc/>
    public bool IsFixedSize => false;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public bool IsSynchronized => false;

    /// <inheritdoc/>
    public object SyncRoot => this;

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return this[i];
        }
    }

    /// <inheritdoc/>
    public bool Contains(object? value) => IndexOf(value) >= 0;

    /// <inheritdoc/>
    public int IndexOf(object? value)
    {
        if (value is TableViewGroup group)
        {
            // Header runs are few (one per visible group), and looking a group up by identity is not a hot path.
            var groups = _collectionView.Groups;

            foreach (var run in _runs)
            {
                if (run.GroupIndex >= 0 && ReferenceEquals(groups[run.GroupIndex], group))
                {
                    return run.VisualStart;
                }
            }

            return -1;
        }

        var itemIndex = _collectionView.IndexOf(value);

        return itemIndex < 0 ? -1 : GetVisualIndex(itemIndex);
    }

    /// <inheritdoc/>
    public void CopyTo(Array array, int index)
    {
        for (var i = 0; i < _count; i++)
        {
            array.SetValue(this[i], index + i);
        }
    }

    /// <inheritdoc/>
    public int Add(object? value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Clear() => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Insert(int index, object? value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Remove(object? value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void RemoveAt(int index) => throw new NotSupportedException();
}
