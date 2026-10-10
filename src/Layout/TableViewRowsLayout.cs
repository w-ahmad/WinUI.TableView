using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System;
using Windows.Foundation;

namespace WinUI.TableView.Layout;

/// <summary>
/// Lays out <see cref="TableView"/> rows inside an <see cref="ItemsRepeater"/>, realizing only the rows the
/// viewport needs.
/// </summary>
/// <remarks>
/// The vertical extent is computed from <see cref="TableViewRowHeights"/> rather than from measured content, so a
/// million-row table reports its height without touching a million rows, and the visible range is found by index
/// arithmetic instead of a scan. Rows leaving the realization window are recycled before the rows entering it are
/// realized, so the number of live row elements tracks the viewport height and not the item count, even across a
/// long jump.
/// <para>
/// Horizontal extent is computed from the column widths for the same reason: it no longer depends on which rows
/// happen to be realized, so the horizontal scrollbar stops depending on scroll position.
/// </para>
/// </remarks>
internal sealed partial class TableViewRowsLayout : VirtualizingLayout
{
    private int _firstRealizedIndex = -1;
    private int _lastRealizedIndex = -1;
    private bool _hasMeasuredARow;
    private bool _realizedIndexesMoved;

    /// <summary>
    /// Gets or sets the table this layout belongs to.
    /// </summary>
    public TableView? TableView { get; set; }

    /// <summary>
    /// Gets the row height bookkeeping backing the offset and index arithmetic.
    /// </summary>
    public TableViewRowHeights Heights { get; } = new();

    /// <summary>
    /// Gets the total height of every row, realized or not.
    /// </summary>
    public double TotalHeight => Heights.TotalHeight;

    /// <summary>
    /// Gets the visual index of the row at the given vertical offset within the rows area.
    /// </summary>
    public int GetIndexAtOffset(double offset) => Heights.GetIndexAt(offset);

    /// <summary>
    /// Gets the vertical offset of the row at the given visual index.
    /// </summary>
    public double GetOffsetOfIndex(int visualIndex) => Heights.GetOffset(visualIndex);

    /// <summary>
    /// Gets the height of the row at the given visual index.
    /// </summary>
    public double GetHeightOfIndex(int visualIndex) => Heights.GetHeight(visualIndex);

    /// <summary>
    /// Forgets every measured row height, so the next measure pass re-establishes them.
    /// </summary>
    public void ResetHeights(int rowCount)
    {
        Heights.Reset(rowCount);
        _hasMeasuredARow = false;
        _firstRealizedIndex = _lastRealizedIndex = -1;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        var itemCount = context.ItemCount;
        var rowWidth = GetRowWidth(availableSize.Width);

        Heights.Count = itemCount;
        ApplyConfiguredRowHeight();

        if (itemCount is 0)
        {
            _firstRealizedIndex = _lastRealizedIndex = -1;

            return new Size(rowWidth, 0d);
        }

        var realizationRect = context.RealizationRect;
        var first = Heights.GetIndexAt(realizationRect.Top);
        var last = Heights.GetIndexAt(realizationRect.Bottom);

        if (realizationRect.Height <= 0d)
        {
            // No viewport yet. Realizing the anchor (or the first row) lets the measured height replace the
            // estimate, so the extent is right on the very first pass instead of settling over several.
            first = last = Math.Max(0, Math.Min(itemCount - 1, context.RecommendedAnchorIndex));
        }

        first = Math.Clamp(first, 0, itemCount - 1);
        last = Math.Clamp(last, first, itemCount - 1);

        var rowMaxHeight = GetRowMaxHeight();

        RecycleRowsLeavingTheWindow(context, first, last);

        for (var index = first; index <= last; index++)
        {
            var element = context.GetOrCreateElementAt(index);
            element.Measure(new Size(rowWidth, rowMaxHeight));

            RecordMeasuredHeight(index, element);
        }

        _firstRealizedIndex = first;
        _lastRealizedIndex = last;
        _realizedIndexesMoved = false;

        return new Size(rowWidth, Heights.TotalHeight);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        if (_firstRealizedIndex < 0)
        {
            return finalSize;
        }

        // Rows span at least the viewport so their background, selection visuals and grid lines reach the edge.
        var rowWidth = Math.Max(GetRowWidth(finalSize.Width), finalSize.Width);
        var itemCount = context.ItemCount;

        for (var index = _firstRealizedIndex; index <= _lastRealizedIndex && index < itemCount; index++)
        {
            var element = context.GetOrCreateElementAt(index);

            element.Arrange(new Rect(0d, Heights.GetOffset(index), rowWidth, Heights.GetHeight(index)));
        }

        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Heights.OnRowsInserted(args.NewStartingIndex, Math.Max(1, args.NewItems?.Count ?? 1));
                break;

            case NotifyCollectionChangedAction.Remove:
                Heights.OnRowsRemoved(args.OldStartingIndex, Math.Max(1, args.OldItems?.Count ?? 1));
                break;

            default:
                ResetHeights(context.ItemCount);
                break;
        }

