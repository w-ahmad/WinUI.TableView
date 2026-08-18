using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;

namespace WinUI.TableView.Selection;

/// <summary>
/// Stores row selection as a sorted list of disjoint, non-adjacent index ranges.
/// </summary>
/// <remarks>
/// Selection is held as ranges rather than per-item state so that selecting a large span costs the same as
/// selecting a single row: <c>SelectAll</c> over a million rows is one range, and membership tests are a
/// binary search over the range list rather than a lookup in a million-entry set.
/// <para>
/// Indexes are positions in the <see cref="TableView"/>'s item view (<see cref="CollectionView"/>), not
/// positions in the flattened visual row sequence, so group headers never participate.
/// </para>
/// </remarks>
internal sealed class TableViewSelectionModel
{
    private readonly List<(int First, int Last)> _ranges = [];

    /// <summary>
    /// Gets the number of selected indexes.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets the number of ranges the selection is stored as.
    /// </summary>
    public int RangeCount => _ranges.Count;

    /// <summary>
    /// Gets the lowest selected index, or -1 when nothing is selected.
    /// </summary>
    public int FirstIndex => _ranges.Count is 0 ? -1 : _ranges[0].First;

    /// <summary>
    /// Gets the highest selected index, or -1 when nothing is selected.
    /// </summary>
    public int LastIndex => _ranges.Count is 0 ? -1 : _ranges[^1].Last;

    /// <summary>
    /// Determines whether the given index is selected.
    /// </summary>
    public bool Contains(int index)
    {
        return FindRange(index) >= 0;
    }

    /// <summary>
    /// Returns the selected index at the given position within the selection, or -1 when out of range.
    /// </summary>
    /// <param name="position">The zero-based position within the ordered selection.</param>
    public int IndexAt(int position)
    {
        if (position < 0)
        {
            return -1;
        }

        foreach (var range in _ranges)
        {
            var length = range.Last - range.First + 1;
            if (position < length)
            {
                return range.First + position;
            }

            position -= length;
        }

        return -1;
    }

    /// <summary>
    /// Returns the position of the given index within the ordered selection, or -1 when it is not selected.
    /// </summary>
    public int PositionOf(int index)
    {
        var position = 0;

        foreach (var range in _ranges)
        {
            if (index < range.First)
            {
                return -1;
            }

            if (index <= range.Last)
            {
                return position + (index - range.First);
            }

            position += range.Last - range.First + 1;
        }

        return -1;
    }

    /// <summary>
    /// Creates an independent copy of this model. Used to diff a selection change into added and removed spans.
    /// </summary>
    public TableViewSelectionModel Clone()
    {
        var clone = new TableViewSelectionModel();
        clone._ranges.AddRange(_ranges);
        clone.Count = Count;

        return clone;
    }

    /// <summary>
    /// Enumerates the selected ranges in ascending order.
    /// </summary>
    public IEnumerable<ItemIndexRange> GetRanges()
    {
        foreach (var range in _ranges)
        {
            yield return new ItemIndexRange(range.First, (uint)(range.Last - range.First + 1));
        }
    }

    /// <summary>
    /// Enumerates the selected indexes in ascending order.
    /// </summary>
    public IEnumerable<int> GetIndexes()
    {
        foreach (var range in _ranges)
        {
            for (var index = range.First; index <= range.Last; index++)
            {
                yield return index;
            }
        }
    }

    /// <summary>
    /// Selects the inclusive range <paramref name="first"/>..<paramref name="last"/>.
    /// </summary>
    /// <returns><see langword="true"/> when the selection changed.</returns>
    public bool Select(int first, int last)
    {
        if (last < first || first < 0)
        {
            return false;
        }

        // Adjacent ranges are merged, so start from the first range that reaches first - 1.
        var start = LowerBoundByLast(first - 1);
        var end = start;
        var newFirst = first;
        var newLast = last;
        var removedLength = 0;

        while (end < _ranges.Count && _ranges[end].First <= last + 1)
        {
            var range = _ranges[end];
            newFirst = Math.Min(newFirst, range.First);
            newLast = Math.Max(newLast, range.Last);
            removedLength += range.Last - range.First + 1;
            end++;
        }

        // Nothing to do when a single existing range already covers the requested span exactly.
        if (end - start is 1 && _ranges[start].First <= first && _ranges[start].Last >= last)
        {
            return false;
        }

        _ranges.RemoveRange(start, end - start);
        _ranges.Insert(start, (newFirst, newLast));
        Count += newLast - newFirst + 1 - removedLength;

        return true;
    }

