using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a row in a TableView.
/// </summary>
/// <remarks>
/// Rows are realized and recycled by the table's row host, so a row instance is reused for many different items
/// over its lifetime. Assigning <see cref="ContentControl.Content"/> is the reuse hook: it builds the cells the
/// first time and refreshes them on every subsequent reuse.
/// </remarks>
[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePointerOver, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePressed, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateSelected, GroupName = VisualStates.GroupCommon)]
public partial class TableViewRow : ContentControl
{
    private const string Selection_Background = "SelectionBackground";
    private const double Selection_IndicatorHeight = 16d;
    private readonly Thickness _selectionBackgroundMargin = new(4, 2, 4, 2);
    private readonly Thickness _selectionIndicatorMargin = new(4, 0, 0, 0);
    private Border? _selectionBackground;
    private Border? _selectionIndicator;
    private bool _ensureCells = true;
    private bool _isPointerOver;
    private bool _isPressed;
    private bool _animateSelection = true;
    private bool _isApplyingSelection;
    private Brush? _cellPresenterBackground;
    private Brush? _cellPresenterForeground;

    /// <summary>
    /// Identifies the <see cref="IsSelected"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(TableViewRow), new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>
    /// Initializes a new instance of the TableViewRow class.
    /// </summary>
    public TableViewRow()
    {
        DefaultStyleKey = typeof(TableViewRow);

        Loaded += TableViewRow_Loaded;
#if WINDOWS
        ContextRequested += OnContextRequested;
#endif
        RegisterPropertyChangedCallback(ForegroundProperty, delegate { OnForegroundChanged(); });
        RegisterPropertyChangedCallback(BackgroundProperty, delegate { OnBackgroundChanged(); });
    }

    /// <summary>
    /// Gets or sets a value indicating whether the row is selected.
    /// </summary>
    /// <remarks>
    /// Setting it selects or deselects the row's item in the table, under the table's
    /// <see cref="TableView.SelectionMode"/> and <see cref="TableView.SelectionUnit"/>; a change the table refuses
    /// is reverted.
    /// </remarks>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets the index of the row within the TableView's items.
    /// </summary>
    /// <remarks>
    /// An item index, so it is unaffected by group header rows and by groups being collapsed. Assigned by the row
    /// host as the row is prepared, rather than looked up through a container generator on every read.
    /// </remarks>
    public int Index { get; internal set; } = -1;

    /// <summary>
    /// Gets the index of the row within the flattened visual row sequence, or -1 when the row is not realized.
    /// </summary>
    internal int VisualIndex { get; set; } = -1;

    /// <summary>
    /// Gets the row's 1-based number as shown in the UI. When <see cref="TableView.ShowRowNumbers"/> is
    /// enabled, this is the row's real, stable position among all rows - unaffected by other rows being
    /// hidden by a collapsed group - rather than <see cref="Index"/>, which excludes them.
    /// </summary>
    public int RowNumber => TableView is { ShowRowNumbers: true } tableView ? tableView.GetRealRowIndex(this) + 1 : Index + 1;

    /// <summary>
    /// Gets the list of cells in the row.
    /// </summary>
    public IReadOnlyList<TableViewCell> Cells => RowPresenter?.Cells ?? [];

    /// <summary>
    /// Gets the cell the row shows for a column, or null when it has none.
    /// </summary>
    internal TableViewCell? GetCell(TableViewColumn column)
    {
        return RowPresenter?.GetCell(column);
    }

    /// <summary>
    /// Gets the cell the row shows for the visible column at an index, or null when it has none.
    /// </summary>
    internal TableViewCell? GetCell(int columnIndex)
    {
        return TableView?.Columns is TableViewColumnsCollection { VisibleColumns: { } columns } collection && columnIndex >= 0 && columnIndex < columns.Count
            ? RowPresenter?.GetCell(columns[columnIndex], collection.GetColumnOccurrence(columnIndex))
            : null;
    }

