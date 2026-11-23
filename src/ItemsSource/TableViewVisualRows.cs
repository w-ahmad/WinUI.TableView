using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WinUI.TableView;

/// <summary>
/// The sequence of rows the row host displays: group header rows interleaved with the data rows they cover.
/// </summary>
/// <remarks>
/// This is the boundary between the two index spaces the control works in. An <em>item index</em> is a position
/// in <see cref="CollectionView"/> and is what <see cref="TableViewCellSlot.Row"/>,
/// <see cref="TableView.SelectedRanges"/>, the clipboard and automation all mean by "row". A <em>visual index</em>
/// is a position in this sequence and is what the layout, the repeater, scrolling and hit testing use.
/// <para>
/// The grouping itself is <see cref="CollectionView"/>'s: its <see cref="CollectionView.CollectionGroups"/> is a
/// depth-first, flattened list with one entry per reachable group, and its item view already leaves out the
/// items of collapsed groups. This class only interleaves the two, as a list of segments that is rebuilt when the
/// view resets. Converting between the index spaces is a binary search over those segments, whose count is
/// proportional to the number of visible groups rather than the number of items; with no grouping there is a
/// single segment and every lookup is a comparison.
/// </para>
/// <para>
/// Exposes the non-generic <see cref="IList"/> plus <see cref="INotifyCollectionChanged"/> because that is the
/// shape <c>ItemsSourceView</c> recognises on both Windows App SDK and Uno.
/// </para>
/// </remarks>
internal sealed partial class TableViewVisualRows : IList, INotifyCollectionChanged
{
    private readonly CollectionView _collectionView;

    // Every segment, ordered by visual index.
    private readonly List<Segment> _segments = [];

    // The data segments only, ordered by item index, for the item-to-visual direction.
    private readonly List<Segment> _dataSegments = [];

