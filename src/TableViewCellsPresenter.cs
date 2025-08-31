using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace WinUI.TableView;

/// <summary>
/// Represents a presenter for the cells in a TableView row.
/// </summary>
public partial class TableViewCellsPresenter : Control
{
    private StackPanel? _cellsStackPanel;
    private Rectangle? _v_gridLine;
    private Rectangle? _h_gridLine;
    private ContentControl? _rowHeader;

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

        _cellsStackPanel = GetTemplateChild("CellsStackPanel") as StackPanel;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Rectangle;
        _rowHeader = GetTemplateChild("RowHeaderContent") as ContentControl;

        TableViewRow = this.FindAscendant<TableViewRow>();
        TableView = TableViewRow?.TableView;

#if !WINDOWS
        TableView?.EnsureCells();
#else
        TableViewRow?.EnsureCells();
#endif
        EnsureGridLines();
        EnsureRowHeaders();
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        if (_cellsStackPanel is not null && _v_gridLine is not null && _rowHeader is not null && TableView is not null)
        {
            var height = finalSize.Height;
            var xGridLine = TableView.SelectionMode is ListViewSelectionMode.Multiple ? 44 : _rowHeader.ActualWidth;
            var xCells = -TableView.HorizontalOffset + xGridLine + _v_gridLine.ActualWidth;
            var xClip = (xCells * -1) + xGridLine + _v_gridLine.ActualWidth;

            _v_gridLine.Arrange(new Rect(xGridLine, 0, _v_gridLine.ActualWidth, height));
            _cellsStackPanel.Arrange(new Rect(xCells, 0, _cellsStackPanel.ActualWidth, height));
            _cellsStackPanel.Clip = xCells >= xGridLine + _v_gridLine.ActualWidth ? null :
                new RectangleGeometry
                {
                    Rect = new Rect(xClip, 0, _cellsStackPanel.ActualWidth - xClip, height)
                };

            if (TableView.SelectionMode is ListViewSelectionMode.Multiple)
            {
                _rowHeader.Arrange(new Rect(0, 0, 0, 0));
            }
        }

        return finalSize;
    }

    internal void EnsureRowHeaders()
    {
        if (_rowHeader is not null && TableView is not null)
        {
            if (TableView.SelectionMode is ListViewSelectionMode.Multiple)
            {
                _rowHeader.Opacity = 0;
            }
            else
            {
                _rowHeader.Opacity = 1;
                _rowHeader.Width = TableView.RowHeaderWidth;
                _rowHeader.MinWidth = TableView.RowHeaderMinWidth;
                _rowHeader.MaxWidth = TableView.RowHeaderMaxWidth;
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
