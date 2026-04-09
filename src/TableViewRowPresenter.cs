using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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
    private ToggleButton? _hierarchyToggleButton;
    private bool _isUpdatingHierarchyToggle;

    private static readonly DependencyProperty OriginalCellLeftPaddingProperty =
        DependencyProperty.RegisterAttached(
            "OriginalCellLeftPadding",
            typeof(double),
            typeof(TableViewRowPresenter),
            new PropertyMetadata(double.NaN));
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
        _hierarchyToggleButton = GetTemplateChild("HierarchyToggleButton") as ToggleButton;

        _itemPresenter = this.FindAscendant<ListViewItemPresenter>();
        TableViewRow = this.FindAscendant<TableViewRow>();
        TableView = TableViewRow?.TableView;

        // On Windows, GetTemplateChild("RowPresenter") on TableViewRow always returns null because
        // TableViewRowPresenter is rendered via ItemTemplate inside ListViewItemPresenter, not as a
        // named part of TableViewRow's ControlTemplate. Self-register so the back-pointer is valid.
        TableViewRow?.SetRowPresenter(this);

        if (_rowHeader is not null)
        {
            _rowHeader.TableView = TableView;
            _rowHeader.TableViewRow = TableViewRow;
        }

        if (_hierarchyToggleButton is not null)
        {
            _hierarchyToggleButton.Checked -= OnHierarchyToggleButtonChanged;
            _hierarchyToggleButton.Unchecked -= OnHierarchyToggleButtonChanged;
            _hierarchyToggleButton.Checked += OnHierarchyToggleButtonChanged;
            _hierarchyToggleButton.Unchecked += OnHierarchyToggleButtonChanged;
        }

        if (_detailsToggleButton is not null)
        {
            _detailsToggleButton.Tapped += OnDetailsToggleButtonTapped;
        }

        if (_detailsPanel is not null)
        {
            _detailsPanel.SizeChanged += (_, _) => TableViewRow?.EnsureLayout();
            _detailsPanel.RegisterPropertyChangedCallback(VisibilityProperty, (_, _)
                => TableViewRow?.EnsureLayout());
        }

        // Defer EnsureCells so it runs after both OnApplyTemplate methods have fired.
        // TableViewRow.OnApplyTemplate fires first and sets _rowPresenter, but
        // _scrollableCellsPanel in this presenter is only set during THIS method.
        // A deferred call ensures both are available when cells are created and inserted.
        DispatcherQueue?.TryEnqueue(() => TableViewRow?.EnsureCells());
        EnsureGridLines();
        SetRowHeaderBindings();
        SetRowHeaderVisibility();
        SetRowHeaderTemplate();
        SetRowHeaderWidth();
        SetRowDetailsVisibility();
        SetRowDetailsTemplate();
        UpdateHierarchyPresentation();

        // Deferred retry: handles the case where Content or hierarchy level was not
        // available yet when OnApplyTemplate fired (timing gap between item assignment
        // and visual-tree connection).
        DispatcherQueue?.TryEnqueue(UpdateHierarchyPresentation);
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
            var cornerRadius = _itemPresenter?.CornerRadius ?? new CornerRadius(0);
            var isMultiSelection = TableView is ListView { SelectionMode: ListViewSelectionMode.Multiple };
            var left = isMultiSelection ? 44 : Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);
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
                _detailsPanel.Clip = x >= _v_gridLine.ActualOffset.X + _v_gridLine.ActualWidth ? null :
                    new RectangleGeometry
                    {
                        Rect = new(xClip, 0, Math.Max(0, _detailsPanel.ActualWidth - xClip), _detailsPanel.ActualHeight)
                    };
            }

            if (_scrollableCellsPanel?.ActualWidth > 0 && _frozenCellsPanel is not null)
            {
                xScroll += _frozenCellsPanel.ActualOffset.X + _frozenCellsPanel.ActualWidth;

                _scrollableCellsPanel.Arrange(new(xScroll, 0, _scrollableCellsPanel.ActualWidth, _scrollableCellsPanel.ActualHeight));
                _scrollableCellsPanel.Clip = xScroll >= _frozenCellsPanel.ActualOffset.X + _frozenCellsPanel.ActualWidth ? null :
                    new RectangleGeometry
                    {
                        Rect = new(xClip, 0, Math.Max(0, _scrollableCellsPanel.ActualWidth - xClip), _scrollableCellsPanel.ActualHeight)
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
    private void OnDetailsToggleButtonTapped(object sender, TappedRoutedEventArgs e)
    {
        ToggleDetailsPane(TableViewRow?.Content, _detailsToggleButton!.IsChecked ?? false);
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
            var isChecked = TableView.DetailsPaneStates.TryGetValue(item, out var value) ? value.Value : false;
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

        cell.EnsureStyle(TableViewRow?.Content);
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

    /// <summary>
    /// Gets a value indicating whether the row details panel is currently visible.
    /// </summary>
    internal bool IsDetailsPanelVisible => _detailsPanel?.Visibility is Visibility.Visible;

    /// <summary>
    /// Updates the hierarchy indent and expander button state for this row.
    /// </summary>
    internal void UpdateHierarchyPresentation()
    {
        if (TableView is null)
        {
            return;
        }

        if (!TableView.IsHierarchicalEnabled)
        {
            // Reset everything when hierarchy mode is turned off.
            if (_hierarchyToggleButton is not null)
            {
                _hierarchyToggleButton.Visibility = Visibility.Collapsed;
            }
            SetFirstCellHierarchyMargin(0);
            return;
        }

        if (_hierarchyToggleButton is null)
        {
            return;
        }

        var item = TableViewRow?.Content;
        var level = TableView.GetHierarchyLevel(item);
        var hasChildren = TableView.HasChildItems(item);
        var isExpanded = TableView.IsItemExpanded(item);
        var indent = level * TableView.HierarchyIndent;

        // Position the toggle button at the indent level, overlaying the start of the first cell.
        // Keep it in the visual tree (Visible) but transparent for leaf nodes so that all rows
        // at the same level reserve identical space, keeping the cell content aligned.
        _isUpdatingHierarchyToggle = true;
        _hierarchyToggleButton.Visibility = Visibility.Visible;
        _hierarchyToggleButton.Opacity = hasChildren ? 1.0 : 0.0;
        _hierarchyToggleButton.IsHitTestVisible = hasChildren;
        _hierarchyToggleButton.Margin = new Thickness(indent, 0, 0, 0);
        // Pin width to HierarchyIndent so it exactly fills the space reserved in the cell margin.
        _hierarchyToggleButton.Width = TableView.HierarchyIndent;
        _hierarchyToggleButton.IsChecked = isExpanded;

        // Force the visual state even when IsChecked didn't change (e.g. recycled row
        // already had IsChecked=false) so the glyph always reflects the correct state.
        VisualStateManager.GoToState(_hierarchyToggleButton, isExpanded ? "Checked" : "Unchecked", useTransitions: false);
        _isUpdatingHierarchyToggle = false;

        AutomationProperties.SetName(
            _hierarchyToggleButton,
            isExpanded ? TableViewLocalizedStrings.CollapseRow : TableViewLocalizedStrings.ExpandRow);

        // Offset the first data cell so its content doesn't overlap the toggle button.
        // Reserve one HierarchyIndent-wide slot for the toggle regardless of level so
        // that leaf and parent rows at the same level start their text at the same x position.
        SetFirstCellHierarchyMargin(indent + TableView.HierarchyIndent);
    }

    private void OnHierarchyToggleButtonChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingHierarchyToggle || TableViewRow?.Content is null || TableView is null || _hierarchyToggleButton is null)
        {
            return;
        }

        TableView.SetItemExpanded(TableViewRow.Content, _hierarchyToggleButton.IsChecked is true);
    }

    private void SetFirstCellHierarchyMargin(double leftMargin)
    {
        FrameworkElement? firstCell = null;

        if (_frozenCellsPanel?.Children.Count > 0)
            firstCell = _frozenCellsPanel.Children[0] as FrameworkElement;
        else if (_scrollableCellsPanel?.Children.Count > 0)
            firstCell = _scrollableCellsPanel.Children[0] as FrameworkElement;

        // Use Padding (not Margin) so only the cell's *content* is indented.
        // Margin would shift all subsequent cells right relative to their column headers.
        if (firstCell is Control cell)
        {
            // Capture the original left padding the first time we touch this cell so that
            // subsequent calls (e.g. on recycle with a different indent level) always add
            // the hierarchy margin on top of the template-defined padding rather than
            // accumulating it on top of a previously modified value.
            var originalLeft = (double)cell.GetValue(OriginalCellLeftPaddingProperty);
            if (double.IsNaN(originalLeft))
            {
                originalLeft = cell.Padding.Left;
                cell.SetValue(OriginalCellLeftPaddingProperty, originalLeft);
            }

            cell.Padding = new Thickness(originalLeft + leftMargin, cell.Padding.Top, cell.Padding.Right, cell.Padding.Bottom);
        }
    }
}
