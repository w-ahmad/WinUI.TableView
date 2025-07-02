using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations.Expressions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
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
    private TableViewHeaderRow? _headerRow;
    private ScrollViewer? _scrollViewer;
    private bool _shouldThrowSelectionModeChangedException;
    private bool _ensureColumns = true;
    private readonly List<TableViewRow> _rows = [];
    private readonly CollectionView _collectionView = [];

    /// <summary>
    /// Initializes a new instance of the TableView class.
    /// </summary>
    public TableView()
    {
        DefaultStyleKey = typeof(TableView);

        Columns.TableView = this;
        FilterHandler = new ColumnFilterHandler(this);
        base.ItemsSource = _collectionView;
        base.SelectionMode = SelectionMode;
        RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, OnBaseItemsSourceChanged);
        RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, OnBaseSelectionModeChanged);
        Loaded += OnLoaded;
        SelectionChanged += TableView_SelectionChanged;
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

        if (SelectedIndex > 0)
        {
            DispatcherQueue.TryEnqueue(async () => await ScrollRowIntoView(SelectedIndex));
        }
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        DispatcherQueue.TryEnqueue(() =>
        {
            if (element is TableViewRow row)
            {
                row.ApplyCellsSelectionState();

                if (CurrentCellSlot.HasValue)
                {
                    row.ApplyCurrentCellState(CurrentCellSlot.Value);
                }
            }
        });
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        var row = new TableViewRow { TableView = this };

        row.SetBinding(HeightProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRow.TableView)}.{nameof(RowHeight)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });

        row.SetBinding(MaxHeightProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRow.TableView)}.{nameof(RowMaxHeight)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });

        row.SetBinding(MinHeightProperty, new Binding
        {
            Path = new PropertyPath($"{nameof(TableViewRow.TableView)}.{nameof(RowMinHeight)}"),
            RelativeSource = new RelativeSource { Mode = RelativeSourceMode.Self }
        });

        _rows.Add(row);

        return row;
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        var shiftKey = KeyboardHelper.IsShiftKeyDown();
        var ctrlKey = KeyboardHelper.IsCtrlKeyDown();

        if (HandleShortKeys(shiftKey, ctrlKey, e.Key))
        {
            e.Handled = true;
            return;
        }

        HandleNavigations(e, shiftKey, ctrlKey);
    }

    /// <summary>
    /// Handles navigation keys.
    /// </summary>
    private void HandleNavigations(KeyRoutedEventArgs e, bool shiftKey, bool ctrlKey)
    {
        var currentCell = CurrentCellSlot.HasValue ? GetCellFromSlot(CurrentCellSlot.Value) : default;

        if (e.Key is VirtualKey.F2 && currentCell is not null && !IsEditing)
        {
            currentCell.PrepareForEdit();
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.Escape && currentCell is not null && IsEditing)
        {
            SetIsEditing(false);
            currentCell?.SetElement();
            e.Handled = true;
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


            MakeSelection(newSlot, false);

            if (isEditing && currentCell is not null)
            {
                currentCell = GetCellFromSlot(newSlot);
                currentCell?.PrepareForEdit();
            }

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

    protected async override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _headerRow = GetTemplateChild("HeaderRow") as TableViewHeaderRow;
        _scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;

        if (IsLoaded)
        {
            while (ItemsPanelRoot is null) await Task.Yield();

            ApplyItemsClip();
            UpdateVerticalScrollBarMargin();
            EnsureAutoColumns();
        }
    }


    /// <summary>
    /// Handles the Loaded event of the TableView control.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureAutoColumns();
        ApplyItemsClip();
        UpdateVerticalScrollBarMargin();
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

        return new TableViewCellSlot(nextRow, nextColumn);
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
        package.SetText(GetSelectedContent(includeHeaders));
        Clipboard.SetContent(package);
    }

    /// <summary>
    /// Called before the CopyToClipboard event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnCopyToClipboard(TableViewCopyToClipboardEventArgs args)
    {
        CopyToClipboard?.Invoke(this, args);
    }

    /// <summary>
    /// Returns the selected cells' or rows' content as a string, optionally including headers, with values separated by the given character.
    /// </summary>
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values (default is tab).</param>
    /// <returns>A string of selected cell content separated by the specified character.</returns>
    public string GetSelectedContent(bool includeHeaders, char separator = '\t')
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

        return GetCellsContent(slots, includeHeaders, separator);
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
    /// <param name="includeHeaders">Whether to include headers in the output.</param>
    /// <param name="separator">The character used to separate cell values.</param>
    /// <returns>A string of specified cell content separated by the specified character.</returns>
    public string GetCellsContent(IEnumerable<TableViewCellSlot> slots, bool includeHeaders, char separator = '\t')
    {
        if (!slots.Any())
        {
            return string.Empty;
        }

        var minColumn = slots.Select(x => x.Column).Min();
        var maxColumn = slots.Select(x => x.Column).Max();

        var stringBuilder = new StringBuilder();
        var properties = new Dictionary<string, (PropertyInfo, object?)[]>();

        if (includeHeaders)
        {
            stringBuilder.AppendLine(GetHeadersContent(separator, minColumn, maxColumn));
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

                stringBuilder.Append($"{column.GetCellContent(item)}{separator}");
            }

            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            stringBuilder.Append('\n');
        }

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

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Generates columns based on the types of the properties of the ItemsSource collection type.
    /// </summary>
    private void GenerateColumns()
    {
        var dataType = ItemsSource?.GetItemType();
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
    /// Called before the AutoGeneratingColumn event occurs.
    /// </summary>
    /// <param name="args">Cancelable event args.</param>
    protected virtual void OnAutoGeneratingColumn(TableViewAutoGeneratingColumnEventArgs args)
    {
        AutoGeneratingColumn?.Invoke(this, args);
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

        column.Binding = binding;

        return column;
    }

    /// <summary>
    /// Handles the ItemsSource property changed event.
    /// </summary>
    private void ItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        var defer = _collectionView.DeferRefresh();
        {
            _collectionView.Source = null!;

            if (e.NewValue is IList source)
            {
                EnsureAutoColumns();

                _collectionView.Source = source;
            }
        }
        defer.Complete();
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

#if WINDOWS
        try
        {
            var hWnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
            if (await GetStorageFile(hWnd) is not { } file)
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
#else
        await Task.CompletedTask;
#endif
    }

    /// <summary>
    /// Called before the ExportSelectedContent event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnExportSelectedContent(TableViewExportContentEventArgs args)
    {
        ExportSelectedContent?.Invoke(this, args);
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

#if WINDOWS
        try
        {
            var hWnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
            if (await GetStorageFile(hWnd) is not { } file)
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
#else
        await Task.CompletedTask;
#endif
    }

    /// <summary>
    /// Called before the ExportAllContent event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnExportAllContent(TableViewExportContentEventArgs args)
    {
        ExportAllContent?.Invoke(this, args);
    }

    /// <summary>
    /// Gets a storage file for saving the CSV.
    /// </summary>
    private static async Task<StorageFile> GetStorageFile(IntPtr hWnd)
    {
        var savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, hWnd);
        savePicker.FileTypeChoices.Add("CSV (Comma delimited)", [".csv"]);

        return await savePicker.PickSaveFileAsync();
    }

    /// <summary>
    /// Applies clipping to the items panel. This will allow header row to stay on top while scrolling
    /// </summary>
    private void ApplyItemsClip()
    {
#if WINDOWS
        if (_scrollViewer is null || ItemsPanelRoot is null) return;

        Canvas.SetZIndex(ItemsPanelRoot, -1);

        var scrollProperties = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(_scrollViewer);
        var compositor = scrollProperties.Compositor;
        var scrollPropSet = scrollProperties.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
        var itemsPanelVisual = ElementCompositionPreview.GetElementVisual(ItemsPanelRoot);
        var contentClip = compositor.CreateInsetClip();
        var expressionClipAnimation = ExpressionFunctions.Max(-scrollPropSet.Translation.Y, 0);

        itemsPanelVisual.Clip = contentClip;
        contentClip.TopInset = (float)Math.Max(-_scrollViewer.VerticalOffset, 0);
        contentClip.StartAnimation("TopInset", expressionClipAnimation);
#endif
    }

    /// <summary>
    /// Updates the margin of the vertical scroll bar to always show under the header row.
    /// </summary>
    private void UpdateVerticalScrollBarMargin()
    {
        if (GetTemplateChild("ScrollViewer") is ScrollViewer scrollViewer)
        {
            var verticalScrollBar = scrollViewer.FindDescendant<ScrollBar>(x => x.Name == "VerticalScrollBar");
            if (verticalScrollBar is not null)
            {
                verticalScrollBar.Margin = new Thickness(0, HeaderRowHeight, 0, 0);
            }
        }
    }

    /// <summary>
    /// Clears all sorting applied to the items.
    /// </summary>
    public void ClearAllSorting()
    {
        DeselectAll();
        SortDescriptions.Clear();

        foreach (var column in Columns)
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
            cell?.SetElement();
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
            CellSelectionChanged?.Invoke(this, new TableViewCellSelectionChangedEventArgs(removedCells, addedCells));
        }
    }

    /// <summary>
    /// Scrolls the specified cell slot into view.
    /// </summary>
    /// <param name="slot">The cell slot to scroll into view.</param>
    public async Task<TableViewCell> ScrollCellIntoView(TableViewCellSlot slot)
    {
        if (_scrollViewer is null || !slot.IsValid(this)) return default!;

        var row = await ScrollRowIntoView(slot.Row);
        var (start, end) = GetColumnsInDisplay();
        var xOffset = 0d;
        var yOffset = _scrollViewer.VerticalOffset;

        if (slot.Column < start)
        {
            for (var i = 0; i < slot.Column; i++)
            {
                xOffset += Columns.VisibleColumns[i].ActualWidth;
            }
        }
        else if (slot.Column > end)
        {
            for (var i = 0; i <= slot.Column; i++)
            {
                xOffset += Columns.VisibleColumns[i].ActualWidth;
            }

            var change = xOffset - _scrollViewer.HorizontalOffset - (_scrollViewer.ViewportWidth - SelectionIndicatorWidth);
            xOffset = _scrollViewer.HorizontalOffset + change;
        }
        else if (row is not null)
        {
            return row.Cells.ElementAt(slot.Column);
        }

        var tcs = new TaskCompletionSource<object?>();

        void ViewChanged(object? _, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                return;
            }

            tcs.TrySetResult(result: default);
        }

        try
        {
            _scrollViewer.ViewChanged += ViewChanged;
            _scrollViewer.ChangeView(xOffset, yOffset, null, true);
            _scrollViewer.ScrollToHorizontalOffset(xOffset);
            await tcs.Task;
        }
        finally
        {
            _scrollViewer.ViewChanged -= ViewChanged;
        }

        return row?.Cells.ElementAt(slot.Column)!;
    }

    /// <summary>
    /// Scrolls the specified row into view.
    /// </summary>
    /// <param name="index">The index of the row to scroll into view.</param>
    public async Task<TableViewRow?> ScrollRowIntoView(int index)
    {
        if (_scrollViewer is null) return default!;

        var item = Items[index];
        index = Items.IndexOf(item); // if the ItemsSource has duplicate items in it. ScrollIntoView will only bring first index of item.
        ScrollIntoView(item);

        var tries = 0;
        while (tries < 10)
        {
            if (ContainerFromIndex(index) is TableViewRow row)
            {
                var transform = row.TransformToVisual(_scrollViewer);
                var positionInScrollViewer = transform.TransformPoint(new Point(0, 0));
                if ((index == 0 && _scrollViewer.VerticalOffset > 0) || (index > 0 && positionInScrollViewer.Y < HeaderRowHeight))
                {
                    var xOffset = _scrollViewer.HorizontalOffset;
                    var yOffset = index == 0 ? 0d : _scrollViewer.VerticalOffset - row.ActualHeight + positionInScrollViewer.Y + 8;
                    var tcs = new TaskCompletionSource<object?>();

                    try
                    {
                        _scrollViewer.ViewChanged += ViewChanged;
                        _scrollViewer.ChangeView(xOffset, yOffset, null, true);
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

            tries++;
            await Task.Delay(1); // let the animation complete
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
        foreach (var column in Columns.VisibleColumns)
        {
            if (width >= _scrollViewer.HorizontalOffset && width + column.ActualWidth <= _scrollViewer.HorizontalOffset + _scrollViewer.ViewportWidth - SelectionIndicatorWidth)
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

        foreach (var row in _rows)
        {
            row.EnsureLayout();
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
            row.EnsureGridLines();
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
        RowContextFlyoutOpening?.Invoke(this, eventArgs);

        if (RowContextFlyout is not null && !eventArgs.Handled)
        {
            var presenter = row.FindDescendant<ListViewItemPresenter>();

            RowContextFlyout.ShowAt(row, new FlyoutShowOptions
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
        CellContextFlyoutOpening?.Invoke(this, eventArgs);

        if (CellContextFlyout is not null && !eventArgs.Handled)
        {
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

    public virtual void OnSorting(TableViewSortingEventArgs eventArgs)
    {
        Sorting?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Called before the ClearSorting event occurs.
    /// </summary>
    /// <param name="eventArgs">The event data.</param>
    public virtual void OnClearSorting(TableViewClearSortingEventArgs eventArgs)
    {
        ClearSorting?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Event triggered when a column is auto-generating.
    /// </summary>
    public event EventHandler<TableViewAutoGeneratingColumnEventArgs>? AutoGeneratingColumn;

    /// <summary>
    /// Event triggered when exporting all rows content.
    /// </summary>
    public event EventHandler<TableViewExportContentEventArgs>? ExportAllContent;

    /// <summary>
    /// Event triggered when exporting selected rows or cells content.
    /// </summary>
    public event EventHandler<TableViewExportContentEventArgs>? ExportSelectedContent;

    /// <summary>
    /// Event triggered when copying selected rows or cell content to the clipboard.
    /// </summary>
    public event EventHandler<TableViewCopyToClipboardEventArgs>? CopyToClipboard;

    /// <summary>
    /// Event triggered when the IsReadOnly property changes.
    /// </summary>
    public event DependencyPropertyChangedEventHandler? IsReadOnlyChanged;

    /// <summary>
    /// Event triggered when the row context flyout is opening.
    /// </summary>
    public event EventHandler<TableViewRowContextFlyoutEventArgs>? RowContextFlyoutOpening;

    /// <summary>
    /// Event triggered when the cell context flyout is opening.
    /// </summary>
    public event EventHandler<TableViewCellContextFlyoutEventArgs>? CellContextFlyoutOpening;

    /// <summary>
    /// Occurs when a sorting is being applied to a column in the TableView.
    /// </summary>
    public event EventHandler<TableViewSortingEventArgs>? Sorting;

    /// <summary>
    /// Occurs when sorting is cleared from a column in the TableView.
    /// </summary>
    public event EventHandler<TableViewClearSortingEventArgs>? ClearSorting;

    /// <summary>
    /// Event triggered when selected cells change.
    /// </summary>
    public event EventHandler<TableViewCellSelectionChangedEventArgs>? CellSelectionChanged;

    /// <summary>
    /// Event triggered when the current cell changes.
    /// </summary>
    public event DependencyPropertyChangedEventHandler? CurrentCellChanged;
}
