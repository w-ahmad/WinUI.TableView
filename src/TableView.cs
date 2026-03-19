using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a control that displays data in customizable table-like interface.
/// </summary>
[StyleTypedProperty(Property = nameof(ColumnHeaderStyle), StyleTargetType = typeof(TableViewColumnHeader))]
[StyleTypedProperty(Property = nameof(CellStyle), StyleTargetType = typeof(TableViewCell))]
public partial class TableView : ListView
{
    private sealed class GroupHeaderRowItem
    {
        public required object GroupKey { get; init; }

        public required string Header { get; init; }
    }

    private static readonly object NullGroupKey = new();
    private TableViewHeaderRow? _headerRow;
    private ScrollViewer? _scrollViewer;
    private RowDefinition? _headerRowDefinition;
    private bool _shouldThrowSelectionModeChangedException;
    private bool _isUpdatingBaseItemsSource;
    private bool _ensureColumns = true;
    private readonly List<TableViewRow> _rows = [];
    private readonly CollectionView _collectionView = [];
    private readonly ObservableCollection<object> _displayItems = [];
    private readonly Dictionary<object, int> _hierarchyLevelsByItem = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, string> _groupHeadersByItem = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, object> _groupKeysByItem = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, object> _groupHeaderItemsByKey = [];
    private readonly HashSet<object> _collapsedHierarchyItems = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<object> _collapsedGroupKeys = [];
    private readonly Dictionary<(Type Type, string Path), Func<object, object?>?> _propertyPathAccessorCache = [];
    private SortDescription? _groupSortDescription;
    private bool _isUpdatingGroupingSortDescription;
    private bool _isDisplayedItemsRebuildQueued;