    /// <summary>
    /// Deselects the inclusive range <paramref name="first"/>..<paramref name="last"/>.
    /// </summary>
    /// <returns><see langword="true"/> when the selection changed.</returns>
    public bool Deselect(int first, int last)
    {
        if (last < first || _ranges.Count is 0)
        {
            return false;
        }

        var index = LowerBoundByLast(first);
        var changed = false;

        while (index < _ranges.Count && _ranges[index].First <= last)
        {
            var range = _ranges[index];
            var keepsLeft = range.First < first;
            var keepsRight = range.Last > last;

            Count -= range.Last - range.First + 1;

            if (keepsLeft && keepsRight)
            {
                _ranges[index] = (range.First, first - 1);
                _ranges.Insert(index + 1, (last + 1, range.Last));
                Count += first - range.First + (range.Last - last);
                index += 2;
            }
            else if (keepsLeft)
            {
                _ranges[index] = (range.First, first - 1);
                Count += first - range.First;
                index++;
            }
            else if (keepsRight)
            {
                _ranges[index] = (last + 1, range.Last);
                Count += range.Last - last;
                index++;
            }
            else
            {
                _ranges.RemoveAt(index);
            }

            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Selects every index in <c>[0, itemCount)</c>.
    /// </summary>
    /// <returns><see langword="true"/> when the selection changed.</returns>
    public bool SelectAll(int itemCount)
    {
        if (itemCount <= 0)
        {
            return Clear();
        }

        if (_ranges.Count is 1 && _ranges[0] == (0, itemCount - 1))
        {
            return false;
        }

        _ranges.Clear();
        _ranges.Add((0, itemCount - 1));
        Count = itemCount;

        return true;
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    /// <returns><see langword="true"/> when the selection changed.</returns>
    public bool Clear()
    {
        if (_ranges.Count is 0)
        {
            return false;
        }

        _ranges.Clear();
        Count = 0;

        return true;
    }

    /// <summary>
    /// Replaces the selection with the single index <paramref name="index"/>.
    /// </summary>
    /// <returns><see langword="true"/> when the selection changed.</returns>
    public bool SelectOnly(int index)
    {
        if (index < 0)
        {
            return Clear();
        }

        if (_ranges.Count is 1 && _ranges[0] == (index, index))
        {
            return false;
        }

        _ranges.Clear();
        _ranges.Add((index, index));
        Count = 1;

        return true;
    }

    /// <summary>
    /// Shifts the selection to account for <paramref name="count"/> items inserted at <paramref name="index"/>.
    /// The inserted items are not selected, so a range spanning the insertion point is split.
    /// </summary>
    public void OnItemsInserted(int index, int count)
    {
        if (count <= 0)
        {
            return;
        }

        for (var i = _ranges.Count - 1; i >= 0; i--)
        {
            var range = _ranges[i];

            if (range.Last < index)
            {
                break;
            }

            if (range.First >= index)
            {
                _ranges[i] = (range.First + count, range.Last + count);
            }
            else
            {
                _ranges[i] = (range.First, index - 1);
                _ranges.Insert(i + 1, (index + count, range.Last + count));
            }
        }
    }

    /// <summary>
    /// Shifts the selection to account for <paramref name="count"/> items removed at <paramref name="index"/>.
    /// </summary>
    /// <returns><see langword="true"/> when removed items had been selected.</returns>
    public bool OnItemsRemoved(int index, int count)
    {
        if (count <= 0)
        {
            return false;
        }

        var changed = Deselect(index, index + count - 1);

        // Deselect left no range overlapping the removed span, so everything after it shifts down wholesale.
        for (var i = 0; i < _ranges.Count; i++)
        {
            var range = _ranges[i];

            if (range.First >= index)
            {
                _ranges[i] = (range.First - count, range.Last - count);
            }
        }

        MergeAdjacent();

        return changed;
    }

    /// <summary>
    /// Drops any selection at or beyond <paramref name="itemCount"/>, for when the item view shrinks.
    /// </summary>
    /// <returns><see langword="true"/> when the selection changed.</returns>
    public bool TrimTo(int itemCount)
    {
        if (itemCount <= 0)
        {
            return Clear();
        }

        return LastIndex >= itemCount && Deselect(itemCount, LastIndex);
    }

    /// <summary>
    /// Merges ranges that became adjacent after a shift.
    /// </summary>
    private void MergeAdjacent()
    {
        for (var i = _ranges.Count - 1; i > 0; i--)
        {
            if (_ranges[i - 1].Last + 1 >= _ranges[i].First)
            {
                _ranges[i - 1] = (_ranges[i - 1].First, Math.Max(_ranges[i - 1].Last, _ranges[i].Last));
                _ranges.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Returns the index of the range containing <paramref name="index"/>, or the bitwise complement of the
    /// insertion point when it is not selected.
    /// </summary>
    private int FindRange(int index)
    {
        var low = 0;
        var high = _ranges.Count - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) >> 1);
            var range = _ranges[middle];

            if (index < range.First)
            {
                high = middle - 1;
            }
            else if (index > range.Last)
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        return ~low;
    }

    /// <summary>
    /// Returns the index of the first range whose last index is greater than or equal to <paramref name="value"/>.
    /// </summary>
    private int LowerBoundByLast(int value)
    {
        var low = 0;
        var high = _ranges.Count;

        while (low < high)
        {
            var middle = low + ((high - low) >> 1);

            if (_ranges[middle].Last < value)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}
