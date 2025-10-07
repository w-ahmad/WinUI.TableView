using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using WinUI.TableView.Helpers;
using WinUI.TableView.Primitives;

namespace WinUI.TableView;

/// <summary>
/// Represents a row in a TableView.
/// </summary>

#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewRow : ListViewItem
{
    private const string Selection_Indictor = nameof(Selection_Indictor);
    private const string Selection_Background = nameof(Selection_Background);
    private const string Check_Mark = "\uE73E";
    private Thickness _focusVisualMargin = new(1);
    private Thickness _selectionBackgroundMargin = new(4, 2, 4, 2);

    private TableView? _tableView;
    private ListViewItemPresenter? _itemPresenter;
    private TableViewRowPresenter? _rowPresenter;
    private Border? _selectionBackground;
    private bool _ensureCells = true;
    private Brush? _cellPresenterBackground;
    private Brush? _cellPresenterForeground;

    /// <summary>
    /// Initializes a new instance of the TableViewRow class.
    /// </summary>
    public TableViewRow()
    {
        DefaultStyleKey = typeof(TableViewRow);

        SizeChanged += OnSizeChanged;
        Loaded += TableViewRow_Loaded;
#if WINDOWS
        ContextRequested += OnContextRequested;
        RegisterPropertyChangedCallback(IsSelectedProperty, delegate { OnIsSelectedChanged(); });
#endif
        RegisterPropertyChangedCallback(ForegroundProperty, delegate { OnForegroundChanged(); });
        RegisterPropertyChangedCallback(BackgroundProperty, delegate { OnBackgroundChanged(); });
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
        e.Handled = TableView?.ShowRowContext(this, position) is true;
    }

#if WINDOWS
    /// <summary>
    /// Handles the IsSelected property changed.
    /// </summary>
    private void OnIsSelectedChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (IsSelected && TableView?.SelectionMode is not ListViewSelectionMode.Multiple)
            {
                if (_itemPresenter is not null)
                {
                    var cornerRadius = _itemPresenter.CornerRadius;
                    var left = Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft) / 2;
                    var selectionIndictor = _itemPresenter.FindDescendants()
                                                          .OfType<Border>()
                                                          .FirstOrDefault(x => x is { Name: not Selection_Indictor, Width: 3 });

                    if (selectionIndictor is not null)
                    {
                        selectionIndictor.Name = Selection_Indictor;
                        selectionIndictor.Margin = new Thickness(
                            selectionIndictor.Margin.Left + left,
                            selectionIndictor.Margin.Top,
                            selectionIndictor.Margin.Right,
                            selectionIndictor.Margin.Bottom);
                    }
                }
            }
        });
    }
#endif

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
    /// Handles the Loaded event.
    /// </summary>
    private void TableViewRow_Loaded(object sender, RoutedEventArgs e)
    {
        _focusVisualMargin = FocusVisualMargin;

        EnsureGridLines();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _cellPresenterBackground = Background;
        _cellPresenterForeground = Foreground;
        _itemPresenter = GetTemplateChild("Root") as ListViewItemPresenter;
    }

    /// <inheritdoc/>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
#if WINDOWS
        if (_ensureCells)
        {
            EnsureCells();
        }
        else
        {
#endif
            foreach (var cell in Cells)
            {
                cell.RefreshElement();
            }
#if WINDOWS
        }