    /// <summary>
    /// Initializes a new instance of the TableView class.
    /// </summary>
    public TableView()
    {
        DefaultStyleKey = typeof(TableView);

        Columns = new TableViewColumnsCollection(this);
        FilterHandler = new ColumnFilterHandler(this);

        _isUpdatingBaseItemsSource = true;
        base.ItemsSource = _displayItems;
        _isUpdatingBaseItemsSource = false;
        base.SelectionMode = SelectionMode;

        SetValue(ConditionalCellStylesProperty, new TableViewConditionalCellStylesCollection());
        RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, OnBaseItemsSourceChanged);
        RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, OnBaseSelectionModeChanged);

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SelectionChanged += TableView_SelectionChanged;
        _collectionView.ItemPropertyChanged += OnItemPropertyChanged;
        _collectionView.VectorChanged += OnCollectionViewVectorChanged;

        if (SortDescriptions is INotifyCollectionChanged sortDescriptions)
        {
            sortDescriptions.CollectionChanged += OnSortDescriptionsCollectionChanged;
        }
    }

    private void OnCollectionViewVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        QueueDisplayedItemsRebuild();
    }

    private bool _isEnsureGroupingSortQueued;

    private void OnSortDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Defer to avoid ObservableCollection reentrancy:
        // modifying SortDescriptions inside its own CollectionChanged
        // handler throws InvalidOperationException when >1 subscriber.
        if (_isEnsureGroupingSortQueued || _isUpdatingGroupingSortDescription)
        {
            return;
        }

        _isEnsureGroupingSortQueued = true;

        if (DispatcherQueue is null)
        {
            _isEnsureGroupingSortQueued = false;
            EnsureGroupingSortDescription();
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            _isEnsureGroupingSortQueued = false;
            EnsureGroupingSortDescription();
        });
    }

    private void QueueDisplayedItemsRebuild()
    {
        if (_isDisplayedItemsRebuildQueued)
        {
            return;
        }

        _isDisplayedItemsRebuildQueued = true;

        if (DispatcherQueue is null)
        {
            _isDisplayedItemsRebuildQueued = false;
            RebuildDisplayedItems();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() =>
        {
            _isDisplayedItemsRebuildQueued = false;
            RebuildDisplayedItems();
        });
    }

    /// <summary>
    /// Handles the SelectionChanged event of the TableView control.
    /// </summary>
    private void TableView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!KeyboardHelper.IsCtrlKeyDown())
        {
            SelectedCellRanges.Clear();
        }
        else
        {
            SelectedCellRanges.RemoveWhere(slots =>
            {
                slots.RemoveWhere(slot => SelectedRanges.Any(range => range.IsInRange(slot.Row)));
                return slots.Count == 0;
            });
        }

        CurrentCellSlot = null;
        OnCellSelectionChanged();

        if (SelectedItems?.Count == 1)
        {
            DispatcherQueue.TryEnqueue(async () => await ScrollRowIntoView(SelectedIndex));
        }
    }

    /// <summary>
    /// Handles the PropertyChanged event of an item in the TableView.
    /// </summary>
    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var row = ContainerFromItem(sender) as TableViewRow;

        row?.EnsureCellsStyle(default, sender);
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        DispatcherQueue.TryEnqueue(() =>
        {
            if (element is TableViewRow row)
            {
                row.EnsureCellsStyle(default, item);
                row.ApplyCellsSelectionState();

                if (CurrentCellSlot.HasValue)
                {
                    row.ApplyCurrentCellState(CurrentCellSlot.Value);
                }
            }
        });
    }

    /// <inheritdoc/>
    protected override DependencyObject GetContainerForItemOverride()
    {
        var row = new TableViewRow { TableView = this };

        // Set bindings for FontFamily and FontSize to propagate from TableView to TableViewRow
        row.SetBinding(FontFamilyProperty, new Binding { Path = new("TableView.FontFamily"), RelativeSource = new() { Mode = RelativeSourceMode.Self } });
        row.SetBinding(FontSizeProperty, new Binding { Path = new("TableView.FontSize"), RelativeSource = new() { Mode = RelativeSourceMode.Self } });

        _rows.Add(row);
        return row;
    }
        
    /// <inheritdoc/>
    protected override async void OnKeyDown(KeyRoutedEventArgs e)
    {
        var shiftKey = KeyboardHelper.IsShiftKeyDown();
        var ctrlKey = KeyboardHelper.IsCtrlKeyDown();

        if (HandleShortKeys(shiftKey, ctrlKey, e.Key))
        {
            e.Handled = true;
            return;
        }

        await HandleNavigations(e, shiftKey, ctrlKey);
    }

    /// <summary>
    /// Handles navigation keys.
    /// </summary>
    private async Task HandleNavigations(KeyRoutedEventArgs e, bool shiftKey, bool ctrlKey)
    {
        var currentCell = CurrentCellSlot.HasValue ? GetCellFromSlot(CurrentCellSlot.Value) : default;

        if (!IsEditing && e.Key is VirtualKey.Left or VirtualKey.Right && TryHandleHierarchyArrowNavigation(e.Key))
        {
            e.Handled = true;
            return;
        }

        if (e.Key is VirtualKey.F2 && currentCell is { IsReadOnly: false } && !IsEditing)
        {
            e.Handled = await currentCell.BeginCellEditing(e);
        }
        else if (e.Key is VirtualKey.Escape && currentCell is not null && IsEditing)
        {
            e.Handled = EndCellEditing(TableViewEditAction.Cancel, currentCell);
            SetIsEditing(false);
        }
        else if (e.Key is VirtualKey.Space && currentCell is not null && CurrentCellSlot.HasValue && !IsEditing)
        {
            if (!currentCell.IsSelected)
            {
                MakeSelection(CurrentCellSlot.Value, shiftKey, ctrlKey);
            }
            else
            {
                DeselectCell(CurrentCellSlot.Value);
            }
        }

        // Handle navigation keys
        else if (e.Key is VirtualKey.Tab or VirtualKey.Enter)
        {
            var isEditing = IsEditing;

            var newSlot = CurrentCellSlot ?? new();

            do
            {
                newSlot = GetNextSlot(newSlot, shiftKey, e.Key is VirtualKey.Enter);

            } while (isEditing && Columns[newSlot.Column].IsReadOnly);

            if (isEditing && currentCell is not null)
            {
                if (!EndCellEditing(TableViewEditAction.Commit, currentCell)) return;

                if (CurrentCellSlot == newSlot || GetCellFromSlot(newSlot) is not { } nextCell || !await nextCell.BeginCellEditing(e))
                {
                    SetIsEditing(false);
                }
            }

            MakeSelection(newSlot, false);

            e.Handled = true;
        }
        else if ((e.Key is VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down)
                 && !IsEditing)
        {
            var row = (LastSelectionUnit is TableViewSelectionUnit.Row ? CurrentRowIndex : CurrentCellSlot?.Row) ?? -1;
            var column = CurrentCellSlot?.Column ?? -1;

            if (row == -1 && column == -1)
            {
                row = column = 0;
            }
            else if (e.Key is VirtualKey.Left or VirtualKey.Right)
            {
                column = e.Key is VirtualKey.Left ? ctrlKey ? 0 : column - 1 : ctrlKey ? Columns.VisibleColumns.Count - 1 : column + 1;
                if (column >= Columns.VisibleColumns.Count)
                {
                    column = 0;
                    row++;
                }
            }
            else
            {
                row = e.Key == VirtualKey.Up ? ctrlKey ? 0 : row - 1 : ctrlKey ? Items.Count - 1 : row + 1;
            }

            var newSlot = new TableViewCellSlot(row, column);
            MakeSelection(newSlot, shiftKey);
            e.Handled = true;
        }
    }

    private bool TryHandleHierarchyArrowNavigation(VirtualKey key)
    {
        if (!IsHierarchicalEnabled || Items.Count == 0)
        {
            return false;
        }

        var row = (LastSelectionUnit is TableViewSelectionUnit.Row ? CurrentRowIndex : CurrentCellSlot?.Row) ?? -1;
        var column = CurrentCellSlot?.Column ?? 0;

        if (row < 0)
        {
            row = SelectedIndex;
        }

        if (row < 0 || row >= Items.Count)
        {
            return false;
        }

        if (!IsSelectableItem(Items[row]))
        {
            var selectableRow = GetNextSelectableRowIndex(row, key is VirtualKey.Left or VirtualKey.Up ? -1 : 1);
            if (selectableRow < 0)
            {
                return false;
            }

            MakeSelection(new TableViewCellSlot(selectableRow, Math.Max(0, column)), false);
            return true;
        }

        var item = Items[row];
        var level = GetHierarchyLevel(item);
        var hasChildren = HasChildItems(item);
        var isExpanded = IsItemExpanded(item);

        if (key is VirtualKey.Right)
        {
            if (hasChildren && !isExpanded)
            {
                SetItemExpanded(item, true);
                return true;
            }

            if (hasChildren && isExpanded)
            {
                var firstChildRow = row + 1;
                if (firstChildRow < Items.Count && GetHierarchyLevel(Items[firstChildRow]) == level + 1)
                {
                    MakeSelection(new TableViewCellSlot(firstChildRow, Math.Max(0, column)), false);
                    return true;
                }
            }

            return false;
        }

        if (hasChildren && isExpanded)
        {
            SetItemExpanded(item, false);
            return true;
        }

        if (level <= 0)
        {
            return false;
        }

        for (var index = row - 1; index >= 0; index--)
        {
            if (GetHierarchyLevel(Items[index]) == level - 1)
            {
                MakeSelection(new TableViewCellSlot(index, Math.Max(0, column)), false);
                return true;
            }
        }

        return false;
    }

    internal bool EndCellEditing(TableViewEditAction editAction, TableViewCell cell)
    {
        var editingElement = cell.Content as FrameworkElement;
        var endingArgs = new TableViewCellEditEndingEventArgs(cell, cell.Row?.Content, cell.Column!, editingElement!, editAction);
        OnCellEditEnding(endingArgs);
        if (endingArgs.Cancel)
        {
            return false;
        }

        cell.EndEditing(editAction);

        var endArgs = new TableViewCellEditEndedEventArgs(cell, cell.Row?.Content, cell.Column!, editAction);
        OnCellEditEnded(endArgs);

        return true;
    }

    /// <summary>
    /// Handles shortcut keys.
    /// </summary>
    private bool HandleShortKeys(bool shiftKey, bool ctrlKey, VirtualKey key)
    {
        if (key == VirtualKey.A && ctrlKey && !shiftKey)
        {
            SelectAll();
            return true;
        }
        else if (key == VirtualKey.A && ctrlKey && shiftKey)
        {
            DeselectAll();
            return true;
        }
        else if (key == VirtualKey.C && ctrlKey)
        {
            CopyToClipboardInternal(shiftKey);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected async override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _headerRow = GetTemplateChild("HeaderRow") as TableViewHeaderRow;
        _scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
        _headerRowDefinition = GetTemplateChild("HeaderRowDefinition") as RowDefinition;
        if (IsLoaded)
        {
            while (ItemsPanelRoot is null) await Task.Yield();

            EnsureAutoColumns();
        }

        SetHeadersVisibility();
    }

    /// <summary>
    /// Handles the Loaded event of the TableView control.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var scrollPresenter = _scrollViewer?.FindDescendant<ScrollContentPresenter>();
        var xScrollBar = _scrollViewer?.FindDescendant<ScrollBar>(sb => sb.Name is "HorizontalScrollBar2");
        var yScrollBar = _scrollViewer?.FindDescendant<ScrollBar>(sb => sb.Name is "VerticalScrollBar");

        if (scrollPresenter is not null)
        {
            scrollPresenter.PointerWheelChanged += OnScrollContentPresenterPointerWheelChanged;
        }

        if (yScrollBar is not null)
        {
            yScrollBar.ValueChanged += (_, _) => SetValue(VerticalOffsetProperty, yScrollBar.Value);
        }

        xScrollBar?.SetBinding(RangeBase.ValueProperty, new Binding
        {
            Path = new PropertyPath(nameof(HorizontalOffset)),
            Mode = BindingMode.TwoWay,
            Source = this
        });

        EnsureAutoColumns();
    }

    /// <summary>
    /// Handles the Unloaded event of the TableView control.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (IsEditing && CurrentCellSlot.HasValue && GetCellFromSlot(CurrentCellSlot.Value) is { } currentCell)
        {
            currentCell.EndEditing(TableViewEditAction.Commit);
        }
    }

    /// <summary>
    /// Handles the PointerWheelChanged event of the ScrollContentPresenter.
    /// </summary>
    private void OnScrollContentPresenterPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        var isShiftButton = KeyboardHelper.IsShiftKeyDown();
        var isHorizontalScroll = isShiftButton || pointerPoint.Properties.IsHorizontalMouseWheel;

        if (isHorizontalScroll && _scrollViewer?.ComputedHorizontalScrollBarVisibility is Visibility.Visible)
        {
            e.Handled = true;
            var mouseWheelDelta = isShiftButton ? -pointerPoint.Properties.MouseWheelDelta : pointerPoint.Properties.MouseWheelDelta;
            var xOffset = HorizontalOffset + (mouseWheelDelta / 4.0);
            SetValue(HorizontalOffsetProperty, Math.Clamp(xOffset, 0, _scrollViewer.ScrollableWidth));
        }
    }

    /// <summary>
    /// Gets the next cell slot based on the current slot and input keys.
    /// </summary>
    private TableViewCellSlot GetNextSlot(TableViewCellSlot? currentSlot, bool isShiftKeyDown, bool isEnterKey)
    {
        var rows = Items.Count;
        var columns = Columns.VisibleColumns.Count;
        var currentRow = currentSlot?.Row ?? SelectedIndex;
        var currentColumn = currentSlot?.Column ?? -1;
        var nextRow = currentRow;
        var nextColumn = currentColumn;

        if (nextRow == -1 && nextColumn == -1)
        {
            nextRow = nextColumn = 0;
        }
        else if (isEnterKey)
        {
            nextRow += isShiftKeyDown ? -1 : 1;
            if (nextRow < 0)
            {
                nextRow = rows - 1;
                nextColumn = (nextColumn - 1 + columns) % columns;
            }
            else if (nextRow >= rows)
            {
                nextRow = 0;
                nextColumn = (nextColumn + 1) % columns;
            }
        }
        else
        {
            nextColumn += isShiftKeyDown ? -1 : 1;
            if (nextColumn < 0)
            {
                nextColumn = columns - 1;
                nextRow = (nextRow - 1 + rows) % rows;
            }
            else if (nextColumn >= columns)
            {
                nextColumn = 0;
                nextRow = (nextRow + 1) % rows;
            }
        }

        nextRow = GetNextSelectableRowIndex(nextRow, isShiftKeyDown ? -1 : 1);

        return new TableViewCellSlot(nextRow, nextColumn);
    }

    private int GetNextSelectableRowIndex(int startIndex, int step)
    {
        if (Items.Count == 0)
        {
            return -1;
        }

        step = step == 0 ? 1 : Math.Sign(step);
        var index = Math.Clamp(startIndex, 0, Items.Count - 1);

        for (var count = 0; count < Items.Count; count++)
        {
            if (IsSelectableItem(Items[index]))
            {
                return index;
            }

            index += step;

            if (index < 0)
            {
                index = Items.Count - 1;
            }
            else if (index >= Items.Count)
            {
                index = 0;
            }
        }

        return -1;
    }

    /// <summary>
    /// Copies the selected rows or cells content to the clipboard.
    /// </summary>
    internal void CopyToClipboardInternal(bool includeHeaders)
    {
        var args = new TableViewCopyToClipboardEventArgs(includeHeaders);
        OnCopyToClipboard(args);

        if (args.Handled)
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(GetSelectedClipboardContent(includeHeaders));
        Clipboard.SetContent(package);
    }

    /// <summary>
    /// Returns the selected cells' or rows' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values (default is tab).</param>
    /// <returns>A string of selected cell content separated by the specified character.</returns>
    public string GetSelectedContent(bool includeHeaders, char separator = '\t')
    {
        var slots = GetSelectedCellSlots();

        return GetCellsContent(slots, includeHeaders, separator);
    }

    /// <summary>
    /// Returns the selected cells' or rows' clipboard content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values (default is tab).</param>
    /// <returns>A string of selected cell clipboard content separated by the specified character.</returns>
    public string GetSelectedClipboardContent(bool includeHeaders, char separator = '\t')
    {
        var slots = GetSelectedCellSlots();

        return GetCellsContent(slots, includeHeaders, separator, true);
    }

    private IEnumerable<TableViewCellSlot> GetSelectedCellSlots()
    {
        var slots = Enumerable.Empty<TableViewCellSlot>();

        if (SelectedItems.Any() || SelectedCells.Count != 0)
        {
            slots = SelectedRanges.SelectMany(x => Enumerable.Range(x.FirstIndex, (int)x.Length))
                                  .SelectMany(r => Enumerable.Range(0, Columns.VisibleColumns.Count)
                                                                     .Select(c => new TableViewCellSlot(r, c)))
                                  .Concat(SelectedCells)
                                  .OrderBy(x => x.Row)
                                  .ThenByDescending(x => x.Column);
        }
        else if (CurrentCellSlot.HasValue)
        {
            slots = [CurrentCellSlot.Value];
        }

        return slots;
    }

    /// <summary>
    /// Returns all the cells' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values (default is tab).</param>
    /// <returns>A string of all cell content separated by the specified character.</returns>
    public string GetAllContent(bool includeHeaders, char separator = '\t')
    {
        var rows = Enumerable.Range(0, Items.Count).ToArray();

        return GetRowsContent(rows, includeHeaders, separator);
    }

    /// <summary>
    /// Returns specified rows' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="rows">Row indexes to get content for.</param>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values.</param>
    /// <returns>A string of specified row content separated by the specified character.</returns>
    public string GetRowsContent(int[] rows, bool includeHeaders, char separator = '\t')
    {
        var slots = rows.SelectMany(r => Enumerable.Range(0, Columns.VisibleColumns.Count)
                                                           .Select(c => new TableViewCellSlot(r, c)))
                        .OrderBy(x => x.Row)
                        .ThenByDescending(x => x.Column);

        return GetCellsContent(slots, includeHeaders, separator);
    }

    /// <summary>
    /// Returns specified cells' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="slots">Cell slots to get content for.</param>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values.</param>
    /// <returns>A string of specified cell content separated by the specified character.</returns>
    public string GetCellsContent(IEnumerable<TableViewCellSlot> slots, bool includeHeaders, char separator = '\t')
    {
        return GetCellsContent(slots, includeHeaders, separator, false);
    }

    private string GetCellsContent(IEnumerable<TableViewCellSlot> slots, bool includeHeaders, char separator, bool isClipboardContent)
    {
        if (!slots.Any())
        {
            return string.Empty;
        }

        var minColumn = slots.Select(x => x.Column).Min();
        var maxColumn = slots.Select(x => x.Column).Max();
        var stringBuilder = new StringBuilder();

        if (includeHeaders)
        {
            stringBuilder.Append(GetHeadersContent(separator, minColumn, maxColumn));
            stringBuilder.Append('\n');
        }

        foreach (var row in slots.Select(x => x.Row).Distinct())
        {
            var item = Items[row];

            for (var col = minColumn; col <= maxColumn; col++)
            {
                if (Columns.VisibleColumns[col] is not TableViewBoundColumn column ||
                   !slots.Contains(new TableViewCellSlot(row, col)))
                {
                    stringBuilder.Append(separator);
                    continue;
                }

                var content = isClipboardContent ? column.GetClipboardContent(item) : column.GetCellContent(item);
                stringBuilder.Append($"{content}{separator}");
            }

            stringBuilder.Remove(stringBuilder.Length - 1, 1); // remove extra separator at the end of the line
            stringBuilder.Append('\n');
        }

        stringBuilder.Remove(stringBuilder.Length - 1, 1); // remove extra line at the end

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Returns all headers content as a string with values separated by the given character.
    /// </summary>
    /// <param name="separator">The character used to separate cell values.</param>
    /// <param name="minColumn">Min index column.</param>
    /// <param name="maxColumn">Max index column.</param>
    /// <returns>A string of all headers content separated by the specified character.</returns>
    private string GetHeadersContent(char separator, int minColumn, int maxColumn)
    {
        var stringBuilder = new StringBuilder();
        for (var col = minColumn; col <= maxColumn; col++)
        {
            var column = Columns.VisibleColumns[col];
            stringBuilder.Append($"{column.Header}{separator}");
        }

        stringBuilder.Remove(stringBuilder.Length - 1, 1); // remove extra separator at the end of the line

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Generates columns based on the types of the properties of the ItemsSource collection type.
    /// </summary>
    private void GenerateColumns()
    {
        if (ItemsSource is not IEnumerable source) return;

        var dataType = source?.GetItemType();
        if (dataType is null || dataType.IsPrimitive())
        {
            var columnArgs = GenerateColumn(dataType, null, "", dataType?.IsInheritedFromIComparable() is true);
            OnAutoGeneratingColumn(columnArgs);

            if (!columnArgs.Cancel && columnArgs.Column is not null)
            {
                Columns.Insert(Columns.Count, columnArgs.Column);
            }
        }
        else
        {
            foreach (var propertyInfo in dataType.GetProperties())
            {
                if (ShouldSkipAutoGeneratedHierarchyProperty(propertyInfo.Name))
                {
                    continue;
                }

                var displayAttribute = propertyInfo.GetCustomAttributes().OfType<DisplayAttribute>().FirstOrDefault();
                var autoGenerateField = displayAttribute?.GetAutoGenerateField();
                if (autoGenerateField == false)
                {
                    continue;
                }

                var header = displayAttribute?.GetShortName() ?? propertyInfo.Name;
                var canFilter = displayAttribute?.GetAutoGenerateFilter() is true or null;
                var columnArgs = GenerateColumn(propertyInfo.PropertyType, propertyInfo.Name, header, canFilter);
                OnAutoGeneratingColumn(columnArgs);

                if (!columnArgs.Cancel && columnArgs.Column is not null)
                {
                    columnArgs.Column.Order = displayAttribute?.GetOrder();
                    Columns.Add(columnArgs.Column);
                }
            }
        }
    }

    private bool ShouldSkipAutoGeneratedHierarchyProperty(string propertyName)
    {
        if (!IsHierarchicalEnabled)
        {
            return false;
        }

        var comparableName = GetPropertyPathLeaf(propertyName);
        var childrenLeaf = GetPropertyPathLeaf(ChildrenPath);
        var hasChildrenLeaf = GetPropertyPathLeaf(HasChildrenPath);
        var isExpandedLeaf = GetPropertyPathLeaf(IsExpandedPath);

        return string.Equals(comparableName, childrenLeaf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(comparableName, hasChildrenLeaf, StringComparison.OrdinalIgnoreCase)
            || string.Equals(comparableName, isExpandedLeaf, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetPropertyPathLeaf(string? propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            return null;
        }

        var parts = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? null : parts[^1];
    }

    /// <summary>
    /// Generates a column based on the property type.
    /// </summary>
    private static TableViewAutoGeneratingColumnEventArgs GenerateColumn(Type? propertyType, string? propertyName, string header, bool canFilter)
    {
        var newColumn = GetTableViewColumnFromType(propertyName, propertyType);
        newColumn.Header = header;
        newColumn.CanFilter = canFilter;
        newColumn.IsAutoGenerated = true;

        return new TableViewAutoGeneratingColumnEventArgs(propertyName!, propertyType, newColumn);
    }

    /// <summary>
    /// Gets a TableViewColumn based on the property type.
    /// </summary>
    private static TableViewBoundColumn GetTableViewColumnFromType(string? propertyName, Type? type)
    {
        var binding = new Binding { Path = new PropertyPath(propertyName), Mode = BindingMode.TwoWay };
        TableViewBoundColumn column = new TableViewTextColumn { Binding = binding };

        if (type is null)
        {
            return column;
        }
        else if (type.IsTimeSpan() || type.IsTimeOnly())
        {
            column = new TableViewTimeColumn();
        }
        else if (type.IsDateOnly() || type.IsDateTime() || type.IsDateTimeOffset())
        {
            column = new TableViewDateColumn();
        }
        else if (type.IsNumeric())
        {
            column = new TableViewNumberColumn();
        }
        else if (type.IsBoolean())
        {
            column = new TableViewCheckBoxColumn();
        }
        else if(type.IsUri())
        {
            column = new TableViewHyperlinkColumn();
        }

        column.Binding = binding;

        return column;
    }

    /// <summary>
    /// Handles the ItemsSource property changed event.
    /// </summary>
    private void ItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OldValue, e.NewValue))
        {
            _collapsedHierarchyItems.Clear();
            _collapsedGroupKeys.Clear();
        }

        RefreshProcessedItemsSource(e.NewValue as IEnumerable);
    }

    private void RebuildHierarchyView()
    {
        RefreshProcessedItemsSource(ItemsSource as IEnumerable);
    }

    private void EnsureGroupingSortDescription()
    {
        if (_isUpdatingGroupingSortDescription)
        {
            return;
        }

        var desiredPath = string.IsNullOrWhiteSpace(GroupByPath) ? null : GroupByPath;

        // If group sort is already correct, skip to avoid cascading updates.
        if (_groupSortDescription is not null && desiredPath is not null
            && _groupSortDescription.PropertyName == desiredPath
            && _groupSortDescription.Direction == GroupSortDirection
            && SortDescriptions.IndexOf(_groupSortDescription) == 0)
        {
            return;
        }

        _isUpdatingGroupingSortDescription = true;

        try
        {
            using var defer = _collectionView.DeferRefresh();

            if (_groupSortDescription is not null)
            {
                SortDescriptions.Remove(_groupSortDescription);
                _groupSortDescription = null;
            }

            if (desiredPath is not null)
            {
                _groupSortDescription = new SortDescription(desiredPath, GroupSortDirection);
                SortDescriptions.Insert(0, _groupSortDescription);
            }
        }
        finally
        {
            _isUpdatingGroupingSortDescription = false;
        }
    }

    internal void RefreshHierarchySorting()
    {
        if (IsHierarchicalEnabled)
        {
            RebuildHierarchyView();
        }
    }

    private void RefreshProcessedItemsSource(IEnumerable? source)
    {
        using var defer = _collectionView.DeferRefresh();

        _collectionView.SuppressSorting = IsHierarchicalEnabled;
        EnsureGroupingSortDescription();

        _hierarchyLevelsByItem.Clear();
        _groupHeadersByItem.Clear();
        _groupKeysByItem.Clear();
        _groupHeaderItemsByKey.Clear();
        _displayItems.Clear();

        _collectionView.Source = null!;

        if (source is not null)
        {
            EnsureAutoColumns();
            _collectionView.Source = BuildProcessedSource(source);
        }

        RebuildDisplayedItems();
    }

    private void RebuildDisplayedItems()
    {
        BuildGroupHeadersFromCurrentView();

        _displayItems.Clear();

        if (!string.IsNullOrWhiteSpace(GroupByPath) && ShowGroupHeaders)
        {
            object? previousGroupKey = null;
            var hasPreviousGroup = false;

            foreach (var item in _collectionView.OfType<object>())
            {
                var groupKey = GetNormalizedGroupKeyForItem(item);

                if (!hasPreviousGroup || !Equals(previousGroupKey, groupKey))
                {
                    if (_groupHeaderItemsByKey.TryGetValue(groupKey, out var headerItem))
                    {
                        _displayItems.Add(headerItem);
                    }

                    previousGroupKey = groupKey;
                    hasPreviousGroup = true;
                }

                if (!_collapsedGroupKeys.Contains(groupKey))
                {
                    _displayItems.Add(item);
                }
            }

            RefreshRowsGroupingState();
            return;
        }

        foreach (var item in _collectionView.OfType<object>())
        {
            _displayItems.Add(item);
        }

        RefreshRowsGroupingState();
    }

    private IEnumerable BuildProcessedSource(IEnumerable source)
    {
        if (IsHierarchicalEnabled && HasHierarchyBinding())
        {
            var flattened = new List<object>();
            FlattenHierarchy(source, flattened, 0, []);
            return flattened;
        }

        foreach (var item in source.OfType<object>())
        {
            _hierarchyLevelsByItem[item] = 0;
        }

        return source;
    }

    private void FlattenHierarchy(IEnumerable source, ICollection<object> target, int level, HashSet<object> path)
    {
        foreach (var item in GetSortedHierarchyLevelItems(source))
        {
            target.Add(item);
            _hierarchyLevelsByItem[item] = level;

            if (!IsHierarchicalEnabled || !HasHierarchyBinding() || !path.Add(item) || !IsItemExpanded(item))
            {
                continue;
            }

            try
            {
                var children = GetChildren(item);

                if (children is IEnumerable childrenEnumerable && children is not string)
                {
                    FlattenHierarchy(childrenEnumerable, target, level + 1, path);
                }
            }
            finally
            {
                path.Remove(item);
            }
        }
    }

    private IReadOnlyList<object> GetSortedHierarchyLevelItems(IEnumerable source)
    {
        var items = source.OfType<object>()
                          .Select((item, index) => new { Item = item, Index = index })
                          .ToList();

        if (_collectionView.SortDescriptions.Count > 0)
        {
            items.Sort((left, right) =>
            {
                var comparison = _collectionView.Compare(left.Item, right.Item);
                return comparison != 0 ? comparison : left.Index.CompareTo(right.Index);
            });
        }

        return [.. items.Select(x => x.Item)];
    }

    private object? ResolvePropertyPathValue(object item, string propertyPath)
    {
        var key = (item.GetType(), propertyPath);

        if (!_propertyPathAccessorCache.TryGetValue(key, out var accessor))
        {
            accessor = item.GetFuncCompiledPropertyPath(propertyPath);
            _propertyPathAccessorCache[key] = accessor;
        }

        return accessor?.Invoke(item);
    }

    private bool HasHierarchyBinding()
    {
        return ChildrenSelector is not null || !string.IsNullOrWhiteSpace(ChildrenPath);
    }

    private IEnumerable? GetChildren(object item)
    {
        if (ChildrenSelector is not null)
        {
            return ChildrenSelector(item);
        }

        if (!string.IsNullOrWhiteSpace(ChildrenPath))
        {
            return ResolvePropertyPathValue(item, ChildrenPath!) as IEnumerable;
        }

        return null;
    }

    private bool TrySetSimplePropertyPathValue(object item, string propertyPath, object? value)
    {
        try
        {
            object current = item;
            var parts = propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < parts.Length - 1; index++)
            {
                var propertyInfo = current.GetType().GetProperty(parts[index], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (propertyInfo?.GetValue(current) is not { } next)
                {
                    return false;
                }

                current = next;
            }

            var targetProperty = current.GetType().GetProperty(parts[^1], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (targetProperty is null || !targetProperty.CanWrite)
            {
                return false;
            }

            var convertedValue = value;
            if (value is not null && targetProperty.PropertyType != value.GetType())
            {
                convertedValue = Convert.ChangeType(value, targetProperty.PropertyType);
            }

            targetProperty.SetValue(current, convertedValue);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal bool HasChildItems(object? item)
    {
        if (!IsHierarchicalEnabled || item is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(HasChildrenPath) && ResolvePropertyPathValue(item, HasChildrenPath!) is bool hasChildren)
        {
            return hasChildren;
        }

        var children = GetChildren(item);
        if (children is null || children is string)
        {
            return false;
        }

        if (children is ICollection collection)
        {
            return collection.Count > 0;
        }

        var enumerator = children.GetEnumerator();
        return enumerator.MoveNext();
    }

    internal bool IsItemExpanded(object? item)
    {
        if (!IsHierarchicalEnabled || item is null || !HasChildItems(item))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(IsExpandedPath) && ResolvePropertyPathValue(item, IsExpandedPath!) is bool isExpanded)
        {
            return isExpanded;
        }

        return !_collapsedHierarchyItems.Contains(item);
    }

    internal void ToggleItemExpansion(object? item)
    {
        if (item is null || !HasChildItems(item))
        {
            return;
        }

        SetItemExpanded(item, !IsItemExpanded(item));
    }

    internal void SetItemExpanded(object item, bool isExpanded)
    {
        if (!HasChildItems(item))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(IsExpandedPath))
        {
            _ = TrySetSimplePropertyPathValue(item, IsExpandedPath!, isExpanded);
        }

        if (isExpanded)
        {
            _collapsedHierarchyItems.Remove(item);
            OnRowExpanded(new TableViewRowExpansionChangedEventArgs(item, _collectionView.IndexOf(item), true));
        }
        else
        {
            _collapsedHierarchyItems.Add(item);
            OnRowCollapsed(new TableViewRowExpansionChangedEventArgs(item, _collectionView.IndexOf(item), false));
        }

        RebuildHierarchyView();
    }

    private void BuildGroupHeadersFromCurrentView()
    {
        _groupHeadersByItem.Clear();
        _groupKeysByItem.Clear();
        _groupHeaderItemsByKey.Clear();

        if (string.IsNullOrWhiteSpace(GroupByPath))
        {
            return;
        }

        var groupedItems = _collectionView.OfType<object>()
                                          .Select(item => new
                                          {
                                              Item = item,
                                              GroupKey = ResolvePropertyPathValue(item, GroupByPath!)
                                          })
                                          .ToList();

        if (groupedItems.Count == 0)
        {
            return;
        }

        var groupStartIndex = 0;

        for (var index = 1; index <= groupedItems.Count; index++)
        {
            var isBoundary = index == groupedItems.Count
                             || !Equals(groupedItems[index - 1].GroupKey, groupedItems[index].GroupKey);

            if (!isBoundary)
            {
                continue;
            }

            var startItem = groupedItems[groupStartIndex];
            var count = index - groupStartIndex;
            var groupKey = NormalizeGroupKey(startItem.GroupKey);

            for (var groupItemIndex = groupStartIndex; groupItemIndex < index; groupItemIndex++)
            {
                _groupKeysByItem[groupedItems[groupItemIndex].Item] = groupKey;
            }

            if (ShowGroupHeaders)
            {
                // Only count top-level (non-child) items so hierarchy children don't inflate the group count.
                var topLevelCount = Enumerable.Range(groupStartIndex, count)
                                              .Count(i => GetHierarchyLevel(groupedItems[i].Item) == 0);

                var headerItem = new GroupHeaderRowItem
                {
                    GroupKey = groupKey,
                    Header = FormatGroupHeader(startItem.GroupKey, topLevelCount)
                };

                _groupHeaderItemsByKey[groupKey] = headerItem;
                _groupKeysByItem[headerItem] = groupKey;
                _groupHeadersByItem[headerItem] = headerItem.Header;
            }

            groupStartIndex = index;
        }
    }

    private string FormatGroupHeader(object? groupKey, int count)
    {
        var title = groupKey?.ToString() ?? "(null)";

        if (!ShowGroupItemCount)
        {
            return title;
        }

        return $"{title} ({count})";
    }

    private void RefreshRowsGroupingState()
    {
        foreach (var row in _rows)
        {
            row.RowPresenter?.SetRowHeaderTemplate();
            row.RowPresenter?.SetRowHeaderVisibility();
            row.UpdateHierarchyPresentation();
        }
    }

    internal int GetHierarchyLevel(object? item)
    {
        if (item is not null && _hierarchyLevelsByItem.TryGetValue(item, out var level))
        {
            return level;
        }

        return 0;
    }

    internal bool TryGetGroupHeader(object? item, out string header)
    {
        header = string.Empty;

        if (item is not null && _groupHeadersByItem.TryGetValue(item, out var value))
        {
            header = value;
            return true;
        }

        return false;
    }

    internal bool HasGroupHeader(object? item)
    {
        return item is not null && _groupHeadersByItem.ContainsKey(item);
    }

    internal bool IsGroupHeaderItem(object? item)
    {
        return item is GroupHeaderRowItem;
    }

    internal bool IsSelectableItem(object? item)
    {
        return item is not GroupHeaderRowItem;
    }

    internal bool IsGroupExpanded(object? item)
    {
        if (item is null || !_groupKeysByItem.TryGetValue(item, out var groupKey))
        {
            return true;
        }

        return !_collapsedGroupKeys.Contains(groupKey);
    }

    internal bool ShouldShowGroupedItemContent(object? item)
    {
        return item is not GroupHeaderRowItem;
    }

    internal void ToggleGroupExpansion(object? item)
    {
        if (item is null || !HasGroupHeader(item) || !_groupKeysByItem.TryGetValue(item, out var groupKey))
        {
            return;
        }

        if (_collapsedGroupKeys.Contains(groupKey))
        {
            _collapsedGroupKeys.Remove(groupKey);
        }
        else
        {
            _collapsedGroupKeys.Add(groupKey);
        }

        RebuildDisplayedItems();
    }

    private object GetNormalizedGroupKeyForItem(object item)
    {
        if (_groupKeysByItem.TryGetValue(item, out var groupKey))
        {
            return groupKey;
        }

        return NormalizeGroupKey(ResolvePropertyPathValue(item, GroupByPath!));
    }

    private static object NormalizeGroupKey(object? groupKey)
    {
        return groupKey ?? NullGroupKey;
    }

    /// <summary>
    /// Ensures that columns are automatically generated based on the current state of the control.
    /// </summary>
    private void EnsureAutoColumns(bool force = false)
    {
        if ((_ensureColumns || force) && IsLoaded && AutoGenerateColumns && ItemsSource is not null)
        {
            RemoveAutoGeneratedColumns();
            GenerateColumns();

            _ensureColumns = false;
        }
    }

    /// <summary>
    /// Removes auto-generated columns.
    /// </summary>
    private void RemoveAutoGeneratedColumns()
    {
        Columns.RemoveWhere(x => x.IsAutoGenerated);
    }

    /// <summary>
    /// Exports the selected rows or cells content to a CSV file.
    /// </summary>
    internal async void ExportSelectedToCSV()
    {
        var args = new TableViewExportContentEventArgs();
        OnExportSelectedContent(args);

        if (args.Handled)
        {
            return;
        }

        try
        {
            if (await GetStorageFile() is not { } file)
            {
                return;
            }

            var content = GetSelectedContent(true, ',');
            using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);

            using var tw = new StreamWriter(stream);
            await tw.WriteAsync(content);
        }
        catch { }
    }

    /// <summary>
    /// Exports all rows content to a CSV file.
    /// </summary>
    internal async void ExportAllToCSV()
    {
        var args = new TableViewExportContentEventArgs();
        OnExportAllContent(args);

        if (args.Handled)
        {
            return;
        }

        try
        {
            if (await GetStorageFile() is not { } file)
            {
                return;
            }

            var content = GetAllContent(true, ',');
            using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);

            using var tw = new StreamWriter(stream);
            await tw.WriteAsync(content);
        }
        catch { }
    }

    /// <summary>
    /// Gets a storage file for saving the CSV.
    /// </summary>
    private async Task<StorageFile> GetStorageFile()
    {
        var savePicker = new FileSavePicker();
        savePicker.FileTypeChoices.Add("CSV (Comma delimited)", [".csv"]);
#if WINDOWS
        var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
#endif

        return await savePicker.PickSaveFileAsync();
    }

    /// <summary>
    /// Refreshes the items view of the TableView.
    /// </summary>
    public void RefreshView()
    {
        DeselectAll();
        _collectionView.Refresh();
    }

    /// <summary>
    /// Refreshes the sorting applied to the items in the TableView.
    /// </summary>
    public void RefreshSorting()
    {
        DeselectAll();
        _collectionView.RefreshSorting();
    }

    /// <summary>
    /// Clears all sorting applied to the items.
    /// </summary>
    public void ClearAllSorting()
    {
        DeselectAll();
        SortDescriptions.Clear();

        foreach (var column in Columns.Where(c => c.SortDirection is not null))
        {
            if (column is not null)
            {
                column.SortDirection = null;
            }
        }
    }

    /// <summary>
    /// Clears all sorting applied to the items with event.
    /// </summary>
    internal void ClearAllSortingWithEvent()
    {
        var eventArgs = new TableViewClearSortingEventArgs();
        OnClearSorting(eventArgs);

        if (eventArgs.Handled)
        {
            return;
        }

        ClearAllSorting();
    }

    /// <summary>
    /// Clears all filters applied to the items.
    /// </summary>
    public void ClearAllFilters()
    {
        FilterHandler.ClearFilter(null);
    }

    /// <summary>
    /// Refreshes all applied filters.
    /// </summary>
    public void RefreshFilter()
    {
        DeselectAll();
        _collectionView.RefreshFilter();
    }

    /// <summary>
    /// Selects all rows or cells in the TableView.
    /// </summary>
    internal new void SelectAll()
    {
        if (IsEditing)
        {
            return;
        }

        if (SelectionUnit is TableViewSelectionUnit.Cell)
        {
            SelectAllCells();
            CurrentCellSlot = null;
        }
        else
        {
            switch (SelectionMode)
            {
                case ListViewSelectionMode.Single:
                    SelectedItem = Items.FirstOrDefault();
                    break;
                case ListViewSelectionMode.Multiple:
                case ListViewSelectionMode.Extended:
                    SelectRange(new ItemIndexRange(0, (uint)Items.Count));
                    break;
            }
        }
    }

    /// <summary>
    /// Selects all cells in the TableView.
    /// </summary>
    private void SelectAllCells()
    {
        switch (SelectionMode)
        {
            case ListViewSelectionMode.Single:
                if (Items.Count > 0 && Columns.VisibleColumns.Count > 0)
                {
                    SelectedCellRanges.Clear();
                    SelectedCellRanges.Add([new TableViewCellSlot(0, 0)]);
                }
                break;
            case ListViewSelectionMode.Multiple:
            case ListViewSelectionMode.Extended:
                SelectedCellRanges.Clear();
                var selectionRange = new HashSet<TableViewCellSlot>();

                for (var row = 0; row < Items.Count; row++)
                {
                    for (var column = 0; column < Columns.VisibleColumns.Count; column++)
                    {
                        selectionRange.Add(new TableViewCellSlot(row, column));
                    }
                }
                SelectedCellRanges.Add(selectionRange);
                break;
        }

        OnCellSelectionChanged();
    }

    /// <summary>
    /// Deselects all rows or cells in the TableView.
    /// </summary>
    public void DeselectAll()
    {
        DeselectAllItems();
        DeselectAllCells();
    }

    /// <summary>
    /// Deselects all rows in the TableView.
    /// </summary>
    private void DeselectAllItems()
    {
        if (SelectedRanges.Count is 0) return;

        switch (SelectionMode)
        {
            case ListViewSelectionMode.Single:
                SelectedItem = null;
                break;
            case ListViewSelectionMode.Multiple:
            case ListViewSelectionMode.Extended:
                DeselectRange(new ItemIndexRange(0, (uint)Items.Count));
                break;
        }
    }

    /// <summary>
    /// Deselects all cells in the TableView.
    /// </summary>
    private void DeselectAllCells()
    {
        if (SelectedCellRanges.Count is 0) return;

        SelectedCellRanges.Clear();
        OnCellSelectionChanged();
        CurrentCellSlot = null;
    }

    /// <summary>
    /// Selects a row or cell based on the specified cell slot.
    /// </summary>
    internal void MakeSelection(TableViewCellSlot slot, bool shiftKey, bool ctrlKey = false)
    {
        if (!slot.IsValidRow(this))
        {
            return;
        }

        if (!IsSelectableItem(Items[slot.Row]))
        {
            var selectableRow = GetNextSelectableRowIndex(slot.Row, 1);
            if (selectableRow < 0)
            {
                return;
            }

            slot = new TableViewCellSlot(selectableRow, slot.Column);
        }

        if (SelectionMode != ListViewSelectionMode.None)
        {
            ctrlKey = ctrlKey || SelectionMode is ListViewSelectionMode.Multiple;

            if (!ctrlKey || !(SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended))
            {
                if (SelectedItems.Count > 0)
                {
                    DeselectAllItems();
                }

                if (SelectedCells.Count > 0)
                {
                    SelectedCellRanges.Clear();
                }
            }

            if (SelectionUnit is TableViewSelectionUnit.Row
               || (LastSelectionUnit is TableViewSelectionUnit.Row && slot.IsValidRow(this) && !slot.IsValidColumn(this))
               || (SelectionUnit is TableViewSelectionUnit.CellOrRow && slot.IsValidRow(this) && !slot.IsValidColumn(this)))
            {
                SelectRows(slot, shiftKey);
                LastSelectionUnit = TableViewSelectionUnit.Row;
            }
            else
            {
                SelectCells(slot, shiftKey);
                LastSelectionUnit = TableViewSelectionUnit.Cell;
            }
        }
        else if (!IsReadOnly)
        {
            SelectionStartCellSlot = slot;
            CurrentCellSlot = slot;
        }
    }

    /// <summary>
    /// Selects rows based on the specified cell slot.
    /// </summary>
    private void SelectRows(TableViewCellSlot slot, bool shiftKey)
    {
        var selectionRange = SelectedRanges.FirstOrDefault(x => x.IsInRange(slot.Row));
        SelectionStartRowIndex ??= slot.Row;
        CurrentRowIndex = slot.Row;

        if (selectionRange is not null)
        {
            DeselectRange(selectionRange);
        }

        if (shiftKey && SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended)
        {
            var min = Math.Min(SelectionStartRowIndex.Value, slot.Row);
            var max = Math.Max(SelectionStartRowIndex.Value, slot.Row);

            SelectRange(new ItemIndexRange(min, (uint)(max - min) + 1));
        }
        else
        {
            SelectionStartRowIndex = slot.Row;
            if (SelectionMode is ListViewSelectionMode.Single)
            {
                SelectedIndex = slot.Row;
            }
            else
            {
                SelectRange(new ItemIndexRange(slot.Row, 1));
            }
        }

        if (!IsReadOnly && slot.IsValid(this))
        {
            CurrentCellSlot = slot;
        }
        else
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                var row = await ScrollRowIntoView(slot.Row);
                row?.Focus(FocusState.Programmatic);
            });
        }
    }

    /// <summary>
    /// Selects cells based on the specified cell slot.
    /// </summary>
    private void SelectCells(TableViewCellSlot slot, bool shiftKey)
    {
        if (!slot.IsValid(this))
        {
            return;
        }

        var selectionRange = (SelectionStartCellSlot is null ? null : SelectedCellRanges.LastOrDefault(x => SelectionStartCellSlot.HasValue && x.Contains(SelectionStartCellSlot.Value))) ?? [];
        SelectedCellRanges.Remove(selectionRange);
        selectionRange.Clear();
        SelectionStartCellSlot ??= CurrentCellSlot;
        SelectionStartCellSlot ??= slot;

        if (shiftKey && SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended)
        {
            var currentSlot = SelectionStartCellSlot.Value;
            var startRow = Math.Min(slot.Row, currentSlot.Row);
            var endRow = Math.Max(slot.Row, currentSlot.Row);
            var startCol = Math.Min(slot.Column, currentSlot.Column);
            var endCol = Math.Max(slot.Column, currentSlot.Column);
            for (var row = startRow; row <= endRow; row++)
            {
                for (var column = startCol; column <= endCol; column++)
                {
                    var nextSlot = new TableViewCellSlot(row, column);
                    selectionRange.Add(nextSlot);
                    if (SelectedCellRanges.LastOrDefault(x => x.Contains(nextSlot)) is { } range)
                    {
                        range.Remove(nextSlot);
                    }
                }
            }
        }
        else
        {
            SelectionStartCellSlot = slot;
            selectionRange.Add(slot);

            if (SelectedCellRanges.LastOrDefault(x => x.Contains(slot)) is { } range)
            {
                range.Remove(slot);
            }
        }

        SelectedCellRanges.Add(selectionRange);
        OnCellSelectionChanged();
        CurrentCellSlot = slot;
    }

    /// <summary>
    /// Deselects the specified cell slot.
    /// </summary>
    internal void DeselectCell(TableViewCellSlot slot)
    {
        var selectionRange = SelectedCellRanges.LastOrDefault(x => x.Contains(slot));
        selectionRange?.Remove(slot);

        if (selectionRange?.Count == 0)
        {
            SelectedCellRanges.Remove(selectionRange);
        }

        CurrentCellSlot = slot;
        OnCellSelectionChanged();
    }

    /// <summary>
    /// Handles changes to the current cell in the table view.
    /// </summary>
    private async Task OnCurrentCellChanged(TableViewCellSlot? oldSlot, TableViewCellSlot? newSlot)
    {
        if (oldSlot == newSlot)
        {
            return;
        }

        if (oldSlot.HasValue)
        {
            var cell = GetCellFromSlot(oldSlot.Value);
            cell?.ApplyCurrentCellState();
        }

        if (newSlot.HasValue)
        {
            var cell = await ScrollCellIntoView(newSlot.Value);
            cell?.ApplyCurrentCellState();
        }
    }

    /// <summary>
    /// Handles cell selection changes.
    /// </summary>
    private void OnCellSelectionChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var oldSelection = SelectedCells;
            SelectedCells = [.. SelectedCellRanges.SelectMany(x => x)];

            var rowIndexes = oldSelection.Select(x => x.Row).Concat(SelectedCells.Select(x => x.Row)).Distinct();

            foreach (var rowIndex in rowIndexes)
            {
                var row = _rows.FirstOrDefault(x => x.Index == rowIndex);
                row?.ApplyCellsSelectionState();
            }

            InvokeCellSelectionChangedEvent(oldSelection);
        });
    }

    /// <summary>
    /// Invokes the <see cref="CellSelectionChanged"/> event to notify subscribers of changes in the selected cells.
    /// </summary>
    private void InvokeCellSelectionChangedEvent(HashSet<TableViewCellSlot> oldSelection)
    {
        var removedCells = oldSelection.Except(SelectedCells).ToList();
        var addedCells = SelectedCells.Except(oldSelection).ToList();

        if (removedCells.Count > 0 || addedCells.Count > 0)
        {
            OnCellSelectionChanged(new TableViewCellSelectionChangedEventArgs(removedCells, addedCells));
        }
    }

    /// <summary>
    /// Scrolls the specified cell slot into view.
    /// </summary>
    /// <param name="slot">The cell slot to scroll into view.</param>
    public async Task<TableViewCell> ScrollCellIntoView(TableViewCellSlot slot)
    {
        if (_scrollViewer is null || !slot.IsValid(this) || await ScrollRowIntoView(slot.Row) is not { } row)
            return default!;

        var (start, end) = GetColumnsInDisplay();
        var xOffset = 0d;
        var yOffset = _scrollViewer.VerticalOffset;

        // Calculate the left and right edge of the cell
        var cellLeft = Columns.VisibleColumns.Take(slot.Column).Sum(x => x.ActualWidth);
        var cellWidth = Columns.VisibleColumns[slot.Column].ActualWidth;
        var cellRight = cellLeft + cellWidth;
        var viewportLeft = HorizontalOffset;
        var headersOffset = CellsHorizontalOffset;
        var viewportRight = viewportLeft + _scrollViewer.ViewportWidth - headersOffset;

        // If cell is wider than the viewport, align left edge
        if (cellWidth > _scrollViewer.ViewportWidth - headersOffset)
        {
            xOffset = cellLeft;
        }
        // If cell is left of the viewport, scroll to its left edge
        else if (cellLeft < viewportLeft)
        {
            xOffset = cellLeft;
        }
        // If cell is right of the viewport, scroll so its right edge is visible
        else if (cellRight > viewportRight)
        {
            xOffset = cellRight - (_scrollViewer.ViewportWidth - headersOffset);
        }

        // If cell is fully in view, just return
        if ((cellLeft >= viewportLeft && cellRight <= viewportRight) ||
            xOffset == HorizontalOffset)
        {
            return row.Cells.ElementAt(slot.Column);
        }

        SetValue(HorizontalOffsetProperty, xOffset);

        return row?.Cells.ElementAt(slot.Column)!;
    }

    /// <summary>
    /// Scrolls the specified row into view.
    /// </summary>
    /// <param name="index">The index of the row to scroll into view.</param>
    public async Task<TableViewRow?> ScrollRowIntoView(int index)
    {
        if (_scrollViewer is null || index < 0 || index >= Items.Count)
        {
            return default!;
        }

        var item = Items[index];
        index = Items.IndexOf(item); // if the ItemsSource has duplicate items in it. ScrollIntoView will only bring first index of the item.

        if (index < 0 || index >= Items.Count)
        {
            return default!;
        }

        ScrollIntoView(item);

        var tries = 0;
        while (tries < 10)
        {
            tries++;
            await Task.Yield();

            if (ContainerFromIndex(index) is TableViewRow row)
            {
                var transform = row.TransformToVisual(_scrollViewer);
                var positionInScrollViewer = transform.TransformPoint(new Point(0, 0));
                if ((index == 0 && _scrollViewer.VerticalOffset > 0) || (index > 0 && positionInScrollViewer.Y < HeaderRowHeight))
                {
                    var yOffset = index == 0 ? 0d : _scrollViewer.VerticalOffset - row.ActualHeight + positionInScrollViewer.Y + 8;
                    var tcs = new TaskCompletionSource<object?>();

                    try
                    {
                        _scrollViewer.ViewChanged += ViewChanged;
                        _scrollViewer.ChangeView(0, yOffset, null, true);
                        await tcs.Task;
                    }
                    finally
                    {
                        _scrollViewer.ViewChanged -= ViewChanged;
                    }

                    void ViewChanged(object? _, ScrollViewerViewChangedEventArgs e)
                    {
                        if (e.IsIntermediate)
                        {
                            return;
                        }

                        tcs.TrySetResult(result: default);
                    }
                }

                return row;
            }
        }

        return default;
    }

    /// <summary>
    /// Gets the cell based on the specified cell slot.
    /// </summary>
    internal TableViewCell? GetCellFromSlot(TableViewCellSlot slot)
    {
        return slot.IsValid(this) && ContainerFromIndex(slot.Row) is TableViewRow row ? row.Cells[slot.Column] : default;
    }

    /// <summary>
    /// Gets the columns currently in view.
    /// </summary>
    private (int start, int end) GetColumnsInDisplay()
    {
        if (_scrollViewer is null) return default!;

        var start = -1;
        var end = -1;
        var width = 0d;
        var headersOffset = CellsHorizontalOffset;

        foreach (var column in Columns.VisibleColumns)
        {
            if (width >= HorizontalOffset &&
                width + column.ActualWidth <= HorizontalOffset + _scrollViewer.ViewportWidth - headersOffset)
            {
                if (start == -1)
                {
                    start = end = Columns.VisibleColumns.IndexOf(column);
                }
                else
                {
                    end = Columns.VisibleColumns.IndexOf(column);
                }
            }

            width += column.ActualWidth;
        }

        return (start, end);
    }

    /// <summary>
    /// Updates the base SelectionMode property.
    /// </summary>
    private void UpdateBaseSelectionMode()
    {
        _shouldThrowSelectionModeChangedException = true;

        base.SelectionMode = SelectionUnit is TableViewSelectionUnit.Cell ? ListViewSelectionMode.None : SelectionMode;

        UpdateHorizontalScrollBarMargin();
        _headerRow?.SetHeadersVisibility();

        foreach (var row in _rows)
        {
            row.EnsureLayout();
            row.RowPresenter?.SetRowHeaderVisibility();

        }

        _shouldThrowSelectionModeChangedException = false;
    }

    /// <summary>
    /// Ensures grid lines are applied to the header row and body rows.
    /// </summary>
    private void EnsureGridLines()
    {
        _headerRow?.EnsureGridLines();

        foreach (var row in _rows)
        {
            row.RowPresenter?.EnsureGridLines();
        }
    }

    /// <summary>
    /// Ensures alternate row colors are applied.
    /// </summary>
    internal void EnsureAlternateRowColors()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            foreach (var row in _rows)
            {
                row.EnsureAlternateColors();
            }
        });
    }

    /// <summary>
    /// Ensures the column headers style is applied.
    /// </summary>
    private void EnsureColumnHeadersStyle()
    {
        foreach (var column in Columns)
        {
            column.EnsureHeaderStyle();
        }
    }

    /// <summary>
    /// Ensures the cells style is applied.
    /// </summary>
    private void EnsureCellsStyle()
    {
        foreach (var row in _rows)
        {
            row.EnsureCellsStyle();
        }
    }