        // The repeater has moved the realized rows to their new indexes, so the last window no longer names them.
        _realizedIndexesMoved = true;
        InvalidateMeasure();
    }

    /// <inheritdoc/>
    protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
    {
        base.UninitializeForContextCore(context);

        _firstRealizedIndex = _lastRealizedIndex = -1;
    }

    /// <summary>
    /// Recycles the rows of the last realization window that fall outside the new one. The repeater recycles whatever
    /// a measure pass did not ask for, but only once the pass is over, so without this a jump to a distant offset
    /// builds a new row for every index it realizes while the rows it is leaving are still held.
    /// </summary>
    private void RecycleRowsLeavingTheWindow(VirtualizingLayoutContext context, int first, int last)
    {
        if (_firstRealizedIndex < 0 || _realizedIndexesMoved)
        {
            return;
        }

        var end = Math.Min(_lastRealizedIndex, context.ItemCount - 1);

        for (var index = _firstRealizedIndex; index <= end; index++)
        {
            if (index < first || index > last)
            {
                context.RecycleElement(context.GetOrCreateElementAt(index));
            }
        }
    }

    /// <summary>
    /// Records a measured row height, adopting the first data row's measurement as the common height when the
    /// table does not pin one. Doing so keeps the exception list empty in the overwhelmingly common case of uniform
    /// rows, which is what makes offset and index lookups pure arithmetic. Group headers are recorded as exceptions
    /// but never adopted, since a grouped view usually starts with one.
    /// </summary>
    private void RecordMeasuredHeight(int index, UIElement element)
    {
        var height = element.DesiredSize.Height;

        if (height <= 0d)
        {
            return;
        }

        if (!_hasMeasuredARow && element is TableViewRow && TableView is { } tableView && double.IsNaN(tableView.RowHeight))
        {
            Heights.DefaultHeight = height;
            _hasMeasuredARow = true;
        }

        Heights.SetHeight(index, height);
    }

    /// <summary>
    /// Uses the table's pinned row height, when it has one, as the common height.
    /// </summary>
    private void ApplyConfiguredRowHeight()
    {
        if (TableView is not { } tableView)
        {
            return;
        }

        if (!double.IsNaN(tableView.RowHeight))
        {
            Heights.DefaultHeight = tableView.RowHeight;
        }
        else if (!_hasMeasuredARow)
        {
            Heights.DefaultHeight = tableView.RowMinHeight;
        }
    }

    /// <summary>
    /// Gets the width every row is laid out at: the row header gutter plus the visible column widths.
    /// </summary>
    private double GetRowWidth(double availableWidth)
    {
        if (TableView is not { } tableView)
        {
            return double.IsInfinity(availableWidth) ? 0d : availableWidth;
        }

        var width = tableView.CellsHorizontalOffset;

        foreach (var column in tableView.Columns.VisibleColumns)
        {
            var columnWidth = column.ActualWidth;

            if (!double.IsNaN(columnWidth) && columnWidth > 0d)
            {
                width += columnWidth;
            }
        }

        if (double.IsNaN(width) || width <= 0d)
        {
            return double.IsInfinity(availableWidth) ? 0d : availableWidth;
        }

        return width;
    }

    /// <summary>
    /// Gets the height budget a row is measured against.
    /// </summary>
    private double GetRowMaxHeight()
    {
        if (TableView is not { } tableView)
        {
            return double.PositiveInfinity;
        }

        return double.IsNaN(tableView.RowMaxHeight) ? double.PositiveInfinity : tableView.RowMaxHeight;
    }
}
