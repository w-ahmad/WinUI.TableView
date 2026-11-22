using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace WinUI.TableView;

/// <summary>
/// Lays out a row's scrollable cells, each at the place of its column.
/// </summary>
/// <remarks>
/// The panel is as wide as the scrollable columns together, whichever cells it holds, and a cell's place comes from
/// its column and not from the cells before it.
/// </remarks>
public partial class TableViewCellsPanel : Panel
{
    /// <summary>
    /// Gets or sets the table whose columns place the cells.
    /// </summary>
    internal TableView? TableView { get; set; }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var childSize = new Size(double.PositiveInfinity, availableSize.Height);
        var stackedWidth = 0d;
        var height = 0d;

        foreach (var child in Children)
        {
            child.Measure(childSize);
            stackedWidth += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(Math.Max(stackedWidth, GetColumnsWidth()), height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = TableView?.Columns as TableViewColumnsCollection;
        var firstOffset = columns?.GetVisibleColumnOffset(GetFirstScrollableIndex(columns)) ?? 0d;
        var stackedX = 0d;

        foreach (var child in Children)
        {
            var width = child.DesiredSize.Width;
            var x = stackedX;

            // Anything that is not the cell of a visible column follows the element before it.
            if (columns is not null
                && child is TableViewCell { Column: { } column }
                && columns.GetVisibleColumnIndex(column) is >= 0 and var index)
            {
                x = columns.GetVisibleColumnOffset(index) - firstOffset;
            }

            child.Arrange(new Rect(x, 0d, width, finalSize.Height));
            stackedX = x + width;
        }

        return finalSize;
    }

    /// <summary>
    /// Gets the width the scrollable columns take together.
    /// </summary>
    private double GetColumnsWidth()
    {
        return TableView?.Columns is TableViewColumnsCollection columns
            ? columns.GetVisibleColumnOffset(columns.VisibleColumns.Count) - columns.GetVisibleColumnOffset(GetFirstScrollableIndex(columns))
            : 0d;
    }

    private int GetFirstScrollableIndex(TableViewColumnsCollection? columns)
    {
        return columns is null ? 0 : Math.Clamp(TableView?.FrozenColumnCount ?? 0, 0, columns.VisibleColumns.Count);
    }
}