    // The visual index of every group header, ascending, for finding the header a row belongs to.
    private readonly List<int> _headerVisualIndexes = [];
    private readonly List<TableViewGroupInfo> _headers = [];
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewVisualRows"/> class.
    /// </summary>
    /// <param name="collectionView">The item view to project.</param>
    public TableViewVisualRows(CollectionView collectionView)
    {
        _collectionView = collectionView;

        Rebuild();
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public int Count => _count;

    /// <summary>
    /// Gets a value indicating whether any group header rows are present.
    /// </summary>
    public bool HasGroups => _headers.Count > 0;

    /// <inheritdoc/>
    public object? this[int index]
    {
        get
        {
            var segment = FindSegment(index);

            if (segment < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var entry = _segments[segment];

            return entry.HeaderIndex >= 0
                ? _headers[entry.HeaderIndex]
                : _collectionView[entry.ItemStart + (index - entry.VisualStart)];
        }
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Returns the item index the given visual index shows, or -1 when it is a group header row or out of range.
    /// </summary>
    public int GetItemIndex(int visualIndex)
    {
        if (!HasGroups)
        {
            return visualIndex >= 0 && visualIndex < _count ? visualIndex : -1;
        }

        var segment = FindSegment(visualIndex);

        if (segment < 0)
        {
            return -1;
        }

        var entry = _segments[segment];

        return entry.HeaderIndex >= 0 ? -1 : entry.ItemStart + (visualIndex - entry.VisualStart);
    }

    /// <summary>
    /// Returns the visual index showing the given item index, or -1 when it is out of range.
    /// </summary>
    public int GetVisualIndex(int itemIndex)
    {
        if (!HasGroups)
        {
            return itemIndex >= 0 && itemIndex < _count ? itemIndex : -1;
        }

        var low = 0;
        var high = _dataSegments.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var entry = _dataSegments[middle];

            if (itemIndex < entry.ItemStart)
            {
                high = middle - 1;
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
    /// Returns the item index of the first data row at or after <paramref name="visualIndex"/>, or -1 when there is
    /// none - skipping over any group headers in between.
    /// </summary>
    public int FindItemIndexAtOrAfter(int visualIndex)
    {
        visualIndex = Math.Max(0, visualIndex);

        // The first data segment that ends at or after the visual index.
        var found = -1;
        var low = 0;
        var high = _dataSegments.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var entry = _dataSegments[middle];

            if (entry.VisualStart + entry.Length - 1 < visualIndex)
            {
                low = middle + 1;
            }
            else
            {
                found = middle;
                high = middle - 1;
            }
        }

        if (found < 0)
        {
            return -1;
        }

        var segment = _dataSegments[found];

        return segment.ItemStart + Math.Max(0, visualIndex - segment.VisualStart);
    }

    /// <summary>
    /// Returns the item index of the last data row at or before <paramref name="visualIndex"/>, or -1 when there is
    /// none - skipping over any group headers in between.
    /// </summary>
    public int FindItemIndexAtOrBefore(int visualIndex)
    {
        // The last data segment that starts at or before the visual index.
        var found = -1;
        var low = 0;
        var high = _dataSegments.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);

            if (_dataSegments[middle].VisualStart <= visualIndex)
            {
                found = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        if (found < 0)
        {
            return -1;
        }

        var segment = _dataSegments[found];

        return segment.ItemStart + Math.Min(segment.Length - 1, visualIndex - segment.VisualStart);
    }

    /// <summary>
    /// Returns the group whose header sits at the given visual index, or <see langword="null"/> for a data row.
    /// </summary>
    public TableViewGroupInfo? GetGroupHeader(int visualIndex)
    {
        if (!HasGroups)
        {
            return null;
        }

        var segment = FindSegment(visualIndex);

        return segment >= 0 && _segments[segment].HeaderIndex is var headerIndex and >= 0 ? _headers[headerIndex] : null;
    }

    /// <summary>
    /// Returns the visual index of the nearest group header at or before <paramref name="visualIndex"/> - the
    /// header of the group the row belongs to - or -1 when there is none.
    /// </summary>
    public int FindHeaderAtOrBefore(int visualIndex)
    {
        var position = _headerVisualIndexes.BinarySearch(visualIndex);

        if (position >= 0)
        {
            return _headerVisualIndexes[position];
        }

        position = ~position - 1;

        return position >= 0 ? _headerVisualIndexes[position] : -1;
    }

    /// <summary>
    /// Returns the visual index of the first group header after <paramref name="visualIndex"/>, or -1 when there
    /// is none.
    /// </summary>
    public int FindHeaderAfter(int visualIndex)
    {
        var position = _headerVisualIndexes.BinarySearch(visualIndex);
        position = position >= 0 ? position + 1 : ~position;

        return position < _headerVisualIndexes.Count ? _headerVisualIndexes[position] : -1;
    }

    /// <summary>
    /// Reprojects after the item view changed shape in a way that is not a single insert or remove, and reports a
    /// reset.
    /// </summary>
    public void Reset()
    {
        Rebuild();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Reprojects after an item was inserted at the given item index.
    /// </summary>
    /// <remarks>
    /// The item view only reports single inserts when it is not grouped (any change to a grouped view is a
    /// reset), so this is a one-row add unless the projection disagrees, in which case a reset is the honest
    /// report.
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
    /// Reprojects after an item was removed.
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
    /// Rebuilds the segments. Costs one pass over the groups, never over the items.
    /// </summary>
    private void Rebuild()
    {
        _segments.Clear();
        _dataSegments.Clear();
        _headerVisualIndexes.Clear();
        _headers.Clear();
        _count = 0;

        var itemCount = _collectionView.Count;
        var nextItem = 0;

        if (_collectionView.CollectionGroups is { Count: > 0 } groups)
        {
            foreach (var entry in groups)
            {
                if (entry is not CollectionViewGroup { Group: TableViewGroupInfo header } group)
                {
                    continue;
                }

                _headerVisualIndexes.Add(_count);
                _segments.Add(new Segment(_count, nextItem, 1, _headers.Count));
                _headers.Add(header);
                _count++;

                // Only an expanded leaf group lists its items, and the item view holds exactly those items, in
                // this same order - so a group's rows are the next GroupItems.Count items of the view.
                var length = Math.Min(group.GroupItems?.Count ?? 0, itemCount - nextItem);

                if (length > 0)
                {
                    AddDataSegment(nextItem, length);
                    nextItem += length;
                }
            }
        }

        // Anything the groups did not account for (nothing, when the two agree) is still shown rather than lost.
        if (itemCount > nextItem)
        {
            AddDataSegment(nextItem, itemCount - nextItem);
        }
    }

    private void AddDataSegment(int itemStart, int length)
    {
        var segment = new Segment(_count, itemStart, length, -1);

        _segments.Add(segment);
        _dataSegments.Add(segment);
        _count += length;
    }

    /// <summary>
    /// Returns the index of the segment containing <paramref name="visualIndex"/>, or -1 when out of range.
    /// </summary>
    private int FindSegment(int visualIndex)
    {
        if (visualIndex < 0 || visualIndex >= _count)
        {
            return -1;
        }

        var low = 0;
        var high = _segments.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var entry = _segments[middle];

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
    private readonly struct Segment(int visualStart, int itemStart, int length, int headerIndex)
    {
        /// <summary>The first visual index this segment covers.</summary>
        public int VisualStart { get; } = visualStart;

        /// <summary>For a data segment, its first item index; for a header, the item index that follows it.</summary>
        public int ItemStart { get; } = itemStart;

        /// <summary>The number of visual rows in this segment; always 1 for a header.</summary>
        public int Length { get; } = length;

        /// <summary>The header's position in the header list, or -1 for a data segment.</summary>
        public int HeaderIndex { get; } = headerIndex;
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
        if (value is TableViewGroupInfo header)
        {
            // Headers are few (one per visible group), and looking one up by identity is not a hot path.
            var headerIndex = _headers.IndexOf(header);

            return headerIndex < 0 ? -1 : _headerVisualIndexes[headerIndex];
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
