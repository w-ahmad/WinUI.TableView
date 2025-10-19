using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace WinUI.TableView;

/// <summary>
/// Represents a control that presents visuals for the <see cref="WinUI.TableView.TableViewRow"/>.
/// </summary>
[TemplateVisualState(Name = VisualStates.StateDetailsVisible, GroupName = VisualStates.GroupRowDetails)]
[TemplateVisualState(Name = VisualStates.StateDetailsCollapsed, GroupName = VisualStates.GroupRowDetails)]
[TemplateVisualState(Name = VisualStates.StateDetailsButtonVisible, GroupName = VisualStates.GroupRowDetailsButton)]
[TemplateVisualState(Name = VisualStates.StateDetailsButtonCollapsed, GroupName = VisualStates.GroupRowDetailsButton)]
public partial class TableViewRowPresenter : Control
{
    private TableViewRowHeader? _rowHeader;
    private Panel? _rootPanel;
    private StackPanel? _scrollableCellsPanel;
    private StackPanel? _frozenCellsPanel;
    private Rectangle? _v_gridLine;
    private Rectangle? _h_gridLine;
    private Panel? _detailsPanel;
    private ContentPresenter? _detailsPresenter;
    private ToggleButton? _detailsToggleButton;
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

        _rowHeader = GetTemplateChild("RowHeader") as TableViewRowHeader;
        _rootPanel = GetTemplateChild("RootPanel") as Panel;
        _scrollableCellsPanel = GetTemplateChild("ScrollableCellsPanel") as StackPanel;
        _frozenCellsPanel = GetTemplateChild("FrozenCellsPanel") as StackPanel;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Rectangle;
        _detailsPanel = GetTemplateChild("DetailsPanel") as Panel;
        _detailsPresenter = GetTemplateChild("DetailsPresenter") as ContentPresenter;
        _detailsToggleButton = GetTemplateChild("DetailsToggleButton") as ToggleButton;

        _itemPresenter = this.FindAscendant<ListViewItemPresenter>();
        TableViewRow = this.FindAscendant<TableViewRow>();
        TableView = TableViewRow?.TableView;

        if (_rowHeader is not null)
        {
            _rowHeader.TableView = TableView;
            _rowHeader.TableViewRow = TableViewRow;
        }

        if (_detailsToggleButton is not null)
        {
            _detailsToggleButton.Checked += OnDetailsToggleButtonChecked;
            _detailsToggleButton.Unchecked += OnDetailsToggleButtonUnChecked;
        }

        if (_detailsPanel is not null)
        {
            _detailsPanel.SizeChanged += (_, _) => TableViewRow?.EnsureLayout();
            _detailsPanel.RegisterPropertyChangedCallback(VisibilityProperty, (_, _)
                => TableViewRow?.EnsureLayout());
        }

#if !WINDOWS
        TableView?.EnsureCells();
#else
        TableViewRow?.EnsureCells();
#endif
        EnsureGridLines();
        SetRowHeaderBindings();
        SetRowHeaderVisibility();
        SetRowHeaderTemplate();
        SetRowHeaderWidth();
        SetRowDetailsVisibility();
        SetRowDetailsTemplate();
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