    /// <summary>
    /// Gets the cell for the visible column at an index, creating it when its column is out of view.
    /// </summary>
    /// <remarks>The cell stays until the row next syncs its cells, unless it is the current cell.</remarks>
    internal TableViewCell? GetOrRealizeCell(int columnIndex)
    {
        return GetCell(columnIndex) ?? RowPresenter?.RealizeCell(columnIndex);
    }

    /// <summary>
    /// Brings the row's cells in line with the columns that are in view.
    /// </summary>
    internal void SyncCells()
    {
        HasStaleCells = false;

        if (!_ensureCells)
        {
            RowPresenter?.SyncCells();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the row was out of view when the columns in view changed, and
    /// still has the cells of the columns that were in view before.
    /// </summary>
    internal bool HasStaleCells { get; set; }

    /// <summary>
    /// Gets the visible-column index of the cell the row keeps wherever it is scrolled to, or -1.
    /// </summary>
    /// <remarks>The current cell holds the keyboard focus, and the editing element when it is being edited.</remarks>
    internal int GetPinnedColumnIndex()
    {
        return TableView?.CurrentCellSlot is { } slot && slot.Row == Index && Index >= 0 ? slot.Column : -1;
    }

    /// <summary>
    /// Gets the presenter hosting the row's cells, row header and details pane.
    /// </summary>
    public TableViewRowPresenter? RowPresenter { get; private set; }

    /// <summary>
    /// Gets or sets the TableView associated with the row.
    /// </summary>
    public TableView? TableView
    {
        get;
        internal set
        {
            if (field != value)
            {
                OnTableViewChanging();
                field = value;
                OnTableViewChanged();
            }
        }
    }

#if !WINDOWS
    /// <inheritdoc/>
    protected override void OnRightTapped(RightTappedRoutedEventArgs e)
    {
        base.OnRightTapped(e);

        var position = e.GetPosition(this);
#else
    /// <summary>
    /// Handles the ContextRequested event.
    /// </summary>
    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs e)
    {
        if (!e.TryGetPosition(sender, out var position)) return;
#endif

        // Select the row before showing the Context Menu
        if (TableView is not null && TableView.ForceRowOrCellSelectionOnContextRequested && !IsSelected)
        {
            TableView.MakeSelection(new TableViewCellSlot(Index, -1), false);
        }

        e.Handled = TableView?.ShowRowContext(this, position) is true;
    }

    /// <summary>
    /// Tells assistive technology that the row's selection changed, which the list item base class used to do.
    /// </summary>
    internal void NotifySelectionChanged(bool isOnlySelection)
    {
#if WINDOWS
        // A row has a peer only once an automation client has asked for it.
        if (FrameworkElementAutomationPeer.FromElement(this) is not { } peer)
        {
            return;
        }

        var isSelected = IsSelected;
        var automationEvent = !isSelected ? AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection
            : isOnlySelection ? AutomationEvents.SelectionItemPatternOnElementSelected
            : AutomationEvents.SelectionItemPatternOnElementAddedToSelection;

        peer.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, !isSelected, isSelected);

        if (AutomationPeer.ListenerExists(automationEvent))
        {
            peer.RaiseAutomationEvent(automationEvent);
        }
#endif
    }

    /// <summary>
    /// Sets <see cref="IsSelected"/> for the row.
    /// </summary>
    /// <remarks>
    /// Preparing a row for an item shows the selection that item already has, so <paramref name="animate"/> is false
    /// there: unlike a selection the user just made, it must not bring the row's details into view.
    /// </remarks>
    internal void SetSelectionState(bool isSelected, bool animate)
    {
        _animateSelection = animate;
        _isApplyingSelection = true;

        try
        {
            IsSelected = isSelected;
        }
        finally
        {
            _animateSelection = true;
            _isApplyingSelection = false;
        }
    }

    /// <summary>
    /// Brings this row into view once the row details animation has had time to run. Skips the scroll if
    /// the container was detached or recycled for different content while waiting.
    /// </summary>
    internal async Task ScrollIntoViewAfterDetailsPaneVisibility()
    {
        var content = Content;
        var index = Index;

        await Task.Delay(20);

        if (TableView is not null && index >= 0 && Index == index && object.ReferenceEquals(Content, content))
        {
            UpdateLayout(); // the row is only as tall as its details once they are laid out
            await TableView.ScrollRowIntoView(index);
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _cellPresenterBackground = Background;
        _cellPresenterForeground = Foreground;
        RowPresenter = GetTemplateChild("RowPresenter") as TableViewRowPresenter;
        _selectionBackground = GetTemplateChild(Selection_Background) as Border;
        _selectionIndicator = GetTemplateChild("SelectionIndicator") as Border;

        ApplyRowHeights();

        UpdateVisualStates(useTransitions: false);
    }

    /// <inheritdoc/>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (_ensureCells)
        {
            EnsureCells();
        }
        else
        {
            foreach (var cell in Cells)
            {
                // Defensively resync width on reuse — a recycled container can otherwise keep a
                // stale Width if it missed a Column.ActualWidth change while off-screen (e.g. an
                // auto-width recalculation triggered by a sort), leaving cells misaligned with headers.
                if (cell.Column is not null)
                {
                    cell.Width = cell.Column.ActualWidth;
                }

                cell.RefreshElement();
#if !WINDOWS
                cell.ReleaseDeferredHeight(); // held for the item the row showed before
#endif
            }
        }

        RowPresenter?.InvalidateMeasure(); // The cells presenter does not measure every time.
    }

    /// <inheritdoc/>
    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        e.Handled = RaiseDoubleTapped();

        base.OnDoubleTapped(e);
    }

