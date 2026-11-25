using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
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
    private TextBlock? _rowNumberText;
    private double _rowNumberMinWidth;
    private int _rowNumber = int.MinValue;
    private Panel? _rootPanel;
    private Panel? _scrollableCellsPanel; // a custom template may still use a StackPanel
    private Panel? _frozenCellsPanel;
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
    private TranslateTransform? _detailsPanelShift;
    private RectangleGeometry? _scrollableCellsClip;
    private TableViewCell[]? _cells;
    private readonly Dictionary<(TableViewColumn Column, int Occurrence), TableViewCell> _cellsByColumn = [];

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
        _rowNumberText = GetTemplateChild("RowNumberText") as TextBlock;
        _rowNumberMinWidth = _rowNumberText?.MinWidth ?? 0d;
        _rowNumber = int.MinValue;
        _rootPanel = GetTemplateChild("RootPanel") as Panel;
        _scrollableCellsPanel = GetTemplateChild("ScrollableCellsPanel") as Panel;
        _frozenCellsPanel = GetTemplateChild("FrozenCellsPanel") as Panel;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as FrameworkElement;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Border;
        _detailsGridLine = GetTemplateChild("DetailsGridLine") as FrameworkElement;
        _h_gridLineSpace = GetTemplateChild("HorizontalGridLineSpace") as FrameworkElement;
        _detailsPanel = GetTemplateChild("DetailsPanel") as Panel;
        _detailsPanelShift = null;
        _detailsPresenter = GetTemplateChild("DetailsPresenter") as FrameworkElement;
        _detailsToggleButton = GetTemplateChild("DetailsToggleButton") as ToggleButton;
        _indentPlaceholder = GetTemplateChild("IndentPlaceholder") as Border;
        // Cells of a template that has been replaced are in panels that are gone.
        var hadCells = _cellsByColumn.Count > 0;
        _cellsByColumn.Clear();
        _spareCells.Clear();
        _cells = null;

        TableViewRow = this.FindAscendant<TableViewRow>();
        TableView = TableViewRow?.TableView;
        (_scrollableCellsPanel as TableViewCellsPanel)?.TableView = TableView;
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

        if (hadCells)
        {
            ClearCells();
            SyncCells();
        }

        EnsureGridLines();
        SetRowHeaderHeights();
        SetRowHeaderVisibility();
        SetRowHeaderTemplate();
        SetRowHeaderWidth();
        SetRowDetailsVisibility();
        SetRowDetailsTemplate();
        SetGroupIndent();
        SetRowNumberVisibility();
        SetRowNumberWidth();
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
        UpdateGroupIndent();
    }

    /// <summary>
    /// Sets the indent to what the row number, row header and details button leave of it: they fill the indent
    /// before they push the cells further right.
    /// </summary>
    /// <returns>Whether the indent changed.</returns>
    private bool UpdateGroupIndent()
    {
        if (_indentPlaceholder is null)
        {
            return false;
        }

        var indent = GetGroupIndent();

        if (indent > 0d)
        {
            var used = (_rowNumberText?.DesiredSize.Width ?? 0d) + (_detailsToggleButton?.DesiredSize.Width ?? 0d);

            // An empty row header is as wide as its minimum, which stays in front of the indent.
            if (_rowHeader is { Visibility: Visibility.Visible })
            {
                used += Math.Max(0d, _rowHeader.DesiredSize.Width - _rowHeader.MinWidth);
            }

            indent = Math.Max(0d, indent - used);
        }

        if (_indentPlaceholder.Width.Equals(indent))
        {
            return false;
        }

        _indentPlaceholder.Width = indent;

        return true;
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

        var columns = TableView.Columns as TableViewColumnsCollection;
        var firstOffset = columns?.GetVisibleColumnOffset(Math.Clamp(TableView.FrozenColumnCount, 0, columns.VisibleColumns.Count)) ?? 0d;

        foreach (var cell in _scrollableCellsPanel.Children)
        {
            var x = (double)cell.ActualOffset.X;
            var right = x + cell.ActualSize.X;

            // A cell that has just been given its column is not arranged there yet; the column says where it will be.
            if (columns is not null
                && cell is TableViewCell { Column: { } column } columnCell
                && columns.GetVisibleColumnIndex(column, columnCell.ColumnOccurrence) is >= 0 and var index)
            {
                x = columns.GetVisibleColumnOffset(index) - firstOffset;
                right = columns.GetVisibleColumnOffset(index + 1) - firstOffset;
            }

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

        var result = base.MeasureOverride(availableSize);

        // The indent depends on the header widths just measured.
        if (UpdateGroupIndent())
        {
            result = base.MeasureOverride(availableSize);
        }

        // Rows measure their number independently; the widest one sets the width they all share.
        if (TableView is { ShowRowNumbers: true } tableView && _rowNumberText is { Visibility: Visibility.Visible })
        {
            var margin = _rowNumberText.Margin;
            var width = _rowNumberText.DesiredSize.Width - margin.Left - margin.Right;

            if (width > tableView.RowNumberColumnWidth)
            {
                tableView.SetValue(TableView.RowNumberColumnWidthProperty, width);
            }
        }

        return result;
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

            if (_detailsPanel?.Visibility is Visibility.Visible)
            {
                // Shifted, not arranged: the root panel puts the details back in their slot whenever it arranges alone.
                var shift = TableView.AreRowDetailsFrozen ? 0d : xScroll;

                if (_detailsPanelShift is null)
                {
                    _detailsPanel.RenderTransform = _detailsPanelShift = new TranslateTransform();
                }

                if (_detailsPanelShift.X != shift)
                {
                    _detailsPanelShift.X = shift;
                }

                _detailsPanel.Clip = SetClip(
                    ref _detailsPanelClip,
                    needsClip: shift < 0d,
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
    // Until the shared width is known a header takes its own content's width, so its row never shows it collapsed.
    private static double GetRowHeaderWidth(TableView tableView)
    {
        if (!double.IsNaN(tableView.RowHeaderWidth))
        {
            return tableView.RowHeaderWidth;
        }

        return tableView.RowHeaderActualWidth > 0d ? tableView.RowHeaderActualWidth : double.NaN;
    }

    internal void SetRowHeaderWidth()
    {
        if (_rowHeader is not null && TableView is not null)
        {
            _rowHeader.Width = GetRowHeaderWidth(TableView);
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

        if (!_rowHeader.Width.Equals(GetRowHeaderWidth(TableView))
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

    /// <summary>
    /// Shows the row number when <see cref="TableView.ShowRowNumbers"/> is set and row headers are visible.
    /// </summary>
    internal void SetRowNumberVisibility()
    {
        if (_rowNumberText is not null && TableView is not null)
        {
            var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var visibility = TableView.ShowRowNumbers && areHeadersVisible ? Visibility.Visible : Visibility.Collapsed;

            if (_rowNumberText.Visibility != visibility)
            {
                _rowNumberText.Visibility = visibility;
            }
        }
    }

    /// <summary>
    /// Sets the displayed row number and the row header's <see cref="FrameworkElement.Tag"/>, which a custom
    /// <see cref="TableView.RowHeaderTemplate"/> can bind to whether or not the number is shown.
    /// </summary>
    internal void SetRowNumber()
    {
        if (TableView is null || TableViewRow is null) return;

        // A pooled row missed any change made while it was released.
        SetRowNumberVisibility();
        SetRowNumberWidth();

        var rowNumber = TableViewRow.RowNumber;

        if (rowNumber == _rowNumber) return;

        _rowNumber = rowNumber;

        if (_rowHeader is not null)
        {
            _rowHeader.Tag = rowNumber;
        }

        if (_rowNumberText is not null)
        {
            _rowNumberText.Text = rowNumber > 0 ? rowNumber.ToString() : string.Empty;
        }
    }

    /// <summary>
    /// Applies the width shared by every row's number, so a wider number does not shift only its own row's cells.
    /// </summary>
    internal void SetRowNumberWidth()
    {
        if (_rowNumberText is not null && TableView is not null)
        {
            // A minimum, not a fixed width, so a number wider than the shared width still measures in full.
            var minWidth = Math.Max(_rowNumberMinWidth, TableView.RowNumberColumnWidth);

            if (_rowNumberText.MinWidth != minWidth)
            {
                _rowNumberText.MinWidth = minWidth;
            }
        }
    }

    /// <summary>
    /// Applies the table's row heights to the row header.
    /// </summary>
    internal void SetRowHeaderHeights()
    {
        if (_rowHeader is not null)
        {
            TableView?.ApplyRowHeights(_rowHeader);
        }
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
    /// Brings the cells in line with the columns: one for every frozen column, and one for every scrollable column
    /// in or near the viewport (see <see cref="TableView.RealizedColumns"/>).
    /// </summary>
    /// <remarks>
    /// A cell whose column has gone out of view is reused for a column that has come into view. A scrollable panel
    /// that is not a <see cref="TableViewCellsPanel"/>, as in a custom template, keeps a cell for every column.
    /// </remarks>
    internal void SyncCells()
    {
        if (TableView is null || TableViewRow is null || _scrollableCellsPanel is null || _frozenCellsPanel is null)
        {
            return;
        }

        var columns = (TableViewColumnsCollection)TableView.Columns;
        var visibleColumns = columns.VisibleColumns;
        var count = visibleColumns.Count;
        var frozenCount = Math.Clamp(TableView.FrozenColumnCount, 0, count);
        var (first, last) = _scrollableCellsPanel is TableViewCellsPanel ? TableView.RealizedColumns : (frozenCount, count - 1);
        var pinned = TableViewRow.GetPinnedColumnIndex();
        var changed = false;
        var frozenChanged = false;

        // Cells whose column is gone, out of view, or now on the other side of the frozen boundary.
        _leavingCells.Clear();

        foreach (var cell in Cells)
        {
            var index = cell.Column is { } column ? columns.GetVisibleColumnIndex(column, cell.ColumnOccurrence) : -1;
            var isWanted = index >= 0 && (index < frozenCount || (index >= first && index <= last) || index == pinned);

            if (!isWanted || cell.IsInFrozenPanel != (index < frozenCount))
            {
                _leavingCells.Add(cell);
            }
            else if (cell.Index != index)
            {
                cell.Index = index;
                ApplyCellState(cell); // selection and the current cell go by index
                frozenChanged |= cell.IsInFrozenPanel;
                changed = true;
            }
        }

        foreach (var cell in _leavingCells)
        {
            Forget(cell);
        }

        for (var i = 0; i < frozenCount; i++)
        {
            changed |= EnsureCell(visibleColumns[i], i, isFrozen: true, ref frozenChanged);
        }

        for (var i = Math.Max(first, frozenCount); i <= last && i < count; i++)
        {
            changed |= EnsureCell(visibleColumns[i], i, isFrozen: false, ref frozenChanged);
        }

        if (pinned >= frozenCount && pinned < count)
        {
            changed |= EnsureCell(visibleColumns[pinned], pinned, isFrozen: false, ref frozenChanged);
        }

        // Cells no column took over.
        foreach (var cell in _leavingCells)
        {
            frozenChanged |= cell.IsInFrozenPanel;
            (cell.IsInFrozenPanel ? _frozenCellsPanel : _scrollableCellsPanel).Children.Remove(cell);
            _spareCells.Add(cell);
            changed = true;
        }

        _leavingCells.Clear();
        _syncedCellCount = _cellsByColumn.Count;

        if (_spareCells.Count > MaxSpareCells)
        {
            _spareCells.RemoveRange(MaxSpareCells, _spareCells.Count - MaxSpareCells);
        }

        if (changed)
        {
            _cells = null;

            if (frozenChanged)
            {
                EnsureStackOrder(_frozenCellsPanel, isFrozen: true);
            }

            EnsureStackOrder(_scrollableCellsPanel, isFrozen: false);
            _scrollableCellsPanel.InvalidateArrange();
            InvalidateMeasure();
        }
    }

    // Cells whose column no longer needs one, while the cells are being synced.
    private readonly List<TableViewCell> _leavingCells = [];

    // Cells kept for reuse between syncs; a sideways scroll trades a few columns at a time.
    private const int MaxSpareCells = 8;
    private const int MaxCellsOnDemand = 32;
    private int _syncedCellCount;
    private readonly List<TableViewCell> _spareCells = [];

    /// <summary>
    /// Gives a column a cell if it has none: one that is leaving the same panel, where it stays, else a spare
    /// one, else a new one.
    /// </summary>
    private bool EnsureCell(TableViewColumn column, int index, bool isFrozen, ref bool frozenChanged)
    {
        var occurrence = TableView?.Columns is TableViewColumnsCollection columns ? columns.GetColumnOccurrence(index) : 0;

        if (TableViewRow is null || _cellsByColumn.ContainsKey((column, occurrence)))
        {
            return false;
        }

        for (var i = _leavingCells.Count - 1; i >= 0; i--)
        {
            var leaving = _leavingCells[i];

            if (leaving.IsInFrozenPanel == isFrozen)
            {
                _leavingCells.RemoveAt(i);
                leaving.ColumnOccurrence = occurrence;
                leaving.Reuse(column, index, wasDetached: false);
                _cellsByColumn[(column, occurrence)] = leaving;
                ApplyCellState(leaving);
                frozenChanged |= isFrozen;

                return true;
            }
        }

        TableViewCell cell;

        if (_spareCells.Count > 0)
        {
            cell = _spareCells[^1];
            _spareCells.RemoveAt(_spareCells.Count - 1);
            cell.ColumnOccurrence = occurrence;
            cell.Reuse(column, index, wasDetached: true);
        }
        else
        {
            cell = TableViewRow.CreateCell(column, index);
            cell.ColumnOccurrence = occurrence;
        }

        frozenChanged |= isFrozen;
        AttachCell(cell, isFrozen);

        return true;
    }

    private void ApplyCellState(TableViewCell cell)
    {
        cell.EnsureStyle(TableViewRow?.Content);
        cell.ApplySelectionState();
        cell.ApplyCurrentVisualState();
    }

    /// <summary>
    /// Creates the cell of a column that is out of view. It stays until the cells are next synced.
    /// </summary>
    internal TableViewCell? RealizeCell(int columnIndex)
    {
        if (TableView is null || columnIndex < 0 || columnIndex >= TableView.Columns.VisibleColumns.Count)
        {
            return null;
        }

        var column = TableView.Columns.VisibleColumns[columnIndex];

        // Cells created on demand stay until the next sync; a caller asking for column after column gets one.
        if (_cellsByColumn.Count >= _syncedCellCount + MaxCellsOnDemand)
        {
            SyncCells();
        }

        var frozenChanged = false;

        if (EnsureCell(column, columnIndex, isFrozen: columnIndex < TableView.FrozenColumnCount, ref frozenChanged))
        {
            _cells = null;
            _scrollableCellsPanel?.InvalidateArrange();
        }

        return GetCell(column, ((TableViewColumnsCollection)TableView.Columns).GetColumnOccurrence(columnIndex));
    }

    // Takes a cell out of the lookup, unless another cell has taken its place there.
    private void Forget(TableViewCell cell)
    {
        if (cell.Column is { } column
            && _cellsByColumn.TryGetValue((column, cell.ColumnOccurrence), out var current)
            && ReferenceEquals(current, cell))
        {
            _cellsByColumn.Remove((column, cell.ColumnOccurrence));
        }
    }

    private void AttachCell(TableViewCell cell, bool isFrozen)
    {
        if (cell.Column is not { } column)
        {
            return;
        }

        cell.IsInFrozenPanel = isFrozen;
        (isFrozen ? _frozenCellsPanel : _scrollableCellsPanel)?.Children.Add(cell);
        _cellsByColumn[(column, cell.ColumnOccurrence)] = cell;
        _cells = null;

        ApplyCellState(cell);
    }

    private void DetachCell(TableViewCell cell)
    {
        (cell.IsInFrozenPanel ? _frozenCellsPanel : _scrollableCellsPanel)?.Children.Remove(cell);
        Forget(cell);

        _cells = null;
    }

    /// <summary>
    /// Puts the cells of a panel that stacks its children in column order. A <see cref="TableViewCellsPanel"/>
    /// places cells by their column and needs no order.
    /// </summary>
    private void EnsureStackOrder(Panel panel, bool isFrozen)
    {
        if (panel is TableViewCellsPanel)
        {
            return;
        }

        var position = 0;
        var inOrder = true;

        foreach (var cell in Cells)
        {
            if (cell.IsInFrozenPanel == isFrozen)
            {
                if (position >= panel.Children.Count || !ReferenceEquals(panel.Children[position], cell))
                {
                    inOrder = false;
                    break;
                }

                position++;
            }
        }

        if (inOrder && position == panel.Children.Count)
        {
            return;
        }

        panel.Children.Clear();

        foreach (var cell in Cells)
        {
            if (cell.IsInFrozenPanel == isFrozen)
            {
                panel.Children.Add(cell);
            }
        }
    }

    /// <summary>
    /// Inserts a cell at the place of its column.
    /// </summary>
    /// <param name="cell">The cell to insert.</param>
    public void InsertCell(TableViewCell cell)
    {
        if (TableView is null || cell is not { Column: { } column } || _frozenCellsPanel is null || _scrollableCellsPanel is null) return;

        if (_cellsByColumn.TryGetValue((column, cell.ColumnOccurrence), out var existing))
        {
            DetachCell(existing);
        }

        AttachCell(cell, column.IsFrozen);
        EnsureStackOrder(_frozenCellsPanel, isFrozen: true);
        EnsureStackOrder(_scrollableCellsPanel, isFrozen: false);
    }

    /// <summary>
    /// Removes a cell from the presenter.
    /// </summary>
    /// <param name="cell">The cell to remove.</param>
    public void RemoveCell(TableViewCell cell)
    {
        DetachCell(cell);
    }

    /// <summary>
    /// Clears all cells from the presenter.
    /// </summary>
    public void ClearCells()
    {
        _frozenCellsPanel?.Children.Clear();
        _scrollableCellsPanel?.Children.Clear();
        _cellsByColumn.Clear();
        _spareCells.Clear();
        _cells = null;
    }

    /// <summary>
    /// Gets the list of cells in the presenter, in column order.
    /// </summary>
    /// <remarks>
    /// Only the columns in or near the viewport have cells. The list is kept until the cells change, since it is
    /// read several times for every row the table shows. A change builds a new list rather than altering this
    /// one, so a caller enumerating it is not disturbed.
    /// </remarks>
    public IReadOnlyList<TableViewCell> Cells => _cells ??= CollectCells();

    /// <summary>
    /// Gets the cell the row shows for a column, or null when it has none.
    /// </summary>
    internal TableViewCell? GetCell(TableViewColumn column, int occurrence = 0)
    {
        return _cellsByColumn.TryGetValue((column, occurrence), out var cell) ? cell : null;
    }

    /// <summary>
    /// Collects the cells in column order, which puts the frozen ones first.
    /// </summary>
    private TableViewCell[] CollectCells()
    {
        var cells = new TableViewCell[_cellsByColumn.Count];
        _cellsByColumn.Values.CopyTo(cells, 0);

        if (TableView?.Columns is TableViewColumnsCollection columns)
        {
            Array.Sort(cells, (x, y) => GetOrder(x).CompareTo(GetOrder(y)));

            int GetOrder(TableViewCell cell)
            {
                return cell.Column is { } column && columns.GetVisibleColumnIndex(column, cell.ColumnOccurrence) is >= 0 and var index ? index : int.MaxValue;
            }
        }

        return cells;
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
