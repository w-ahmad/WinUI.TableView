using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a row in a TableView.
/// </summary>
public partial class TableViewRow : ListViewItem
{
    private const string Selection_Indictor = nameof(Selection_Indictor);
    private const string Selection_Background = nameof(Selection_Background);
    private Thickness _focusVisualMargin = new(1);
    private Thickness _selectionBackgroundMargin = new(4, 2, 4, 2);

    private TableView? _tableView;
    private ListViewItemPresenter? _itemPresenter;
    private TableViewCellsPresenter? _cellPresenter;
    private bool _ensureCells = true;
    private Border? _selectionBackground;
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
        ContextRequested += OnContextRequested;
        RegisterPropertyChangedCallback(IsSelectedProperty, delegate { OnIsSelectedChanged(); });
    }

    /// <summary>
    /// Handles the ContextRequested event.
    /// </summary>
    private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (args.TryGetPosition(sender, out var position))
        {
            if (IsContextRequestedFromCell(position) && TableView?.CellContextFlyout is not null) return;

            TableView?.ShowRowContext(this, position);
        }
    }

    /// <summary>
    /// Determines if the context request is from a cell.
    /// </summary>
    private bool IsContextRequestedFromCell(Windows.Foundation.Point position)
    {
        if (_cellPresenter is null) return false;

        var transform = _cellPresenter.TransformToVisual(this).Inverse;
        var point = transform.TransformPoint(position);
        var transformedPoint = _cellPresenter.TransformToVisual(null).TransformPoint(point);
        return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, _cellPresenter)
                               .OfType<TableViewCell>()
                               .Any();
    }

    /// <summary>
    /// Handles the IsSelected property changed event.
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
                    var left = Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);
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

    /// <summary>
    /// Handles the Loaded event.
    /// </summary>
    private void TableViewRow_Loaded(object sender, RoutedEventArgs e)
    {
        _focusVisualMargin = FocusVisualMargin;

        EnsureGridLines();
        EnsureLayout();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemPresenter = GetTemplateChild("Root") as ListViewItemPresenter;

        if (_itemPresenter is not null)
        {
            var cornerRadius = _itemPresenter.CornerRadius;
            var left = Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);
            var right = Math.Max(cornerRadius.TopRight, cornerRadius.BottomRight);

            _itemPresenter.Margin = new Thickness(
                _itemPresenter.Margin.Left - left,
                _itemPresenter.Margin.Top,
                _itemPresenter.Margin.Right - right,
                _itemPresenter.Margin.Bottom);
        }
    }

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
                cell.RefreshElement();
            }
        }

        _tableView?.EnsureAlternateRowColors();
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!KeyboardHelper.IsShiftKeyDown() && TableView is not null)
        {
            TableView.SelectionStartRowIndex = Index;
        }
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!KeyboardHelper.IsShiftKeyDown() && TableView is not null)
        {
            TableView.SelectionStartCellSlot = null;
            TableView.SelectionStartRowIndex = Index;
        }
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        base.OnTapped(e);

        if (TableView?.SelectionUnit is TableViewSelectionUnit.Row or TableViewSelectionUnit.CellOrRow)
        {
            TableView.CurrentRowIndex = Index;
            TableView.LastSelectionUnit = TableViewSelectionUnit.Row;
        }
    }

    /// <summary>
    /// Ensures cells are created for the row.
    /// </summary>
    private void EnsureCells()
    {
        if (TableView is null)
        {
            return;
        }

        _cellPresenter = ContentTemplateRoot as TableViewCellsPresenter;

        if (_cellPresenter is not null)
        {
            _cellPresenterBackground = _cellPresenter.Background;
            _cellPresenterForeground = _cellPresenter.Foreground;
            _cellPresenter.Children.Clear();

            AddCells(TableView.Columns.VisibleColumns);
        }

        _ensureCells = false;
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
            AddCells(newItems.Where(x => x.Visibility == Visibility.Visible), e.NewStartingIndex);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> oldItems)
        {
            RemoveCells(oldItems);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset && _cellPresenter is not null)
        {
            _cellPresenter.Children.Clear();
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
                AddCells([e.Column], e.Index);
            }
            else
            {
                RemoveCells([e.Column]);
            }
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
        if (_cellPresenter is not null)
        {
            foreach (var column in columns)
            {
                var cell = _cellPresenter.Children.OfType<TableViewCell>().FirstOrDefault(x => x.Column == column);
                if (cell is not null)
                {
                    _cellPresenter.Children.Remove(cell);
                }
            }
        }
    }

    /// <summary>
    /// Adds cells for the specified columns.
    /// </summary>
    private void AddCells(IEnumerable<TableViewColumn> columns, int index = -1)
    {
        if (_cellPresenter is not null && TableView is not null)
        {
            foreach (var column in columns)
            {
                var cell = new TableViewCell
                {
                    Row = this,
                    Column = column,
                    TableView = TableView!,
                    Index = TableView.Columns.VisibleColumns.IndexOf(column),
                    Width = column.ActualWidth,
                    Style = column.CellStyle ?? TableView.CellStyle
                };

                index = Math.Min(index, _cellPresenter.Children.Count);
                index = Math.Max(index, 0); // handles -ve index.
                _cellPresenter.Children.Insert(index, cell);
                index++;
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
            var left = Math.Max(cornerRadius.TopLeft, cornerRadius.BottomLeft);
            var right = Math.Max(cornerRadius.TopRight, cornerRadius.BottomRight);
            _selectionBackground ??= _itemPresenter.FindDescendants()
                                                   .OfType<Border>()
                                                   .FirstOrDefault(x => x.Name is not Selection_Background && x.Margin == _selectionBackgroundMargin);

            if (_selectionBackground is not null)
            {
                FocusVisualMargin = new Thickness(
                    _focusVisualMargin.Left + left,
                    _focusVisualMargin.Top,
                    _focusVisualMargin.Right + right,
                    _focusVisualMargin.Bottom + TableView.HorizontalGridLinesStrokeThickness);

                _selectionBackground.Name = Selection_Background;
                _selectionBackground.Margin = new Thickness(
                    _selectionBackgroundMargin.Left + left,
                    _selectionBackgroundMargin.Top,
                    _selectionBackgroundMargin.Right + right,
                    _selectionBackgroundMargin.Bottom + TableView.HorizontalGridLinesStrokeThickness);
            }
        }

        _cellPresenter?.EnsureGridLines();
    }

    /// <summary>
    /// Ensures the layout of the row.
    /// </summary>
    internal void EnsureLayout()
    {
        if (_cellPresenter is not null && TableView is not null)
        {
            _cellPresenter.Padding = ((ListView)TableView).SelectionMode is ListViewSelectionMode.Multiple
                                     ? new Thickness(16, 0, 16, 0)
                                     : new Thickness(20, 0, 16, 0);
        }
    }

    /// <summary>
    /// Ensures alternate colors are applied to the row.
    /// </summary>
    internal void EnsureAlternateColors()
    {
        if (TableView is null || _cellPresenter is null) return;

        _cellPresenter.Background =
            Index % 2 == 1 && TableView.AlternateRowBackground is not null ? TableView.AlternateRowBackground : _cellPresenterBackground;

        _cellPresenter.Foreground =
            Index % 2 == 1 && TableView.AlternateRowForeground is not null ? TableView.AlternateRowForeground : _cellPresenterForeground;
    }

    /// <summary>
    /// Gets the list of cells in the row.
    /// </summary>
    internal IList<TableViewCell> Cells => _cellPresenter?.Cells ?? [];

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
}
