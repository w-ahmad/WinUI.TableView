using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

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
    private FrameworkElement? _v_gridLine;
    private Border? _h_gridLine;
    private FrameworkElement? _h_gridLineSpace;
    private FrameworkElement? _detailsGridLine;
    private Panel? _detailsPanel;
    private FrameworkElement? _detailsPresenter; // not ContentPresenter: Native AOT can wrap it as a base class
    private ToggleButton? _detailsToggleButton;
    private Border? _indentPlaceholder;
    private long? _detailsPanelVisibilityCallbackToken;
    private RectangleGeometry? _detailsPanelClip;
    private RectangleGeometry? _scrollableCellsClip;
    private TableViewCell[]? _cells;

    private const double MultiSelectInset = 44d;

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

        _detailsToggleButton?.Tapped -= OnDetailsToggleButtonTapped;

        _detailsPanel?.SizeChanged -= OnDetailsPanelSizeChanged;

        if (_detailsPanelVisibilityCallbackToken is long token)
        {
            _detailsPanel?.UnregisterPropertyChangedCallback(VisibilityProperty, token);
            _detailsPanelVisibilityCallbackToken = null;
        }

        _rowHeader = GetTemplateChild("RowHeader") as TableViewRowHeader;
        _rootPanel = GetTemplateChild("RootPanel") as Panel;
        _scrollableCellsPanel = GetTemplateChild("ScrollableCellsPanel") as StackPanel;
        _frozenCellsPanel = GetTemplateChild("FrozenCellsPanel") as StackPanel;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as FrameworkElement;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Border;
        _detailsGridLine = GetTemplateChild("DetailsGridLine") as FrameworkElement;
        _h_gridLineSpace = GetTemplateChild("HorizontalGridLineSpace") as FrameworkElement;
        _detailsPanel = GetTemplateChild("DetailsPanel") as Panel;
        _detailsPresenter = GetTemplateChild("DetailsPresenter") as FrameworkElement;
        _detailsToggleButton = GetTemplateChild("DetailsToggleButton") as ToggleButton;
        _indentPlaceholder = GetTemplateChild("IndentPlaceholder") as Border;
        _cells = null;

        TableViewRow = this.FindAscendant<TableViewRow>();
        TableView = TableViewRow?.TableView;
        _rowHeader?.TableView = TableView;
        _rowHeader?.TableViewRow = TableViewRow;

        _detailsToggleButton?.Tapped += OnDetailsToggleButtonTapped;

        if (_detailsPanel is not null)
        {
            _detailsPanel.SizeChanged += OnDetailsPanelSizeChanged;
            _detailsPanelVisibilityCallbackToken =
                _detailsPanel.RegisterPropertyChangedCallback(VisibilityProperty, OnDetailsPanelVisibilityChanged);
        }

        TableViewRow?.EnsureCells();
        EnsureGridLines();
        SetRowHeaderBindings();
        SetRowHeaderVisibility();
        SetRowHeaderTemplate();
        SetRowHeaderWidth();
        SetRowDetailsVisibility();
        SetRowDetailsTemplate();
        SetGroupIndent();
        IsTemplateApplied = true;

        // A new row is prepared before this template exists, so its per-item visuals are applied now.
        if (TableViewRow is { VisualIndex: >= 0 } row)
        {
            TableView?.ApplyRowVisuals(row);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the presenter's template has been applied.
    /// </summary>
    internal bool IsTemplateApplied { get; private set; }

    /// <summary>
    /// Indents this row's cells for the active grouping depth by setting a left margin on
    /// <see cref="_frozenCellsPanel"/> - <see cref="_scrollableCellsPanel"/> follows automatically since
    /// <see cref="ArrangeOverride"/> positions it relative to the frozen panel's actual (post-margin) offset.
    /// The row header, in its own column, is unaffected. Called once per row preparation (not from
    /// ArrangeOverride, which runs on every scroll tick - mutating a child's Margin there would
    /// re-invalidate measure every pass and risks a "Layout cycle detected" crash).
    /// </summary>
    internal void SetGroupIndent()
    {
        _indentPlaceholder?.Width = GetGroupIndent();
    }

#if !WINDOWS
    /// <summary>
    /// Shows the cells in view and hides the scrollable cells scrolled out of view sideways (see
    /// <see cref="TableView.UpdateRenderCulling"/>).
    /// </summary>
    /// <param name="viewportWidth">The width of the table's viewport.</param>
    /// <param name="cullCells">Whether to hide the cells out of view, rather than show every cell.</param>
    internal void UpdateCellsRenderCulling(double viewportWidth, bool cullCells)
    {
        if (_scrollableCellsPanel is null || _frozenCellsPanel is null || TableView is null)
        {
            return;
        }

        // A cell that was hidden while it scrolled can have been moved among the frozen ones since.
        foreach (var cell in _frozenCellsPanel.Children)
        {
            SetCellInView(cell, isInView: true, isNearView: true);
        }

        // The scrollable panel is arranged at the frozen cells' right edge less the horizontal offset, and clipped there
        // (see ArrangeOverride), so in its own coordinates the part in view starts at the offset and is as wide as the
        // viewport right of the frozen cells.
        var left = TableView is { SelectionMode: ListViewSelectionMode.Multiple } ? MultiSelectInset : 0d;
        var start = TableView.HorizontalOffset;
        var end = start + viewportWidth - left - (_frozenCellsPanel.ActualOffset.X + _frozenCellsPanel.ActualWidth);

        // Content lays out just before its column scrolls into view, not on the frame it appears.
        const double LayoutLead = 200d;

        foreach (var cell in _scrollableCellsPanel.Children)
        {
            var x = cell.ActualOffset.X;
            var right = x + cell.ActualSize.X;
            SetCellInView(
                cell,
                isInView: !cullCells || (right >= start && x <= end),
                isNearView: !cullCells || (right >= start - LayoutLead && x <= end + LayoutLead));
        }

        static void SetCellInView(UIElement element, bool isInView, bool isNearView)
        {
            if (element is TableViewCell cell)
            {
                cell.UpdateRenderCulling(isInView, isNearView);
            }
            else
            {
                ElementVisuals.SetInView(element, isInView);
            }
        }
    }
#endif

    /// <summary>
    /// Handles size changes in the row details panel.
    /// </summary>
    private void OnDetailsPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        TableViewRow?.EnsureLayout();
    }

    /// <summary>
    /// Handles visibility changes in the row details panel.
    /// </summary>
    private void OnDetailsPanelVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        TableViewRow?.EnsureLayout();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // The row header's size never depends on a data column's width, but this presenter still
        // measures every visible row every frame during a Live-mode resize drag (a cell's width
        // really did change), so forcing this remeasure here too is pure per-frame waste during a drag.
        if (TableView?.IsColumnResizing != true)
        {
            _rowHeader?.InvalidateMeasure(); // The row header does not measure every time.
        }

        LimitDetailsToViewport();

        return base.MeasureOverride(availableSize);
    }

    // Rows are measured at an unbounded width; wide details stay within the viewport instead of widening the row.
    private void LimitDetailsToViewport()
    {
        if (_detailsPanel?.Visibility is not Visibility.Visible || TableView is not { RowsViewportWidth: > 0d } tableView)
        {
            return;
        }

        var cellsLeft = double.IsNaN(tableView.CellsHorizontalOffset) ? 0d : tableView.CellsHorizontalOffset;
        cellsLeft += _v_gridLine?.Visibility is Visibility.Visible ? _v_gridLine.ActualWidth : 0d;
        var maxWidth = Math.Max(0d, tableView.RowsViewportWidth - cellsLeft);

        if (_detailsPanel.MaxWidth != maxWidth)
        {
            _detailsPanel.MaxWidth = maxWidth;
        }
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        if (TableView is not null)
        {
#if !WINDOWS
            TableView.InvalidateRenderCulling();
#endif
            // Multi-select mode reserves room on the left for the row's check box.
            var isMultiSelection = TableView is { SelectionMode: ListViewSelectionMode.Multiple };
            var left = isMultiSelection ? MultiSelectInset : 0d;
            var xScroll = -TableView.HorizontalOffset;
            var xClip = TableView.HorizontalOffset;

            _rootPanel?.Arrange(new(left, 0, Math.Max(0, _rootPanel.ActualWidth), _rootPanel.ActualHeight));

            if (_detailsPanel?.Visibility is Visibility.Visible && _v_gridLine is not null)
            {
                var x = _v_gridLine.ActualOffset.X + _v_gridLine.ActualWidth;
                x += TableView.AreRowDetailsFrozen ? 0 : xScroll;
                var y = _scrollableCellsPanel?.ActualHeight ?? _v_gridLine.ActualOffset.Y;
                var width = _detailsPanel.ActualWidth;
                var height = _detailsPanel.ActualHeight;
                _detailsPanel.Arrange(new(x, y, width, height));
                _detailsPanel.Clip = SetClip(
                    ref _detailsPanelClip,
                    needsClip: x < _v_gridLine.ActualOffset.X + _v_gridLine.ActualWidth,
                    new(xClip, 0, Math.Max(0, _detailsPanel.ActualWidth - xClip), _detailsPanel.ActualHeight));
            }

            if (_scrollableCellsPanel?.ActualWidth > 0 && _frozenCellsPanel is not null)
            {
                xScroll += _frozenCellsPanel.ActualOffset.X + _frozenCellsPanel.ActualWidth;

                _scrollableCellsPanel.Arrange(new(xScroll, 0, _scrollableCellsPanel.ActualWidth, _scrollableCellsPanel.ActualHeight));
                _scrollableCellsPanel.Clip = SetClip(
                    ref _scrollableCellsClip,
                    needsClip: xScroll < _frozenCellsPanel.ActualOffset.X + _frozenCellsPanel.ActualWidth,
                    new(xClip, 0, Math.Max(0, _scrollableCellsPanel.ActualWidth - xClip), _scrollableCellsPanel.ActualHeight));
            }


            // CellsHorizontalOffset is the boundary between the row header and the data cells — it's
            // positioned purely by HeaderColumn's width (see TableViewRowPresenter.xaml's ColumnDefinitions),
            // so it never depends on any data column's width and is safe to skip recomputing (via a real
            // TransformToVisual walk, on every visible row) during a resize drag.
            if (TableView.CollectionView.Count > 0 && !TableView.IsColumnResizing && _v_gridLine is not null && _frozenCellsPanel is not null)
            {
                // Measured from the cells, so it holds when the grid line is collapsed. RootPanel is arranged at
                // x = left above, so no visual-tree walk is needed.
                var offset = left + _frozenCellsPanel.ActualOffset.X;
                offset -= _v_gridLine.Visibility is Visibility.Visible ? _v_gridLine.ActualWidth : 0d;
                offset = Math.Max(0, offset);

                // Compared with the table's value: the header row writes it too.
                if (offset != TableView.LastCellsHorizontalOffset)
                {
                    TableView.SetValue(TableView.CellsHorizontalOffsetProperty, offset);
                }
            }
        }

        return finalSize;
    }

    /// <summary>
    /// The extra <see cref="TableView.CellsHorizontalOffset"/> added for the active grouping depth, so cells (and,
    /// via that shared offset, the column headers) shift right under nested group headers. A single grouping
    /// level isn't indented - only levels after the first add <see cref="TableViewGroupHeaderRow.GroupIndentSize"/>.
    /// </summary>
    private double GetGroupIndent()
    {
        var groupLevels = (TableView?.CollectionView as CollectionView)?.GroupDescriptions.Count ?? 0;
        return Math.Max(0, groupLevels - 1) * TableViewGroupHeaderRow.GroupIndentSize;
    }

    /// <summary>
    /// Applies <paramref name="rect"/> to a cached clip geometry, or returns null when no clip is needed.
    /// The geometry is cached per element because WinUI rejects a single <see cref="RectangleGeometry"/> being
    /// used as the clip of more than one element, and because scrolling re-arranges every visible row —
    /// allocating a geometry per row per frame is pure garbage.
    /// </summary>
    private static RectangleGeometry? SetClip(ref RectangleGeometry? clip, bool needsClip, Rect rect)
    {
        if (!needsClip)
        {
            return null;
        }

        clip ??= new RectangleGeometry();
        clip.Rect = rect;

        return clip;
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
        EnsurePresenterGridLines();

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
            var state = (TableViewRow?.IsSelected ?? false) ? VisualStates.StateDetailsVisible : VisualStates.StateDetailsCollapsed;
            VisualStates.GoToState(this, false, state);
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
    /// Handles the Tapped event of the details toggle button.
    /// </summary>
    private async void OnDetailsToggleButtonTapped(object sender, TappedRoutedEventArgs e)
    {
        var isVisible = _detailsToggleButton!.IsChecked ?? false;
        ToggleDetailsPane(TableViewRow?.Content, isVisible);

        if (isVisible && TableViewRow is not null)
        {
            await TableViewRow.ScrollIntoViewAfterDetailsPaneVisibility();
        }
    }

    /// <summary>
    /// Toggles the visibility of the details pane.
    /// </summary>
    private void ToggleDetailsPane(object? content, bool isVisible)
    {
        if (TableView is null || content is null) return;

        TableView.DetailsPaneStates.AddOrUpdate(content, isVisible);
        var state = isVisible ? VisualStates.StateDetailsVisible : VisualStates.StateDetailsCollapsed;
        VisualStates.GoToState(this, false, state);
    }

    /// <summary>
    /// Ensures that the details pane visibility is synchronized for the specified item when row.
    /// </summary>
    internal void ApplyDetailsPaneState(object? item)
    {
        if (TableView?.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded &&
            _detailsToggleButton is not null && TableView is not null && item is not null)
        {
            var isChecked = TableView.DetailsPaneStates.TryGetValue(item, out var value) && value.Value;
            _detailsToggleButton!.IsChecked = isChecked;
            ToggleDetailsPane(item, isChecked);
        }
    }

    /// <summary>
    /// Sets the DataTemplate for the row details.
    /// </summary>
    internal void SetRowDetailsTemplate()
    {
        if (_detailsPresenter is not null && TableView is not null)
        {
            _detailsPresenter.SetValue(ContentPresenter.ContentTemplateProperty,
                TableView.RowDetailsTemplateSelector?.SelectTemplate(TableViewRow?.Content)
                ?? TableView.RowDetailsTemplate);
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
    /// Brings the row header's widths up to date with the table's, changing nothing when they already are.
    /// </summary>
    /// <remarks>
    /// The table tells the rows it has attached when the header width changes, so a row that is pooled when it does,
    /// as the repeater keeps the rows that leave the viewport for reuse, is not told, and would show its next item
    /// with the width it had before. A row calls this as it is prepared for an item, which is more often than the
    /// width changes, so this only touches the header when something differs.
    /// </remarks>
    internal void SyncRowHeaderWidth()
    {
        if (_rowHeader is null || TableView is null)
        {
            return;
        }

        var headerWidth = TableView.RowHeaderWidth is double.NaN ? TableView.RowHeaderActualWidth : TableView.RowHeaderWidth;

        if (_rowHeader.Width != headerWidth
            || _rowHeader.MinWidth != TableView.RowHeaderMinWidth
            || _rowHeader.MaxWidth != TableView.RowHeaderMaxWidth)
        {
            SetRowHeaderWidth();
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
            var isMultiSelection = TableView is { SelectionMode: ListViewSelectionMode.Multiple };
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

            EnsurePresenterGridLines();
        }
    }

    internal void SetRowHeaderBindings()
    {
        _rowHeader?.SetBinding(HeightProperty, SharedResources.RowHeightBinding);
        _rowHeader?.SetBinding(MaxHeightProperty, SharedResources.RowMaxHeightBinding);
        _rowHeader?.SetBinding(MinHeightProperty, SharedResources.RowMinHeightBinding);
    }

    /// <summary>
    /// Ensures grid lines are applied to the presenter and its cells.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (TableView is null) return;

        EnsurePresenterGridLines();

        foreach (var cell in Cells)
        {
            cell.EnsureGridLines();
        }
    }

    /// <summary>
    /// Ensures the presenter's own horizontal and vertical grid lines are applied, leaving the cells' alone.
    /// </summary>
    private void EnsurePresenterGridLines()
    {
        if (TableView is null) return;

        var thickness = TableView.HorizontalGridLinesStrokeThickness;
        var visibility = TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
                         ? Visibility.Visible : Visibility.Collapsed;

        // Each part is optional: a custom template may leave one out, or have another element under its name.
        if (_h_gridLine is not null)
        {
            GridLineSprite.SetBottomEdge(_h_gridLine, TableView.HorizontalGridLinesStroke, thickness);
            _h_gridLine.Visibility = visibility;
        }

        if (_h_gridLineSpace is not null)
        {
            _h_gridLineSpace.Height = thickness;
            _h_gridLineSpace.Visibility = visibility;
        }

        if (_detailsGridLine is not null)
        {
            GridLineSprite.SetShapeFill(_detailsGridLine, TableView.HorizontalGridLinesStroke);
            _detailsGridLine.Height = thickness;
            _detailsGridLine.Visibility = visibility;
        }

        if (_v_gridLine is not null)
        {
            var vGridLinesVisibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                       || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical;
            var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView is { SelectionMode: ListViewSelectionMode.Multiple };
            var isDetailsToggleButtonVisible = TableView.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded
                                                && (TableView.RowDetailsTemplate is not null || TableView.RowDetailsTemplateSelector is not null);

            GridLineSprite.SetFill(_v_gridLine, TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                                ? TableView.VerticalGridLinesStroke : SharedResources.TransparentBrush);
            _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
            _v_gridLine.Visibility = vGridLinesVisibility && (areHeadersVisible || isMultiSelection || isDetailsToggleButtonVisible) ? Visibility.Visible : Visibility.Collapsed;
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

        _cells = null;
        cell.EnsureStyle(TableViewRow?.Content);
    }

    /// <summary>
    /// Adds cells to the presenter in the order given, which must be the visible column order.
    /// </summary>
    /// <remarks>
    /// Building a row this way is linear in the number of columns, where inserting cell by cell looks each
    /// column's position up among the visible columns.
    /// </remarks>
    internal void AppendCells(IEnumerable<TableViewCell> cells)
    {
        foreach (var cell in cells)
        {
            var panel = cell.Column?.IsFrozen is true ? _frozenCellsPanel : _scrollableCellsPanel;
            panel?.Children.Add(cell);
            cell.EnsureStyle(TableViewRow?.Content);
        }

        _cells = null;
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

        _cells = null;
    }

    /// <summary>
    /// Moves the cell associated with the specified column to a new index.
    /// </summary>
    /// <param name="column">The column associated with the cell to move.</param>
    /// <param name="newIndex">The new index to move the cell to.</param>
    internal void MoveCells(TableViewColumn column, int newIndex)
    {
        if (Cells.FirstOrDefault(h => h.Column == column) is { } cell)
        {
            RemoveCell(cell);
            InsertCell(cell);
        }

        if (newIndex >= 0 && newIndex < TableView?.FrozenColumnCount &&
           _frozenCellsPanel?.Children.OfType<TableViewCell>().LastOrDefault() is { } frozenCell)
        {
            RemoveCell(frozenCell);
            InsertCell(frozenCell);
        }

        UpdateCellIndexes();
    }

    /// <summary>
    /// Updates the indexes of all cells in the presenter.
    /// </summary>
    private void UpdateCellIndexes()
    {
        if (TableView is null) return;

        foreach (var cell in Cells)
        {
            if (cell.Column is not null)
            {
                var index = TableView.Columns.VisibleColumns.IndexOf(cell.Column);
                if (cell.Index != index)
                    cell.Index = index;
            }
        }
    }

    /// <summary>
    /// Clears all cells from the presenter.
    /// </summary>
    public void ClearCells()
    {
        _frozenCellsPanel?.Children.Clear();
        _scrollableCellsPanel?.Children.Clear();
        _cells = null;
    }

    /// <summary>
    /// Gets the list of cells in the presenter.
    /// </summary>
    /// <remarks>
    /// Kept until the cells change, since it is read several times for every row the table shows. A change builds
    /// a new list rather than altering this one, so a caller enumerating it is not disturbed.
    /// </remarks>
    public IReadOnlyList<TableViewCell> Cells => _cells ??= CollectCells();

    /// <summary>
    /// Collects the cells of the frozen and then the scrollable panel, in display order.
    /// </summary>
    private TableViewCell[] CollectCells()
    {
        var cells = new List<TableViewCell>((_frozenCellsPanel?.Children.Count ?? 0) + (_scrollableCellsPanel?.Children.Count ?? 0));
        AddCellsOf(_frozenCellsPanel, cells);
        AddCellsOf(_scrollableCellsPanel, cells);

        return [.. cells];

        static void AddCellsOf(Panel? panel, List<TableViewCell> cells)
        {
            if (panel is null) return;

            foreach (var child in panel.Children)
            {
                if (child is TableViewCell cell)
                {
                    cells.Add(cell);
                }
            }
        }
    }

    /// <summary>
    /// Gets the panel hosting scrollable (non-frozen) cells. Used to shift the whole scrollable
    /// region in one shot when a frozen column is being resized, instead of shifting every
    /// scrollable cell individually.
    /// </summary>
    internal Panel? ScrollableCellsPanel => _scrollableCellsPanel;

    /// <summary>
    /// Gets or sets the TableViewRow associated with the presenter.
    /// </summary>
    public TableViewRow? TableViewRow { get; private set; }

    /// <summary>
    /// Gets or sets the TableView associated with the presenter.
    /// </summary>
    public TableView? TableView { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the row details panel is currently visible.
    /// </summary>
    internal bool IsDetailsPanelVisible => _detailsPanel?.Visibility is Visibility.Visible;

    /// <summary>
    /// Gets the realized row header element.
    /// </summary>
    internal TableViewRowHeader? RowHeader => _rowHeader;

    /// <summary>
    /// Programmatically shows or hides the details pane.
    /// Only takes effect when <see cref="TableView.RowDetailsVisibilityMode"/> is
    /// <see cref="TableViewRowDetailsVisibilityMode.VisibleWhenExpanded"/>.
    /// </summary>
    /// <param name="visible"><see langword="true"/> to expand; <see langword="false"/> to collapse.</param>
    internal async void ShowDetailPane(bool visible)
    {
        if (TableView?.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded)
        {
            if (_detailsToggleButton is not null)
            {
                _detailsToggleButton.IsChecked = visible;
            }

            ToggleDetailsPane(TableViewRow?.Content, visible);

            if (visible && TableViewRow is not null)
            {
                await TableViewRow.ScrollIntoViewAfterDetailsPaneVisibility();
            }
        }
    }
}