#endif

        RowPresenter?.InvalidateMeasure(); // The cells presenter does not measure every time.
        _tableView?.EnsureAlternateRowColors();
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        if (TableView is { IsEditing: false })
        {
            base.OnPointerPressed(e);
        }

        if (!KeyboardHelper.IsShiftKeyDown() && TableView is not null)
        {
            TableView.SelectionStartRowIndex = Index;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!KeyboardHelper.IsShiftKeyDown() && TableView is not null)
        {
            TableView.SelectionStartCellSlot = null;
            TableView.SelectionStartRowIndex = Index;
        }
    }

    /// <inheritdoc/>
    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        base.OnTapped(e);

        if (TableView?.SelectionUnit is TableViewSelectionUnit.Row or TableViewSelectionUnit.CellOrRow)
        {
            TableView.CurrentRowIndex = Index;
            TableView.LastSelectionUnit = TableViewSelectionUnit.Row;
        }
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        var cornerRadius = _itemPresenter?.CornerRadius ?? new();
        var left = Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);

        _itemPresenter?.Arrange(new Rect(-left, 0, _itemPresenter.ActualWidth + left, _itemPresenter.ActualHeight));

        return finalSize;
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

        if (RowPresenter is not null && (_ensureCells || _rowPresenter != RowPresenter))
        {
            RowPresenter.ClearCells();

            AddCells(TableView.Columns.VisibleColumns);
            _ensureCells = false;
        }
    }

    /// <summary>
    /// Handles the SizeChanged event.
    /// </summary>
    private async void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (TableView?.CurrentCellSlot?.Row == Index)
        {
            _ = await TableView.ScrollCellIntoView(TableView.CurrentCellSlot.Value);
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
            foreach (var cell in Cells)
            {
                if (cell.Column == e.Column
                    && cell.Content is FrameworkElement element
                    && cell.Column is TableViewBoundColumn boundColumn
                    && (TableView?.IsEditing is false || TableView?.CurrentCellSlot != cell.Slot))
                {
                    element.Style = boundColumn.ElementStyle;
                }
            }
        }
        else if (e.PropertyName is nameof(TableViewBoundColumn.EditingElementStyle))
        {
            if (TableView?.IsEditing is true
                && TableView.CurrentCellSlot is not null
                && e.Column is TableViewBoundColumn boundColumn
                && TableView.GetCellFromSlot(TableView.CurrentCellSlot.Value) is { } cell
                && cell.Content is FrameworkElement element)
            {
                element.Style = boundColumn.EditingElementStyle;
            }
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
                    Width = column.ActualWidth,
                    Style = column.CellStyle ?? TableView.CellStyle
                };

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

    /// <summary>
    /// Ensures the cells style is applied.
    /// </summary>
    internal void EnsureCellsStyle(TableViewColumn? column = null)
    {
        var cells = Cells.Where(x => column is null || x.Column == column);

        foreach (var cell in cells)
        {
            var style = cell.Column?.CellStyle ?? TableView?.CellStyle;
            cell.Style = style;
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
    /// Ensures grid lines are applied to the row.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (TableView is not null && _itemPresenter is not null)
        {
            var cornerRadius = _itemPresenter.CornerRadius;
            var left = Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft) / 2;
            _selectionBackground ??= _itemPresenter.FindDescendants()
                                                   .OfType<Border>()
                                                   .FirstOrDefault(x => x.Name is not Selection_Background && x.Margin == _selectionBackgroundMargin);

            FocusVisualMargin = new Thickness(
                _focusVisualMargin.Left + left,
                _focusVisualMargin.Top,
                _focusVisualMargin.Right,
                _focusVisualMargin.Bottom + TableView.HorizontalGridLinesStrokeThickness);

            if (_selectionBackground is not null)
            {
                _selectionBackground.Name = Selection_Background;
                _selectionBackground.Margin = new Thickness(
                    _selectionBackgroundMargin.Left + left,
                    _selectionBackgroundMargin.Top,
                    _selectionBackgroundMargin.Right,
                    _selectionBackgroundMargin.Bottom + TableView.HorizontalGridLinesStrokeThickness);
            }
        }

        RowPresenter?.EnsureGridLines();
    }

    /// <summary>
    /// Ensures the layout of the row.
    /// </summary>
    internal void EnsureLayout()
    {
#if !WINDOWS
        var multiSelectSquare = this.FindDescendant<Border>(x => x.Name is "MultiSelectSquare");
        if (multiSelectSquare is not null)
        {
            multiSelectSquare.Opacity = 0.5;
            multiSelectSquare.CornerRadius = new CornerRadius(4);
            multiSelectSquare.BorderThickness = new Thickness(1);
            multiSelectSquare.Margin = new Thickness(10, 0, 0, 0);
        }
#endif
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

    internal void UpdateSelectCheckMarkOpacity()
    {
        var fontIcon = this.FindDescendant<FontIcon>(x => x.Glyph == Check_Mark);

        if (fontIcon?.Parent is Border border)
        {
            border.Opacity = TableView?.IsEditing is true ? 0.3 : 1;
        }
    }

    /// <summary>
    /// Gets the list of cells in the row.
    /// </summary>
    public IReadOnlyList<TableViewCell> Cells => RowPresenter?.Cells ?? [];

    /// <summary>
    /// Gets the index of the row.
    /// </summary>
    public int Index => TableView?.IndexFromContainer(this) ?? -1;

    /// <summary>
    /// Gets or sets the TableView associated with the row.
    /// </summary>
    public TableView? TableView
    {
        get => _tableView;
        internal set
        {
            if (_tableView != value)
            {
                OnTableViewChanging();
                _tableView = value;
                OnTableViewChanged();
            }
        }
    }

    /// <inheritdoc/>
    public TableViewRowPresenter? RowPresenter
    {
        get
        {
#if WINDOWS
            _rowPresenter ??= ContentTemplateRoot as TableViewRowPresenter;
#else
            _rowPresenter ??= this.FindDescendant<TableViewRowPresenter>();
#endif
            return _rowPresenter;
        }
    }
}
