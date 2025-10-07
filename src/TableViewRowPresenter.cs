using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using WinUI.TableView.Primitives;

namespace WinUI.TableView;

/// <summary>
/// Represents a control that presents visuals for the <see cref="WinUI.TableView.TableViewRow"/>.
/// </summary>
public partial class TableViewRowPresenter : Control
{
    private TableViewRowHeader? _rowHeader;
    private Panel? _rootPanel;
    private ColumnDefinition? _rowHeaderColumn;
    private StackPanel? _scrollableCellsPanel;
    private StackPanel? _frozenCellsPanel;
    private Rectangle? _v_gridLine;
    private Rectangle? _h_gridLine;
    private ListViewItemPresenter? _itemPresenter;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowPresenter"/> class.
    /// </summary>
    public TableViewRowPresenter()
    {
        DefaultStyleKey = typeof(TableViewRowPresenter);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _rootPanel = GetTemplateChild("RootPanel") as Panel;
        _rowHeaderColumn = GetTemplateChild("RowHeaderColumn") as ColumnDefinition;
        _rowHeader = GetTemplateChild("RowHeader") as TableViewRowHeader;
        _scrollableCellsPanel = GetTemplateChild("ScrollableCellsPanel") as StackPanel;
        _frozenCellsPanel = GetTemplateChild("FrozenCellsPanel") as StackPanel;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Rectangle;
        _itemPresenter = this.FindAscendant<ListViewItemPresenter>();
        TableViewRow = this.FindAscendant<TableViewRow>();
        TableView = TableViewRow?.TableView;

        if (_rowHeader is not null)
        {
            _rowHeader.TableView = TableView;
            _rowHeader.TableViewRow = TableViewRow;
        }

#if !WINDOWS
        TableView?.EnsureCells();
#else
        TableViewRow?.EnsureCells();
#endif
        EnsureGridLines();
        SetRowHeaderVisibility();
        SetRowHeaderTemplate();
        SetRowHeaderWidth();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _rowHeader?.InvalidateMeasure(); // The row header does not measure every time.
        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        if (TableView is not null && _rootPanel is not null && _scrollableCellsPanel is not null && _frozenCellsPanel is not null && _v_gridLine is not null)
        {
            var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };
            var headerWidth = areHeadersVisible && !isMultiSelection ? TableView.RowHeaderActualWidth + _v_gridLine.ActualWidth : 0;
            var cornerRadius = _itemPresenter?.CornerRadius ?? new CornerRadius(4);
            var left = isMultiSelection ? 44 : Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);
            var xScroll = headerWidth + _frozenCellsPanel.ActualWidth - TableView.HorizontalOffset;
            var xClip = (xScroll * -1) + headerWidth + _frozenCellsPanel.ActualWidth;

            _rootPanel.Arrange(new(left, 0, _rootPanel.ActualWidth - left, _rootPanel.ActualHeight));
            _frozenCellsPanel.Arrange(new(headerWidth, 0, _frozenCellsPanel.ActualWidth, _frozenCellsPanel.ActualHeight));

            if (_scrollableCellsPanel.ActualWidth > 0)
            {
                _scrollableCellsPanel.Arrange(new(xScroll, 0, _scrollableCellsPanel.ActualWidth, _scrollableCellsPanel.ActualHeight));
                _scrollableCellsPanel.Clip = xScroll >= headerWidth + _frozenCellsPanel.ActualWidth ? null :
                    new RectangleGeometry
                    {
                        Rect = new(xClip, 0, _scrollableCellsPanel.ActualWidth - xClip, _scrollableCellsPanel.ActualHeight)
                    };
            }

