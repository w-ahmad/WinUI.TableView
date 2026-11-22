using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace WinUI.TableView;

/// <summary>
/// Lays out a row's scrollable cells, or the scrollable column headers, each at the place of its column.
/// </summary>
/// <remarks>
/// The panel is as wide as the scrollable columns together, whichever cells it holds, and a cell's place comes from
/// its column and not from the cells before it. An element that is wider or narrower than its column, such as the
/// header of a column that is being resized, moves the ones after it by the difference.
/// </remarks>
public partial class TableViewCellsPanel : Panel
{
    private readonly List<(int Index, UIElement Element)> _placed = [];

    /// <summary>
    /// Gets or sets the table whose columns place the cells.
    /// </summary>
    internal TableView? TableView { get; set; }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var columns = TableView?.Columns as TableViewColumnsCollection;
        var childSize = new Size(double.PositiveInfinity, availableSize.Height);
        var stackedWidth = 0d;
        var shift = 0d;
        var height = 0d;

        foreach (var child in Children)
        {
            child.Measure(childSize);
            stackedWidth += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);

            if (columns is not null && GetColumn(child) is { } column && columns.GetVisibleColumnIndex(column) >= 0)
            {
                shift += child.DesiredSize.Width - GetWidth(column);
            }
        }

        return new Size(columns is null ? stackedWidth : Math.Max(0d, GetColumnsWidth(columns) + shift), height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = TableView?.Columns as TableViewColumnsCollection;
        var firstOffset = columns?.GetVisibleColumnOffset(GetFirstScrollableIndex(columns)) ?? 0d;
        var stackedX = 0d;

        _placed.Clear();

        foreach (var child in Children)
        {
            if (columns is not null && GetColumn(child) is { } column && columns.GetVisibleColumnIndex(column) is >= 0 and var index)
            {
                _placed.Add((index, child));
            }
            else
            {
                // Anything that is not the element of a visible column follows the element before it.
                child.Arrange(new Rect(stackedX, 0d, child.DesiredSize.Width, finalSize.Height));
                stackedX += child.DesiredSize.Width;
            }
        }

        _placed.Sort(static (x, y) => x.Index.CompareTo(y.Index));

        var shift = 0d;

        foreach (var (index, child) in _placed)
        {
            var width = child.DesiredSize.Width;
            child.Arrange(new Rect(columns!.GetVisibleColumnOffset(index) - firstOffset + shift, 0d, width, finalSize.Height));
            shift += width - GetWidth(columns.VisibleColumns[index]);
        }

        _placed.Clear();

        return finalSize;
    }

    private static TableViewColumn? GetColumn(UIElement element)
    {
        return element switch
        {
            TableViewCell cell => cell.Column,
            TableViewColumnHeader header => header.Column,
            _ => null
        };
    }

    private static double GetWidth(TableViewColumn column)
    {
        return column.ActualWidth is > 0d and var width ? width : 0d;
    }

    /// <summary>
    /// Gets the width the scrollable columns take together.
    /// </summary>
    private double GetColumnsWidth(TableViewColumnsCollection columns)
    {
        return columns.GetVisibleColumnOffset(columns.VisibleColumns.Count) - columns.GetVisibleColumnOffset(GetFirstScrollableIndex(columns));
    }

    private int GetFirstScrollableIndex(TableViewColumnsCollection? columns)
    {
        return columns is null ? 0 : Math.Clamp(TableView?.FrozenColumnCount ?? 0, 0, columns.VisibleColumns.Count);
    }
}