#if !WINDOWS
    /// <summary>
    /// Ensures the cells are created.
    /// </summary>
    internal void EnsureCells()
    {
        foreach (var row in _rows)
        {
            row.EnsureCells();
        }
    }
#endif

    /// <summary>
    /// Shows the context flyout for the specified row.
    /// </summary>
    internal bool ShowRowContext(TableViewRow row, Point position)
    {
        var eventArgs = new TableViewRowContextFlyoutEventArgs(row.Index, row, row.Content, RowContextFlyout);
        OnRowContextFlyoutOpening(eventArgs);

        if (RowContextFlyout is not null && !eventArgs.Handled)
        {
#if !WINDOWS
            RowContextFlyout.DataContext = row.Content;
#endif
            RowContextFlyout.ShowAt(row.RowPresenter, new FlyoutShowOptions
            {
#if WINDOWS
                ShowMode = FlyoutShowMode.Standard,
#endif
                Placement = RowContextFlyout.Placement,
                Position = position
            });

            return true;
        }

        return false;
    }

    /// <summary>
    /// Shows the context flyout for the specified cell.
    /// </summary>
    internal bool ShowCellContext(TableViewCell cell, Point position)
    {
        var eventArgs = new TableViewCellContextFlyoutEventArgs(cell.Slot, cell, cell.Row?.Content!, CellContextFlyout);
        OnCellContextFlyoutOpening(eventArgs);

        if (CellContextFlyout is not null && !eventArgs.Handled)
        {
#if !WINDOWS
            CellContextFlyout.DataContext = cell.Row?.Content;
#endif
            CellContextFlyout.ShowAt(cell, new FlyoutShowOptions
            {
#if WINDOWS
                ShowMode = FlyoutShowMode.Standard,
#endif
                Placement = CellContextFlyout.Placement,
                Position = position
            });

            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the state of the corner button.
    /// </summary>
    internal void UpdateCornerButtonState()
    {
        _headerRow?.SetCornerButtonState();

        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (SelectionMode is ListViewSelectionMode.Multiple && SelectionUnit is not TableViewSelectionUnit.Cell)
            {
                foreach (var row in _rows)
                {
                    row.UpdateSelectCheckMarkOpacity();
                }
            }
        });
    }

    internal void SetIsEditing(bool value)
    {
        if (IsEditing == value)
        {
            return;
        }

        IsEditing = value;
        UpdateCornerButtonState();
    }

    /// <summary>
    /// Sets the visibility of the headers.
    /// </summary>
    private void SetHeadersVisibility()
    {
        if (_headerRowDefinition is not null)
        {
            var areColumnHeadersVisible = HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Columns;
            _headerRowDefinition.Height = areColumnHeadersVisible ? GridLength.Auto : new(0);
        }

        _headerRow?.SetHeadersVisibility();

        foreach (var row in _rows)
        {
            row.RowPresenter?.SetRowHeaderVisibility();
        }
    }

    /// <summary>
    /// Updates the margin of the horizontal scroll bar to account for frozen columns and row headers.
    /// </summary>
    internal void UpdateHorizontalScrollBarMargin()
    {
        if (_scrollViewer is null) return;

        var offset = CellsHorizontalOffset + Columns.VisibleColumns.Where(c => c.IsFrozen).Sum(c => c.ActualWidth);
        AttachedPropertiesHelper.SetFrozenColumnScrollBarSpace(_scrollViewer, offset);
    }
}