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

namespace WinUI.TableView;

/// <summary>
/// Represents a presenter for the cells in a TableView row.
/// </summary>
public partial class TableViewCellsPresenter : Control
{
    private TableViewRowHeader? _rowHeader;
    private Panel? _rootPanel;
    private ColumnDefinition? _rowHeaderColumn;
    private StackPanel? _cellsStackPanel;
    private Rectangle? _v_gridLine;
    private Rectangle? _h_gridLine;
    private TableViewRowPresenter? _rowPresenter;

    /// <summary>
    /// Initializes a new instance of the TableViewCellsPresenter class.
    /// </summary>
    public TableViewCellsPresenter()
    {
        DefaultStyleKey = typeof(TableViewCellsPresenter);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _rootPanel = GetTemplateChild("RootPanel") as Panel;
        _rowHeaderColumn = GetTemplateChild("RowHeaderColumn") as ColumnDefinition;
        _rowHeader = GetTemplateChild("RowHeader") as TableViewRowHeader;
        _cellsStackPanel = GetTemplateChild("CellsStackPanel") as StackPanel;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Rectangle;
        _rowPresenter = this.FindAscendant<TableViewRowPresenter>();
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

        if (TableView is not null && _rootPanel is not null && _cellsStackPanel is not null && _v_gridLine is not null)
        {
            var height = finalSize.Height;
            var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView.SelectionMode is ListViewSelectionMode.Multiple;
            var headerWidth = areHeadersVisible && !isMultiSelection ? TableView.RowHeaderActualWidth + _v_gridLine.ActualWidth : 0;
            var cornerRadius = _rowPresenter?.CornerRadius ?? new CornerRadius(4);
            var left = isMultiSelection ? 44 : Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);
            var xScroll = headerWidth - TableView.HorizontalOffset;
            var xClip = (xScroll * -1) + headerWidth;

            _rootPanel.Arrange(new(left, 0, _rootPanel.ActualWidth - left, finalSize.Height));
            _cellsStackPanel.Arrange(new(xScroll, 0, _cellsStackPanel.ActualWidth, height));
            _cellsStackPanel.Clip = xScroll >= headerWidth ? null :
                new RectangleGeometry
                {
                    Rect = new Rect(xClip, 0, _cellsStackPanel.ActualWidth - xClip, height)
                };

            if (isMultiSelection)
            {
                _v_gridLine.Arrange(new(0, 0, _v_gridLine.ActualWidth, height));
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
            var isMultiSelection = TableView.SelectionMode is ListViewSelectionMode.Multiple;

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
    /// Retrieves the height of the horizontal gridline.
    /// </summary>
    internal double GetHorizonalGridlineHeight()
    {
        return _h_gridLine?.ActualHeight ?? 0d;
    }

    /// <summary>
    /// Gets the collection of child elements.
    /// </summary>
    internal UIElementCollection Children => _cellsStackPanel?.Children!;

    /// <summary>
    /// Gets the list of cells in the presenter.
    /// </summary>
    public IList<TableViewCell> Cells => _cellsStackPanel?.Children.OfType<TableViewCell>().ToList()!;

    /// <summary>
    /// Gets or sets the TableViewRow associated with the presenter.
    /// </summary>
    public TableViewRow? TableViewRow { get; private set; }

    /// <summary>
    /// Gets or sets the TableView associated with the presenter.
    /// </summary>
    public TableView? TableView { get; private set; }
}