    /// <summary>
    /// Raises <see cref="TableView.RowDoubleTapped"/> and returns whether a handler took it.
    /// </summary>
    internal bool RaiseDoubleTapped()
    {
        var eventArgs = new TableViewRowDoubleTappedEventArgs(Index, this, Content);
        TableView?.OnRowDoubleTapped(eventArgs);

        return eventArgs.Handled;
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);

        _isPointerOver = true;
        UpdateVisualStates();
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        ResetPointerState();
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        _isPressed = true;
        UpdateVisualStates();

        e.Handled = TableView?.OnAnyPointerPressed(this, e) ?? false;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        _isPressed = false;
        UpdateVisualStates();

        TableView?.EndDragSelection();
    }

    /// <inheritdoc/>
    protected override void OnPointerCanceled(PointerRoutedEventArgs e)
    {
        base.OnPointerCanceled(e);

        ResetPointerState();
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        // Without this, a lost capture (alt-tab, touch cancel) left the row stuck looking pressed and left the
        // table stuck in drag selection.
        ResetPointerState();
        TableView?.EndDragSelection();
    }

    /// <summary>
    /// Clears the pointer state and repaints.
    /// </summary>
    internal void ResetPointerState()
    {
        _isPressed = false;
        _isPointerOver = false;
        UpdateVisualStates();
    }

    /// <summary>
    /// Brings the row's visual states in line with its selection, pointer and the table's selection mode, without
    /// transitions.
    /// </summary>
    internal void RefreshVisualStates()
    {
        UpdateVisualStates(useTransitions: false);
    }

    /// <summary>
    /// Gets or sets the table's selection mode version the row's layout was last brought in line with.
    /// </summary>
    internal int SelectionModeVersion { get; set; }

    /// <summary>
    /// Drives the row's visual states. <c>SelectorItem</c> used to do this; now that the row is a plain
    /// <see cref="ContentControl"/> it owns the state machine.
    /// </summary>
    private void UpdateVisualStates(bool useTransitions = true)
    {
        var state = (IsSelected, _isPressed, _isPointerOver) switch
        {
            (true, true, _) => VisualStates.StatePressedSelected,
            (true, _, true) => VisualStates.StatePointerOverSelected,
            (true, _, _) => VisualStates.StateSelected,
            (_, true, _) => VisualStates.StatePressed,
            (_, _, true) => VisualStates.StatePointerOver,
            _ => VisualStates.StateNormal
        };

        VisualStates.GoToState(this, useTransitions, state);
        VisualStates.GoToState(this, useTransitions, (IsSelected, _isPressed) switch
        {
            (true, true) => VisualStates.StateSelectionIndicatorPressed,
            (true, _) => VisualStates.StateSelectionIndicatorShown,
            _ => VisualStates.StateSelectionIndicatorHidden
        });
        VisualStates.GoToState(this, useTransitions, IsEnabled ? VisualStates.StateEnabled : VisualStates.StateDisabled);
        VisualStates.GoToState(this, useTransitions, TableView is { SelectionMode: ListViewSelectionMode.Multiple }
            ? VisualStates.StateMultiSelectEnabled
            : VisualStates.StateMultiSelectDisabled);
    }

    /// <summary>
    /// Clears the per-item state so the row can be reused for a different item.
    /// </summary>
    /// <remarks>
    /// <see cref="IsSelected"/> is left alone: the row is given its next item's selection when it is prepared, and
    /// clearing it here would replay the selection visuals of every selected row that scrolls back in.
    /// </remarks>
    internal void PrepareForRecycle()
    {
        _isPointerOver = false;
        _isPressed = false;
        Index = -1;
        VisualIndex = -1;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the row is in the table's list of realized rows.
    /// </summary>
    internal bool IsAttached { get; set; }

    /// <summary>
    /// Handles the Loaded event.
    /// </summary>
    private void TableViewRow_Loaded(object sender, RoutedEventArgs e)
    {
        RowPresenter?.EnsureGridLines();
        EnsureLayout();
    }

    /// <summary>
    /// Handles the Foreground property changed.
    /// </summary>
    private void OnForegroundChanged()
    {
        _cellPresenterForeground = Foreground;
        EnsureAlternateColors();
    }

    /// <summary>
    /// Handles the Background property changed.
    /// </summary>
    private void OnBackgroundChanged()
    {
        _cellPresenterBackground = Background;
        EnsureAlternateColors();
    }

    /// <summary>
    /// Ensures cells are created for the row.
    /// </summary>
    internal void EnsureCells()
    {
        if (TableView is null)
        {
            return;
        }

        if (RowPresenter is not null && _ensureCells)
        {
            RowPresenter.ClearCells();
            RowPresenter.SyncCells();
            _ensureCells = false;
        }
    }

    /// <summary>
    /// Handles the collection changed event for the columns.
    /// </summary>
    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncCells();
    }

    /// <summary>
    /// Handles the property changed event for a column.
    /// </summary>
    private void OnColumnPropertyChanged(object? sender, TableViewColumnPropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TableViewColumn.Visibility) or nameof(TableViewColumn.Order) or nameof(TableViewColumn.IsFrozen))
        {
            SyncCells();
        }
        else if (e.PropertyName is nameof(TableViewColumn.ActualWidth))
        {
            if (GetCell(e.Column) is { } cell)
            {
                cell.Width = e.Column.ActualWidth;
            }

            if (TableView?.Columns is TableViewColumnsCollection { HasDuplicateColumns: true })
            {
                foreach (var other in Cells)
                {
                    if (other.Column == e.Column && other.ColumnOccurrence > 0)
                    {
                        other.Width = e.Column.ActualWidth;
                    }
                }
            }
        }
        else if (e.PropertyName is nameof(TableViewColumn.IsReadOnly))
        {
            UpdateCellsState();
        }
        else if (e.PropertyName is nameof(TableViewColumn.CellStyle))
        {
            EnsureCellsStyle(e.Column);
        }
        else if (e.PropertyName is nameof(TableViewBoundColumn.ElementStyle))
        {
            EnsureElementStyle(e.Column);
        }
        else if (e.PropertyName is nameof(TableViewBoundColumn.EditingElementStyle))
        {
            EnsureEditingElementStyle(e.Column);
        }
        else if (e.PropertyName is nameof(TableViewDateColumn.DateFormat) or nameof(TableViewTimeColumn.ClockIdentifier))
        {
            foreach (var cell in Cells)
            {
                if (cell.Column == e.Column)
                {
                    e.Column.UpdateElementFormat(cell);
                }
            }
        }
    }

    /// <summary>
    /// Creates the cell this row shows for a column.
    /// </summary>
    internal TableViewCell CreateCell(TableViewColumn column, int index)
    {
        var cell = new TableViewCell
        {
            Row = this,
            Column = column,
            TableView = TableView,
            Index = index,
            Width = column.ActualWidth
        };

        TableView?.ApplyRowHeights(cell);

        return cell;
    }

    /// <summary>
    /// Applies the table's row heights to the row's cells, row header and selection indicator area.
    /// </summary>
    private void ApplyRowHeights()
    {
        if (TableView is not { } tableView)
        {
            return;
        }

        if (GetTemplateChild("HeaderGrid") is FrameworkElement headerGrid)
        {
            tableView.ApplyRowHeights(headerGrid);
        }

        if (RowPresenter is { } presenter)
        {
            presenter.SetRowHeaderHeights();

            foreach (var cell in presenter.Cells)
            {
                tableView.ApplyRowHeights(cell);
            }
        }
    }

    /// <summary>
    /// Handles the TableView changing event.
    /// </summary>
    private void OnTableViewChanging()
    {
        if (TableView is not null)
        {
            TableView.IsReadOnlyChanged -= OnTableViewIsReadOnlyChanged;
            TableView.RowHeightsChanged -= ApplyRowHeights;

            if (TableView.Columns is not null)
            {
                TableView.Columns.CollectionChanged -= OnColumnsCollectionChanged;
                TableView.Columns.ColumnPropertyChanged -= OnColumnPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Handles the TableView changed event.
    /// </summary>
    private void OnTableViewChanged()
    {
        if (TableView is not null)
        {
            TableView.IsReadOnlyChanged += OnTableViewIsReadOnlyChanged;
            TableView.RowHeightsChanged += ApplyRowHeights;

            if (TableView.Columns is not null)
            {
                TableView.Columns.CollectionChanged += OnColumnsCollectionChanged;
                TableView.Columns.ColumnPropertyChanged += OnColumnPropertyChanged;
            }

            SyncCellsWithColumns();
        }
    }

    /// <summary>
    /// Brings the cells up to date with the columns. A row without a TableView does not hear about
    /// column changes, so a recycled row can come back with cells for other columns or other widths.
    /// </summary>
    private void SyncCellsWithColumns()
    {
        if (TableView?.Columns is null || RowPresenter is null || _ensureCells)
        {
            return;
        }

        RowPresenter.SyncCells();

        foreach (var cell in RowPresenter.Cells)
        {
            if (cell.Column is { } column && cell.Width != column.ActualWidth)
            {
                cell.Width = column.ActualWidth;
            }
        }
    }

    /// <summary>
    /// Handles the IsReadOnly property changed event for the TableView.
    /// </summary>
    private void OnTableViewIsReadOnlyChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateCellsState();
    }

    /// <summary>
    /// Updates the state of the cells.
    /// </summary>
    private void UpdateCellsState()
    {
        foreach (var cell in Cells)
        {
            cell.UpdateElementState();
        }
    }

    private void EnsureElementStyle(TableViewColumn column)
    {
        foreach (var cell in Cells)
        {
            if (cell.Column == column
                && cell.Content is FrameworkElement element
                && cell.Column is TableViewBoundColumn boundColumn
                && (TableView?.IsEditing is false || TableView?.CurrentCellSlot != cell.Slot))
            {
                cell.SetElementStyle(element, boundColumn.ElementStyle ?? boundColumn.DefaultElementStyle);
            }
        }
    }

    private void EnsureEditingElementStyle(TableViewColumn column)
    {
        if (TableView?.IsEditing is true
            && TableView.CurrentCellSlot is not null
            && column is TableViewBoundColumn boundColumn
            && TableView.GetCellFromSlot(TableView.CurrentCellSlot.Value) is { } cell
            && cell.Column == column
            && cell.Content is FrameworkElement element)
        {
            element.Style = boundColumn.EditingElementStyle;
        }
    }

    /// <summary>
    /// Ensures the cells style is applied.
    /// </summary>
    internal void EnsureCellsStyle(TableViewColumn? column = null, object? dataItem = null)
    {
        var item = dataItem ?? Content;

        foreach (var cell in Cells)
        {
            if (column is null || cell.Column == column)
            {
                cell.EnsureStyle(item);
            }
        }
    }

    /// <summary>
    /// Applies the current cell state to the specified slot.
    /// </summary>
    internal void ApplyCurrentCellState(TableViewCellSlot slot)
    {
        GetCell(slot.Column)?.ApplyCurrentCellState();
    }

    /// <summary>
    /// Brings every cell's selected and current visual state up to date, without moving focus.
    /// </summary>
    /// <remarks>
    /// Used while the row is prepared for an item, which happens inside a layout pass, where moving focus is not
    /// allowed. Covers every cell rather than only the current one, so a row that was in the pool while the current
    /// cell moved does not keep showing it.
    /// </remarks>
    internal void ApplyCellStates()
    {
        foreach (var cell in Cells)
        {
            cell.ApplySelectionState();
            cell.ApplyCurrentVisualState();
        }
    }

    /// <summary>
    /// Applies the selection state to the cells.
    /// </summary>
    internal void ApplyCellsSelectionState()
    {
        foreach (var cell in Cells)
        {
            cell.ApplySelectionState();
        }
    }

    /// <summary>
    /// Ensures the layout of the row.
    /// </summary>
    /// <remarks>
    /// Sizes the selection chrome around the details pane and the horizontal grid line. This used to find those
    /// visuals by shape-matching inside the native <c>ListViewItemPresenter</c>; they are named template parts now.
    /// </remarks>
    internal void EnsureLayout()
    {
        var detailsHeight = RowPresenter?.GetDetailsContentHeight() ?? 0d;
        var gridLineHeight = GetHorizontalGridlineHeight();

        if (_selectionIndicator is not null)
        {
            var cellsHeight = ActualHeight - detailsHeight;
            _selectionIndicator.MaxHeight = Math.Max(Selection_IndicatorHeight, cellsHeight - 40);
            _selectionIndicator.Margin = _selectionIndicatorMargin;
        }

        if (_selectionBackground is not null)
        {
            _selectionBackground.Margin = new Thickness(
                _selectionBackgroundMargin.Left,
                _selectionBackgroundMargin.Top,
                _selectionBackgroundMargin.Right,
                _selectionBackgroundMargin.Bottom + gridLineHeight + detailsHeight);
        }
    }

    /// <summary>
    /// Ensures alternate colors are applied to the row.
    /// </summary>
    internal void EnsureAlternateColors()
    {
        if (TableView is null || RowPresenter is null) return;

        RowPresenter.Background =
            Index % 2 == 1 && TableView.AlternateRowBackground is not null ? TableView.AlternateRowBackground : _cellPresenterBackground;

        RowPresenter.Foreground =
            Index % 2 == 1 && TableView.AlternateRowForeground is not null ? TableView.AlternateRowForeground : _cellPresenterForeground;
    }

    /// <summary>
    /// Gets the height of the horizontal gridlines.
    /// </summary>
    private double GetHorizontalGridlineHeight()
    {
        return TableView?.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
            ? TableView.HorizontalGridLinesStrokeThickness : 0d;
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewRow row)
        {
            if (!row._isApplyingSelection && row.IsAttached && row.TableView is { } tableView && row.Index >= 0)
            {
                var isSelected = tableView.SetRowSelectedFromRow(row.Index, row.IsSelected);

                if (isSelected != row.IsSelected)
                {
                    row.SetSelectionState(isSelected, animate: false);
                    return;
                }
            }

            row.UpdateVisualStates(useTransitions: row._animateSelection);

            if (row.IsSelected)
            {
                row.EnsureLayout();
            }

            // Only this mode's details depend on the selection.
            if (row.TableView?.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenSelected)
            {
                row.RowPresenter?.SetRowDetailsVisibility();
            }

            if (row._animateSelection
                && row.TableView?.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenSelected
                && (row.RowPresenter?.IsDetailsPanelVisible ?? false))
            {
                _ = row.ScrollIntoViewAfterDetailsPaneVisibility();
            }
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AutomationPeers.TableViewRowAutomationPeer(this);
    }
}
