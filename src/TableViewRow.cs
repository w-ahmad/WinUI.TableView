using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace WinUI.TableView;

/// <summary>
/// Represents a row in a TableView.
/// </summary>
/// <remarks>
/// Rows are realized and recycled by the table's row host, so a row instance is reused for many different items
/// over its lifetime. Assigning <see cref="ContentControl.Content"/> is the reuse hook: it builds the cells the
/// first time and refreshes them on every subsequent reuse.
/// </remarks>
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
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
    /// The table's selection model is the source of truth; this reflects it for the row's visuals. Setting it
    /// directly changes only the visual state, not the table's selection.
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
    /// Gets the list of cells in the row.
    /// </summary>
    public IReadOnlyList<TableViewCell> Cells => RowPresenter?.Cells ?? [];

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

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _cellPresenterBackground = Background;
        _cellPresenterForeground = Foreground;
        RowPresenter = GetTemplateChild("RowPresenter") as TableViewRowPresenter;
        _selectionBackground = GetTemplateChild(Selection_Background) as Border;
        _selectionIndicator = GetTemplateChild("SelectionIndicator") as Border;

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
            }
        }

        RowPresenter?.InvalidateMeasure(); // The cells presenter does not measure every time.
    }

    /// <inheritdoc/>
    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        var eventArgs = new TableViewRowDoubleTappedEventArgs(Index, this, Content);
        TableView?.OnRowDoubleTapped(eventArgs);
        e.Handled = eventArgs.Handled;

        base.OnDoubleTapped(e);
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

        _isPointerOver = false;
        _isPressed = false;
        UpdateVisualStates();
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        _isPressed = true;
        UpdateVisualStates();

        TableView?.OnAnyPointerPressed(this, e);
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
    private void ResetPointerState()
    {
        _isPressed = false;
        _isPointerOver = false;
        UpdateVisualStates();
    }

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
        VisualStates.GoToState(this, useTransitions, IsEnabled ? VisualStates.StateEnabled : VisualStates.StateDisabled);
        VisualStates.GoToState(this, useTransitions, TableView is { SelectionMode: ListViewSelectionMode.Multiple }
            ? VisualStates.StateMultiSelectEnabled
            : VisualStates.StateMultiSelectDisabled);
    }

    /// <summary>
    /// Clears the per-item state so the row can be reused for a different item.
    /// </summary>
    internal void PrepareForRecycle()
    {
        _isPointerOver = false;
        _isPressed = false;
        IsSelected = false;
        Index = -1;
        VisualIndex = -1;
    }

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

            AddCells(TableView.Columns.VisibleColumns);
            _ensureCells = false;
        }
    }

    /// <summary>
    /// Handles the collection changed event for the columns.
    /// </summary>
    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> newItems)
        {
            AddCells(newItems.Where(x => x.Visibility == Visibility.Visible));
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> oldItems)
        {
            RemoveCells(oldItems);
        }
        else if (e.Action == NotifyCollectionChangedAction.Move && e.NewItems?.Count > 0)
        {
            RowPresenter?.MoveCells(e.NewItems.OfType<TableViewColumn>().First(), e.NewStartingIndex);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset && RowPresenter is not null)
        {
            RowPresenter.ClearCells();
        }
    }

    /// <summary>
    /// Handles the property changed event for a column.
    /// </summary>
    private void OnColumnPropertyChanged(object? sender, TableViewColumnPropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TableViewColumn.Visibility))
        {
            if (e.Column.Visibility == Visibility.Visible)
            {
                AddCells([e.Column]);
            }
            else
            {
                RemoveCells([e.Column]);
            }
        }
        else if ((e.PropertyName is nameof(TableViewColumn.Order) ||
            e.PropertyName is nameof(TableViewColumn.IsFrozen)) &&
            e.Column.Visibility is Visibility.Visible)
        {
            RemoveCells([e.Column]);
            AddCells([e.Column]);
        }
        else if (e.PropertyName is nameof(TableViewColumn.ActualWidth))
        {
            if (Cells.FirstOrDefault(x => x.Column == e.Column) is { } cell)
            {
                cell.Width = e.Column.ActualWidth;
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
    }

    /// <summary>
    /// Removes cells for the specified columns.
    /// </summary>
    private void RemoveCells(IEnumerable<TableViewColumn> columns)
    {
        if (RowPresenter is not null)
        {
            foreach (var column in columns)
            {
                var cell = RowPresenter.Cells.FirstOrDefault(x => x.Column == column);
                if (cell is not null)
                {
                    RowPresenter.RemoveCell(cell);
                }
            }
        }
    }

    /// <summary>
    /// Adds cells for the specified columns.
    /// </summary>
    private void AddCells(IEnumerable<TableViewColumn> columns)
    {
        if (RowPresenter is not null && TableView is not null)
        {
            foreach (var column in columns)
            {
                var cell = new TableViewCell
                {
                    Row = this,
                    Column = column,
                    TableView = TableView,
                    Index = TableView.Columns.VisibleColumns.IndexOf(column),
                    Width = column.ActualWidth
                };

                cell.SetBinding(HeightProperty, new Binding
                {
                    Path = new PropertyPath($"{nameof(TableViewCell.TableView)}.{nameof(TableView.RowHeight)}"),
                    RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
                });

                cell.SetBinding(MaxHeightProperty, new Binding
                {
                    Path = new PropertyPath($"{nameof(TableViewCell.TableView)}.{nameof(TableView.RowMaxHeight)}"),
                    RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
                });

                cell.SetBinding(MinHeightProperty, new Binding
                {
                    Path = new PropertyPath($"{nameof(TableViewCell.TableView)}.{nameof(TableView.RowMinHeight)}"),
                    RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
                });

                RowPresenter.InsertCell(cell);
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

            if (TableView.Columns is not null)
            {
                TableView.Columns.CollectionChanged += OnColumnsCollectionChanged;
                TableView.Columns.ColumnPropertyChanged += OnColumnPropertyChanged;
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
                element.Style = boundColumn.ElementStyle;
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
        var cells = Cells.Where(x => column is null || x.Column == column);

        foreach (var cell in cells)
        {
            cell.EnsureStyle(dataItem ?? Content);
        }
    }

    /// <summary>
    /// Applies the current cell state to the specified slot.
    /// </summary>
    internal void ApplyCurrentCellState(TableViewCellSlot slot)
    {
        if (slot.Column >= 0 && slot.Column < Cells.Count)
        {
            var cell = Cells[slot.Column];
            cell.ApplyCurrentCellState();
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
            row.UpdateVisualStates();
            row.EnsureLayout();
            row.RowPresenter?.SetRowDetailsVisibility();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AutomationPeers.TableViewRowAutomationPeer(this);
    }
}