            if (isMultiSelection)
            {
                _v_gridLine.Arrange(new(0, 0, _v_gridLine.ActualWidth, _v_gridLine.ActualHeight));
            }
        }

        return finalSize;
    }

    /// <summary>
    /// Sets the DataTemplate for the row header.
    /// </summary>
    internal void SetRowHeaderTemplate()
    {
        if (_rowHeader is not null && TableView is not null)
        {
            _rowHeader.ContentTemplate =
                TableView.RowHeaderTemplateSelector?.SelectTemplate(TableViewRow?.Content)
                ?? TableView.RowHeaderTemplate;
        }
    }

    /// <summary>
    /// Sets the widths of the row header column.
    /// </summary>
    internal void SetRowHeaderWidth()
    {
        if (_rowHeaderColumn is not null && TableView is not null)
        {
            var headerWidth = TableView.RowHeaderWidth is double.NaN ? TableView.RowHeaderActualWidth : TableView.RowHeaderWidth;

            _rowHeaderColumn.Width = new(headerWidth);
            _rowHeaderColumn.MinWidth = TableView.RowHeaderMinWidth;
            _rowHeaderColumn.MaxWidth = TableView.RowHeaderMaxWidth;

            _rowHeader?.InvalidateMeasure();
            _rowHeader?.InvalidateArrange();
        }
    }

    /// <summary>
    /// Sets the visibility of the row header based on the TableView settings.
    /// </summary>
    internal void SetRowHeaderVisibility()
    {
        if (_rowHeader is not null && _v_gridLine is not null && _rowHeaderColumn is not null && TableView is not null)
        {
            var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };

            _v_gridLine.Visibility = areHeadersVisible || isMultiSelection ? Visibility.Visible : Visibility.Collapsed;

            if (areHeadersVisible && !isMultiSelection)
            {
                _rowHeader.Visibility = Visibility.Visible;
                SetRowHeaderWidth();
            }
            else
            {
                _rowHeaderColumn.Width = new(0);
                _rowHeaderColumn.MinWidth = 0;
                _rowHeaderColumn.MaxWidth = 0;
                _rowHeader.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// Ensures grid lines are applied to the cells.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (TableView is null) return;

        if (_h_gridLine is not null)
        {
            _h_gridLine.Fill = TableView.HorizontalGridLinesStroke;
            _h_gridLine.Height = TableView.HorizontalGridLinesStrokeThickness;
            _h_gridLine.Visibility = TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
                                     ? Visibility.Visible : Visibility.Collapsed;

            if (_v_gridLine is not null)
            {
                _v_gridLine.Fill = TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                   ? TableView.VerticalGridLinesStroke : new SolidColorBrush(Colors.Transparent);
                _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
                _v_gridLine.Visibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                         || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                         ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        foreach (var cell in Cells)
        {
            cell.EnsureGridLines();
        }
    }

    /// <summary>
    /// Inserts a cell at the specified index.
    /// </summary>
    /// <param name="cell">The cell to insert.</param>
    public void InsertCell(TableViewCell cell)
    {
        if (TableView is null || cell is not { Column: { } column }) return;

        var _frozenColumns = TableView.Columns.VisibleColumns.Where(x => x.IsFrozen).ToList();
        var _scrollableColumns = TableView.Columns.VisibleColumns.Where(x => !x.IsFrozen).ToList();

        if (cell is { Column.IsFrozen: true } && _frozenCellsPanel is not null)
        {
            var index = _frozenColumns.IndexOf(column);
            index = Math.Min(index, _frozenColumns.Count);
            index = Math.Max(index, 0); // handles -ve index;

            _frozenCellsPanel.Children.Insert(index, cell);
        }
        else if (_scrollableCellsPanel is not null)
        {
            var index = _scrollableColumns.IndexOf(column);
            index = Math.Min(index, _scrollableColumns.Count);
            index = Math.Max(index, 0); // handles -ve index;

            _scrollableCellsPanel.Children.Insert(index, cell);
        }
    }

    /// <summary>
    /// Removes a cell from the presenter.
    /// </summary>
    /// <param name="cell">The cell to remove.</param>
    public void RemoveCell(TableViewCell cell)
    {
        if (_frozenCellsPanel?.Children.Contains(cell) ?? false)
        {
            _frozenCellsPanel.Children.Remove(cell);
        }
        else if (_scrollableCellsPanel?.Children.Contains(cell) ?? false)
        {
            _scrollableCellsPanel.Children.Remove(cell);
        }
    }

    /// <summary>
    /// Clears all cells from the presenter.
    /// </summary>
    public void ClearCells()
    {
        _frozenCellsPanel?.Children.Clear();
        _scrollableCellsPanel?.Children.Clear();
    }

    /// <summary>
    /// Gets the list of cells in the presenter.
    /// </summary>
    public IReadOnlyList<TableViewCell> Cells =>
        [.. _frozenCellsPanel?.Children.OfType<TableViewCell>() ?? [],
         .. _scrollableCellsPanel?.Children.OfType<TableViewCell>() ?? []];

    /// <summary>
    /// Gets or sets the TableViewRow associated with the presenter.
    /// </summary>
    public TableViewRow? TableViewRow { get; private set; }

    /// <summary>
    /// Gets or sets the TableView associated with the presenter.
    /// </summary>
    public TableView? TableView { get; private set; }
}
