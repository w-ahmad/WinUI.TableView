using System;
using System.Collections.Generic;

namespace WinUI.TableView.Layout;

/// <summary>
/// Maps between visual row indexes and vertical offsets for <see cref="TableViewRowsLayout"/>.
/// </summary>
/// <remarks>
/// Rows are assumed to share a common height, and only the rows that differ from it are stored. In practice
/// nearly every row measures the same, and the exceptions are the few with an expanded details pane plus the
/// group header rows — so the store holds a handful of entries no matter how many rows there are.
/// <para>
/// With no exceptions recorded, offset and index are plain arithmetic; with exceptions they cost a binary search
/// over the exception list. Either way nothing walks the rows, so the total extent of a million-row table is as
/// cheap to compute as that of a ten-row one.
/// </para>
/// </remarks>
internal sealed class TableViewRowHeights
{
    private readonly List<int> _indexes = [];
    private readonly List<double> _heights = [];
    private readonly List<double> _extraPrefix = [];
    private double _defaultHeight = 40d;
    private double _totalExtra;
    private bool _prefixDirty;

    /// <summary>
    /// Gets or sets the number of visual rows.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the height used for rows with no recorded height of their own.
    /// </summary>
    public double DefaultHeight
    {
        get => _defaultHeight;
        set
        {
            if (value > 0 && Math.Abs(_defaultHeight - value) > Tolerance)
            {
                // The recorded heights are stored as absolute values, so only the aggregate needs rebuilding.
                _defaultHeight = value;
                RecomputeExtra();
            }
        }
    }

    /// <summary>
    /// Gets the number of rows whose height differs from <see cref="DefaultHeight"/>.
    /// </summary>
    public int RecordedCount => _indexes.Count;

    /// <summary>
    /// Gets the total height of all rows.
    /// </summary>
    public double TotalHeight => (Count * _defaultHeight) + _totalExtra;

    /// <summary>
    /// Gets the height of the row at <paramref name="index"/>.
    /// </summary>
    public double GetHeight(int index)
    {
        if (_indexes.Count is 0)
        {
            return _defaultHeight;
        }

        var position = _indexes.BinarySearch(index);

        return position >= 0 ? _heights[position] : _defaultHeight;
    }

    /// <summary>
    /// Records the measured height of the row at <paramref name="index"/>.
    /// </summary>
    /// <returns><see langword="true"/> when this changed the total height.</returns>
    public bool SetHeight(int index, double height)
    {
        if (index < 0 || double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            return false;
        }

        var position = _indexes.BinarySearch(index);
        var matchesDefault = Math.Abs(height - _defaultHeight) <= Tolerance;

        if (position >= 0)
        {
            if (Math.Abs(_heights[position] - height) <= Tolerance)
            {
                return false;
            }

            if (matchesDefault)
            {
                _indexes.RemoveAt(position);
                _heights.RemoveAt(position);
            }
            else
            {
                _heights[position] = height;
            }
        }
        else
        {
            if (matchesDefault)
            {
                return false;
            }

            var insertAt = ~position;
            _indexes.Insert(insertAt, index);
            _heights.Insert(insertAt, height);
        }

        RecomputeExtra();

        return true;
    }

    /// <summary>
    /// Gets the vertical offset of the row at <paramref name="index"/>. Accepts <see cref="Count"/> to get the
    /// offset just past the last row.
    /// </summary>
    public double GetOffset(int index)
    {
        if (index <= 0)
        {
            return 0d;
        }

        if (index > Count)
        {
            index = Count;
        }

        return (index * _defaultHeight) + ExtraBefore(index);
    }

    /// <summary>
    /// Gets the index of the row containing <paramref name="offset"/>, clamped to the row range.
    /// </summary>
    public int GetIndexAt(double offset)
    {
        if (Count is 0)
        {
            return 0;
        }

        if (offset <= 0d)
        {
            return 0;
        }

        if (_indexes.Count is 0)
        {
            // No recorded exceptions: uniform rows, so this is division.
            return Math.Min(Count - 1, (int)(offset / _defaultHeight));
        }

        if (offset >= TotalHeight)
        {
            return Count - 1;
        }

        // GetOffset is non-decreasing, so the largest index whose offset is at or before the target is a
        // straight binary search. The nested ExtraBefore lookup is over the (tiny) exception list.
        var low = 0;
        var high = Count - 1;

        while (low < high)
        {
            var middle = low + ((high - low + 1) >> 1);

            if (GetOffset(middle) <= offset)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        return low;
    }

    /// <summary>
    /// Shifts recorded heights to account for rows inserted at <paramref name="index"/>.
    /// </summary>
    public void OnRowsInserted(int index, int count)
    {
        if (count <= 0)
        {
            return;
        }

        Count += count;

        for (var i = _indexes.Count - 1; i >= 0; i--)
        {
            if (_indexes[i] < index)
            {
                break;
            }

            _indexes[i] += count;
        }

        _prefixDirty = true;
    }

    /// <summary>
    /// Drops and shifts recorded heights to account for rows removed at <paramref name="index"/>.
    /// </summary>
    public void OnRowsRemoved(int index, int count)
    {
        if (count <= 0)
        {
            return;
        }

        Count = Math.Max(0, Count - count);

        for (var i = _indexes.Count - 1; i >= 0; i--)
        {
            var recorded = _indexes[i];

            if (recorded < index)
            {
                break;
            }

            if (recorded < index + count)
            {
                _indexes.RemoveAt(i);
                _heights.RemoveAt(i);
            }
            else
            {
                _indexes[i] = recorded - count;
            }
        }

        RecomputeExtra();
    }

    /// <summary>
    /// Forgets every recorded height, keeping <see cref="DefaultHeight"/>.
    /// </summary>
    public void Reset(int count)
    {
        _indexes.Clear();
        _heights.Clear();
        _extraPrefix.Clear();
        _totalExtra = 0d;
        _prefixDirty = false;
        Count = count;
    }

    /// <summary>
    /// Gets the accumulated difference from <see cref="DefaultHeight"/> for every recorded row before
    /// <paramref name="index"/>.
    /// </summary>
    private double ExtraBefore(int index)
    {
        if (_indexes.Count is 0)
        {
            return 0d;
        }

        EnsurePrefix();

        var position = _indexes.BinarySearch(index);

        if (position < 0)
        {
            position = ~position;
        }

        return _extraPrefix[position];
    }

    private void EnsurePrefix()
    {
        if (!_prefixDirty)
        {
            return;
        }

        _extraPrefix.Clear();
        _extraPrefix.Add(0d);

        var running = 0d;

        for (var i = 0; i < _indexes.Count; i++)
        {
            running += _heights[i] - _defaultHeight;
            _extraPrefix.Add(running);
        }

        _totalExtra = running;
        _prefixDirty = false;
    }

    private void RecomputeExtra()
    {
        _prefixDirty = true;
        EnsurePrefix();
    }

    private const double Tolerance = 0.01d;
}