        if (TableView is not null)
        {
            var cornerRadius = _itemPresenter?.CornerRadius ?? new CornerRadius(4);
            var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };
            var left = isMultiSelection ? 44 : Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);
            var xScroll = -TableView.HorizontalOffset;
            var xClip = TableView.HorizontalOffset;

            _rootPanel?.Arrange(new(left, 0, Math.Max(0, ActualWidth - left), _rootPanel.ActualHeight));

            if (_detailsPanel?.Visibility is Visibility.Visible && _v_gridLine is not null)
            {
                var x = _v_gridLine.ActualOffset.X + _v_gridLine.ActualWidth;
                x += TableView.AreRowDetailsFrozen ? 0 : xScroll;
                var y = _scrollableCellsPanel?.ActualHeight ?? _v_gridLine.ActualOffset.Y;
                var width = _detailsPanel.ActualWidth;
                var height = _detailsPanel.ActualHeight;
                _detailsPanel.Arrange(new(x, y, width, height));
                _detailsPanel.Clip = x >= _v_gridLine.ActualOffset.X + _v_gridLine.ActualWidth ? null :
                    new RectangleGeometry
                    {
                        Rect = new(xClip, 0, _detailsPanel.ActualWidth - xClip, _detailsPanel.ActualHeight)
                    };
            }

            if (_scrollableCellsPanel?.ActualWidth > 0 && _frozenCellsPanel is not null)
            {
                xScroll += _frozenCellsPanel.ActualOffset.X + _frozenCellsPanel.ActualWidth;

                _scrollableCellsPanel.Arrange(new(xScroll, 0, _scrollableCellsPanel.ActualWidth, _scrollableCellsPanel.ActualHeight));
                _scrollableCellsPanel.Clip = xScroll >= _frozenCellsPanel.ActualOffset.X + _frozenCellsPanel.ActualWidth ? null :
                    new RectangleGeometry
                    {
                        Rect = new(xClip, 0, _scrollableCellsPanel.ActualWidth - xClip, _scrollableCellsPanel.ActualHeight)
                    };
            }


            if (_v_gridLine is not null && TableView is not null)
            {
                var transform = _v_gridLine.TransformToVisual(this);
                var relativePosition = transform.TransformPoint(new Point(0, 0));
                var offset = _v_gridLine.Visibility is Visibility.Visible ? relativePosition.X : 0d;
                offset -= Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);

                TableView.SetValue(TableView.CellsHorizontalOffsetProperty, Math.Max(0, offset));
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

        SetRowHeaderVisibility();
    }

    /// <summary>
    /// Sets the visibility of the row details based on the <see cref="TableView.RowDetailsVisibilityMode"/>.
    /// </summary>
    internal void SetRowDetailsVisibility()
    {
        EnsureGridLines();

        var mode = TableView?.RowDetailsVisibilityMode;
        var hasTemplate = TableView?.RowDetailsTemplate is not null || TableView?.RowDetailsTemplateSelector is not null;

        if (!hasTemplate)
        {
            VisualStates.GoToState(this, false, VisualStates.StateDetailsCollapsed);
            VisualStates.GoToState(this, false, VisualStates.StateDetailsButtonCollapsed);
        }
        else if (mode is TableViewRowDetailsVisibilityMode.Visible)
        {
            VisualStates.GoToState(this, false, VisualStates.StateDetailsVisible);
            VisualStates.GoToState(this, false, VisualStates.StateDetailsButtonCollapsed);
        }
        else if (mode is TableViewRowDetailsVisibilityMode.VisibleWhenSelected)
        {
            if (_detailsToggleButton is not null)
                _detailsToggleButton.IsChecked = TableViewRow?.IsSelected ?? false;
            else
                VisualStates.GoToState(this, false, (TableViewRow?.IsSelected ?? false) ? VisualStates.StateDetailsVisible : VisualStates.StateDetailsCollapsed);

            VisualStates.GoToState(this, false, VisualStates.StateDetailsButtonCollapsed);
        }
        else if (mode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded)
        {
            VisualStates.GoToState(this, false, VisualStates.StateDetailsButtonVisible);
        }
        else
        {
            VisualStates.GoToState(this, false, VisualStates.StateDetailsCollapsed);
            VisualStates.GoToState(this, false, VisualStates.StateDetailsButtonCollapsed);
        }
    }

    /// <summary>
    /// Handles the Checked event of the details toggle button.
    /// </summary>
    private void OnDetailsToggleButtonChecked(object sender, RoutedEventArgs e)
    {
        VisualStates.GoToState(this, false, VisualStates.StateDetailsVisible);
    }

    /// <summary>
    /// Handles the Unchecked event of the details toggle button.
    /// </summary>
    private void OnDetailsToggleButtonUnChecked(object sender, RoutedEventArgs e)
    {
        VisualStates.GoToState(this, false, VisualStates.StateDetailsCollapsed);
    }

    /// <summary>
    /// Sets the DataTemplate for the row details.
    /// </summary>
    internal void SetRowDetailsTemplate()
    {
        if (_detailsPresenter is not null && TableView is not null)
        {
            _detailsPresenter.ContentTemplate =
                TableView.RowDetailsTemplateSelector?.SelectTemplate(TableViewRow?.Content)
                ?? TableView.RowDetailsTemplate;
        }
    }

    /// <summary>
    /// Sets the widths of the row header column.
    /// </summary>
    internal void SetRowHeaderWidth()
    {
        if (_rowHeader is not null && TableView is not null)
        {
            var headerWidth = TableView.RowHeaderWidth is double.NaN ? TableView.RowHeaderActualWidth : TableView.RowHeaderWidth;

            _rowHeader.Width = headerWidth;
            _rowHeader.MinWidth = TableView.RowHeaderMinWidth;
            _rowHeader.MaxWidth = TableView.RowHeaderMaxWidth;

            _rowHeader?.InvalidateMeasure();
            _rowHeader?.InvalidateArrange();
        }
    }

    /// <summary>
    /// Sets the visibility of the row header based on the TableView settings.
    /// </summary>
    internal void SetRowHeaderVisibility()
    {
        if (_rowHeader is not null && TableView is not null)
        {
            var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };
            var isDetailsToggleButtonVisible = TableView.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded
                                               && (TableView.RowDetailsTemplate is not null || TableView.RowDetailsTemplateSelector is not null);

            if (areHeadersVisible && !isMultiSelection &&
               (!isDetailsToggleButtonVisible || TableView.RowHeaderTemplate is not null || TableView.RowHeaderTemplateSelector is not null))
            {
                _rowHeader.Visibility = Visibility.Visible;
                SetRowHeaderWidth();
            }
            else
            {
                _rowHeader.Visibility = Visibility.Collapsed;
            }

            EnsureGridLines();
        }
    }

    internal void SetRowHeaderBindings()
    {
        _rowHeader?.SetBinding(HeightProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRowHeader.TableView)}.{nameof(TableView.RowHeight)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });

        _rowHeader?.SetBinding(MaxHeightProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRowHeader.TableView)}.{nameof(TableView.RowMaxHeight)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });

        _rowHeader?.SetBinding(MinHeightProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRowHeader.TableView)}.{nameof(TableView.RowMinHeight)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });
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
                var vGridLinesVisibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                           || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical;
                var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
                var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };
                var isDetailsToggleButtonVisible = TableView.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded
                                                    && (TableView.RowDetailsTemplate is not null || TableView.RowDetailsTemplateSelector is not null);

                _v_gridLine.Fill = TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                   ? TableView.VerticalGridLinesStroke : new SolidColorBrush(Colors.Transparent);
                _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
                _v_gridLine.Visibility = vGridLinesVisibility && (areHeadersVisible || isMultiSelection || isDetailsToggleButtonVisible) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        foreach (var cell in Cells)
        {
            cell.EnsureGridLines();
        }
    }

    internal double GetDetailsContentHeight()
    {
        return _detailsPanel?.Visibility is Visibility.Visible ? _detailsPanel.ActualHeight : 0d;
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
