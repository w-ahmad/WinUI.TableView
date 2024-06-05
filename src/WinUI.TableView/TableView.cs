using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations.Expressions;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

namespace WinUI.TableView;
public partial class TableView : ListView
{
    private TableViewHeaderRow? _headerRow;
    private ScrollViewer _scrollViewer = null!;    
    private bool _shouldThrowSelectionModeChangedException;

    public TableView()
    {
        DefaultStyleKey = typeof(TableView);

        CollectionView.Filter = Filter;
        base.ItemsSource = CollectionView;
        base.SelectionMode = ListViewSelectionMode.Extended;
        RegisterPropertyChangedCallback(ItemsControl.ItemsSourceProperty, OnBaseItemsSourceChanged);
        RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, OnBaseSelectionModeChanged);
        Loaded += OnLoaded;
        SelectionChanged += TableView_SelectionChanged;
    }

    private void TableView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!KeyBoardHelper.IsCtrlKeyDown())
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

        CurrentCell?.ApplyCurrentCellState(false);
        CurrentCell = null;

        OnCellSelectionChanged();
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        if (element is TableViewRow row)
        {
            var index = IndexFromContainer(element);
            row.SetValue(TableViewRow.IndexProperty, index);
            row.ApplyCellsSelectionState();
        }
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new TableViewRow { TableView = this };
    }

    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        var shiftKey = KeyBoardHelper.IsShiftKeyDown();
        var ctrlKey = KeyBoardHelper.IsCtrlKeyDown();
        var currentSlot = CurrentCell?.Slot;

        if (e.Key is VirtualKey.F2 && CurrentCell is not null)
        {
            CurrentCell.PrepareForEdit();
            e.Handled = true;
        }
        else if (e.Key is VirtualKey.Escape && CurrentCell is not null && IsEditing)
        {
            CurrentCell?.SetElement();
        }
        else if (e.Key is VirtualKey.Space && CurrentCell is not null && !IsEditing)
        {
            if (!CurrentCell.IsSelected)
            {
                SelectCells(CurrentCell.Slot, shiftKey, ctrlKey);
            }
            else
            {
                DeselectCell(CurrentCell);
            }
        }
        // Handle navigation keys
        else if (e.Key is VirtualKey.Tab or VirtualKey.Enter)
        {
            var isEditing = IsEditing;
            var row = currentSlot?.Row ?? 0;
            var column = currentSlot?.Column ?? 0;

            if (e.Key is VirtualKey.Tab)
            {
                column += shiftKey ? -1 : +1;
            }
            else
            {
                row += shiftKey ? -1 : +1;
            }
            var newSlot = new TableViewCellSlot(row, column);
            SelectCells(newSlot, false);

            if (isEditing)
            {
                CurrentCell?.PrepareForEdit();
            }

            e.Handled = true;
        }
        else if ((e.Key is VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down)
                 && SelectionUnit is not TableViewSelectionUnit.Row
                 && !IsEditing)
        {
            var row = currentSlot?.Row ?? 0;
            var column = currentSlot?.Column ?? 0;

            if (e.Key is VirtualKey.Left or VirtualKey.Right)
            {
                column = e.Key is VirtualKey.Left ? ctrlKey ? 0 : column - 1 : ctrlKey ? Columns.Count - 1 : column + 1;
            }
            else
            {
                row = e.Key == VirtualKey.Up ? ctrlKey ? 0 : row - 1 : ctrlKey ? Items.Count - 1 : row + 1;
            }

            var newSlot = new TableViewCellSlot(row, column);
            SelectCells(newSlot, shiftKey);
            e.Handled = true;
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _headerRow = GetTemplateChild("HeaderRow") as TableViewHeaderRow;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (GetTemplateChild("ScrollViewer") is not ScrollViewer scrollViewer)
        {
            return;
        }

        _scrollViewer = scrollViewer;
        Canvas.SetZIndex(ItemsPanelRoot, -1);

        var scrollProperties = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
        var compositor = scrollProperties.Compositor;
        var scrollPropSet = scrollProperties.GetSpecializedReference<ManipulationPropertySetReferenceNode>();
        var itemsPanelVisual = ElementCompositionPreview.GetElementVisual(ItemsPanelRoot);
        var contentClip = compositor.CreateInsetClip();
        var expressionClipAnimation = ExpressionFunctions.Max(-scrollPropSet.Translation.Y, 0);

        itemsPanelVisual.Clip = contentClip;
        contentClip.TopInset = (float)Math.Max(-scrollViewer.VerticalOffset, 0);
        contentClip.StartAnimation("TopInset", expressionClipAnimation);

        UpdateVerticalScrollBarMargin();
    }

    private bool Filter(object obj)
    {
        return ActiveFilters.All(item => item.Value(obj));
    }

    internal void CopyToClipboardInternal(bool includeHeaders)
    {
        var args = new TableViewCopyToClipboardEventArgs(includeHeaders);
        OnCopyToClipboard(args);

        if (args.Handled)
        {
            return;
        }

        var package = new DataPackage();
        package.SetText(GetSelectedRowsContent(includeHeaders));
        Clipboard.SetContent(package);
    }

    protected virtual void OnCopyToClipboard(TableViewCopyToClipboardEventArgs args)
    {
        CopyToClipboard?.Invoke(this, args);
    }

    public string GetSelectedRowsContent(bool includeHeaders, char separator = '\t')
    {
        var items = SelectedItems.OrderBy(item2 => Items.IndexOf(item2));
        return GetRowsContent(items, includeHeaders, separator);
    }

    public string GetAllRowsContent(bool includeHeaders, char separator = '\t')
    {
        return GetRowsContent(Items, includeHeaders, separator);
    }

    private string GetRowsContent(IEnumerable<object> items, bool includeHeaders, char separator)   
    {
        var stringBuilder = new StringBuilder();
        var properties = new Dictionary<string, (PropertyInfo, object?)[]>();

        if (includeHeaders)
        {
            stringBuilder.AppendLine(GetHeadersContent(separator));
        }

        foreach (var item in items)
        {
            var type = ItemsSource?.GetType() is { } listType && listType.IsGenericType ? listType.GetGenericArguments()[0] : item?.GetType();
            foreach (var column in Columns.VisibleColumns.OfType<TableViewBoundColumn>())
            {
                var property = column.Binding.Path.Path;
                if (!properties.TryGetValue(property, out var pis))
                {
                    stringBuilder.Append($"{item.GetValue(type, property, out pis)}{separator}");
                    properties.Add(property, pis);
                }
                else
                {
                    stringBuilder.Append($"{item.GetValue(pis)}{separator}");
                }
            }

            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            stringBuilder.Append('\n');
        }

        return stringBuilder.ToString();
    }

    private string GetHeadersContent(char separator)
    {
        return string.Join(separator, Columns.VisibleColumns.OfType<TableViewBoundColumn>().Select(x => x.Header));
    }

    internal async void SelectNextRow()
    {
        var nextIndex = SelectedIndex + 1;
        if (nextIndex < Items.Count)
        {
            SelectedIndex = nextIndex;
        }
        else if (TabNavigation == KeyboardNavigationMode.Cycle)
        {
            SelectedIndex = 0;
        }
        else
        {
            return;
        }

        await Task.Delay(5);
        var listViewItem = ContainerFromItem(SelectedItem) as ListViewItem;
        var row = listViewItem?.FindDescendant<TableViewRow>();
        row?.SelectNextCell(null);
    }

    internal async void SelectPreviousRow()
    {
        var previousIndex = SelectedIndex - 1;
        if (previousIndex >= 0)
        {
            SelectedIndex = previousIndex;
        }
        else if (TabNavigation == KeyboardNavigationMode.Cycle)
        {
            SelectedIndex = Items.Count - 1;
        }
        else
        {
            return;
        }

        await Task.Delay(5);
        var listViewItem = ContainerFromItem(SelectedItem) as ListViewItem;
        var row = listViewItem?.FindDescendant<TableViewRow>();
        row?.SelectPreviousCell(null);
    }

    private void GenerateColumns()
    {
        var itemsSourceType = ItemsSource?.GetType();
        if (itemsSourceType?.IsGenericType == true)
        {
            var dataType = itemsSourceType.GenericTypeArguments[0];
            foreach (var propertyInfo in dataType.GetProperties())
            {
                var displayAttribute = propertyInfo.GetCustomAttributes().OfType<DisplayAttribute>().FirstOrDefault();
                var autoGenerateField = displayAttribute?.GetAutoGenerateField();
                if (autoGenerateField == false)
                {
                    continue;
                }

                var header = displayAttribute?.GetShortName() ?? propertyInfo.Name;
                var canFilter = displayAttribute?.GetAutoGenerateFilter() ?? true;
                var columnArgs = GenerateColumn(propertyInfo.PropertyType, propertyInfo.Name, header, canFilter);
                OnAutoGeneratingColumn(columnArgs);

                if (!columnArgs.Cancel && columnArgs.Column is not null)
                {
                    Columns.Insert(displayAttribute?.GetOrder() ?? Columns.Count, columnArgs.Column);
                }
            }
        }
    }

    private static TableViewAutoGeneratingColumnEventArgs GenerateColumn(Type propertyType, string propertyName, string header, bool canFilter)
    {
        var newColumn = GetTableViewColumnFromType(propertyType);
        newColumn.Header = header;
        newColumn.CanFilter = canFilter;
        newColumn.IsAutoGenerated = true;
        newColumn.Binding = new Binding { Path = new PropertyPath(propertyName), Mode = BindingMode.TwoWay };
        return new TableViewAutoGeneratingColumnEventArgs(propertyName, propertyType, newColumn);
    }

    protected virtual void OnAutoGeneratingColumn(TableViewAutoGeneratingColumnEventArgs e)
    {
        AutoGeneratingColumn?.Invoke(this, e);
    }

    private static TableViewBoundColumn GetTableViewColumnFromType(Type type)
    {
        if (type == typeof(bool) || type == typeof(bool?))
        {
            return new TableViewCheckBoxColumn();
        }
        else if (type == typeof(int) || type == typeof(int?) ||
                 type == typeof(double) || type == typeof(double?) ||
                 type == typeof(float) || type == typeof(float?) ||
                 type == typeof(decimal) || type == typeof(decimal?))
        {
            return new TableViewNumberColumn();
        }

        return new TableViewTextColumn();
    }

    private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        ((AdvancedCollectionView)CollectionView).Source = null!;

        if (e.NewValue is IList source)
        {
            if (AutoGenerateColumns)
            {
                RemoveAutoGeneratedColumns();
                GenerateColumns();
            }

            ((AdvancedCollectionView)CollectionView).Source = source;
        }
    }

    private void RemoveAutoGeneratedColumns()
    {
        while (Columns.Any(x => x.IsAutoGenerated))
        {
            var autoGeneratedColumn = Columns.First(x => x.IsAutoGenerated);
            Columns.Remove(autoGeneratedColumn);
        }
    }

    internal async void ExportSelectedToCSV()
    {
        var args = new TableViewExportRowsContentEventArgs();
        OnExportSelectedRowsContent(args);

        if (args.Handled)
        {
            return;
        }

        try
        {
            var hWnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
            if (await GetStorageFile(hWnd) is not { } file)
            {
                return;
            }

            var content = GetSelectedRowsContent(true, ',');
            using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);

            using var tw = new StreamWriter(stream);
            await tw.WriteAsync(content);
        }
        catch { }
    }

    protected virtual void OnExportSelectedRowsContent(TableViewExportRowsContentEventArgs args)
    {
        ExportSelectedRowsContent?.Invoke(this, args);
    }

    internal async void ExportAllToCSV()
    {
        var args = new TableViewExportRowsContentEventArgs();
        OnExportAllRowsContent(args);

        if (args.Handled)
        {
            return;
        }

        try
        {
            var hWnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
            if (await GetStorageFile(hWnd) is not { } file)
            {
                return;
            }

            var content = GetAllRowsContent(true, ',');
            using var stream = await file.OpenStreamForWriteAsync();
            stream.SetLength(0);

            using var tw = new StreamWriter(stream);
            await tw.WriteAsync(content);
        }
        catch { }
    }

    protected virtual void OnExportAllRowsContent(TableViewExportRowsContentEventArgs args)
    {
        ExportAllRowsContent?.Invoke(this, args);
    }

    private static async Task<StorageFile> GetStorageFile(IntPtr hWnd)
    {
        var savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, hWnd);
        savePicker.FileTypeChoices.Add("CSV (Comma delimited)", new List<string>() { ".csv" });

        return await savePicker.PickSaveFileAsync();
    }

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

    internal void ClearSorting()
    {
        CollectionView.SortDescriptions.Clear();

        foreach (var header in Columns.Select(x => x.HeaderControl))
        {
            if (header is not null)
            {
                header.SortDirection = null;
            }
        }
    }

    internal void ClearFilters()
    {
        ActiveFilters.Clear();
        CollectionView.RefreshFilter();

        foreach (var header in Columns.Select(x => x.HeaderControl))
        {
            if (header is not null)
            {
                header.IsFiltered = false;
            }
        }
    }

    private bool IsValidSlot(TableViewCellSlot slot)
    {
        return slot.Row >= 0 && slot.Column >= 0 && slot.Row < Items.Count && slot.Column < Columns.Count;
    }

    internal new void SelectAll()
    {
        if (IsEditing)
        {
            return;
        }

        if (SelectionUnit is TableViewSelectionUnit.Cell)
        {
            SelectAllCells();
            CurrentCell?.ApplyCurrentCellState(false);
            CurrentCell = null;
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

    private void SelectAllCells()
    {
        switch (SelectionMode)
        {
            case ListViewSelectionMode.Single:
                if (Items.Count > 0 && Columns.Count > 0)
                {
                    SelectedCellRanges.Clear();
                    SelectedCellRanges.Add(new() { new TableViewCellSlot(0, 0) });
                }
                break;
            case ListViewSelectionMode.Multiple:
            case ListViewSelectionMode.Extended:
                SelectedCellRanges.Clear();
                var selectionRange = new HashSet<TableViewCellSlot>();

                for (var row = 0; row < Items.Count; row++)
                {
                    for (var column = 0; column < Columns.Count; column++)
                    {
                        selectionRange.Add(new TableViewCellSlot(row, column));
                    }
                }
                SelectedCellRanges.Add(selectionRange);
                break;
        }

        OnCellSelectionChanged();
    }

    internal void DeselectAll()
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

        DeselectAllCells();
    }

    private void DeselectAllCells()
    {
        SelectedCellRanges.Clear();
        OnCellSelectionChanged();
    }

    internal async void SelectCells(TableViewCellSlot slot, bool shiftKey, bool ctrlKey = false)
    {
        if (!IsValidSlot(slot))
        {
            return;
        }

        if (SelectionMode != ListViewSelectionMode.None && SelectionUnit != TableViewSelectionUnit.Row)
        {
            ctrlKey = ctrlKey || SelectionMode is ListViewSelectionMode.Multiple;
            if (!ctrlKey || !(SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended))
            {
                DeselectAll();
            }

            var selectionRange = (SelectionStartCell is null ? null : SelectedCellRanges.LastOrDefault(x => x.Contains(SelectionStartCell.Slot))) ?? new HashSet<TableViewCellSlot>();
            SelectedCellRanges.Remove(selectionRange);
            selectionRange.Clear();
            SelectionStartCell ??= CurrentCell;
            if (shiftKey && SelectionMode is ListViewSelectionMode.Multiple or ListViewSelectionMode.Extended)
            {
                var currentSlot = SelectionStartCell?.Slot ?? slot;
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
                SelectionStartCell = null;
                selectionRange.Add(slot);

                if (SelectedCellRanges.LastOrDefault(x => x.Contains(slot)) is { } range)
                {
                    range.Remove(slot);
                }
            }

            CurrentCell?.ApplyCurrentCellState(false);
            CurrentCell = await ScrollCellIntoView(slot);
            CurrentCell?.ApplyCurrentCellState(true);

            SelectedCellRanges.Add(selectionRange);
            OnCellSelectionChanged();
        }
        else
        {
            CurrentCell?.ApplyCurrentCellState(false);
            CurrentCell = await ScrollCellIntoView(slot);
            CurrentCell?.ApplyCurrentCellState(true);
        }
    }

    internal void DeselectCell(TableViewCell cell)
    {
        var selectionRange = SelectedCellRanges.LastOrDefault(x => x.Contains(cell.Slot));
        selectionRange?.Remove(cell.Slot);

        if (selectionRange?.Count == 0)
        {
            SelectedCellRanges.Remove(selectionRange);
        }

        CurrentCell?.ApplyCurrentCellState(false);
        CurrentCell = cell;
        CurrentCell?.ApplyCurrentCellState(true);

        OnCellSelectionChanged();
    }

    private void OnCellSelectionChanged()
    {
        var oldSelection = SelectedCells;
        SelectedCells = new HashSet<TableViewCellSlot>(SelectedCellRanges.SelectMany(x => x));
        SelectedCellsChanged?.Invoke(this, new CellSelectionChangedEvenArgs(oldSelection, SelectedCells));
    }

    private async Task<TableViewCell> ScrollCellIntoView(TableViewCellSlot slot)
    {
        var row = await ScrollRowIntoView(slot.Row);
        var (start, end) = GetColumnsInDisplay();

        var offset = 0d;
        if (slot.Column < start)
        {
            for (var i = 0; i < slot.Column; i++)
            {
                offset += Columns[i].ActualWidth;
            }
        }
        else if (slot.Column > end)
        {
            for (var i = 0; i <= slot.Column; i++)
            {
                offset += Columns[i].ActualWidth;
            }

            var change = offset - _scrollViewer.HorizontalOffset - (_scrollViewer.ViewportWidth - 16);
            offset = _scrollViewer.HorizontalOffset + change;
        }
        else if (row is not null)
        {
            return row.Cells.ElementAt(slot.Column);
        }

        _scrollViewer.ScrollToHorizontalOffset(offset);

        return row?.Cells.ElementAt(slot.Column)!;
    }

    private async Task<TableViewRow?> ScrollRowIntoView(int index)
    {
        ScrollIntoView(Items[index]);

        var tries = 0;
        while (tries < 10)
        {
            if (ContainerFromIndex(index) is TableViewRow row)
            {
                if (index == 0)
                {
                    _scrollViewer.ScrollToVerticalOffset(0);
                }
                else
                {
                    var transform = row.TransformToVisual(_scrollViewer);
                    var positionInScrollViewer = transform.TransformPoint(new Point(0, 0));
                    if (positionInScrollViewer.Y <= row.ActualHeight)
                    {
                        var offset = _scrollViewer.VerticalOffset - row.ActualHeight + positionInScrollViewer.Y + 8;
                        _scrollViewer.ScrollToVerticalOffset(offset);
                    }
                }

                return row;
            }

            tries++;
            await Task.Delay(1); // let the animation complete
        }

        return default;
    }

    private (int start, int end) GetColumnsInDisplay()
    {
        var start = -1;
        var end = -1;
        var width = 0d;
        foreach (var column in Columns)
        {
            if (width >= _scrollViewer.HorizontalOffset && width + column.ActualWidth <= _scrollViewer.HorizontalOffset + _scrollViewer.ViewportWidth)
            {
                if (start == -1)
                {
                    start = Columns.IndexOf(column);
                }
                else
                {
                    end = Columns.IndexOf(column);
                }
            }

            width += column.ActualWidth;
        }

        return (start, end);
    }

    private void UpdateBaseSelectionMode()
    {
        _shouldThrowSelectionModeChangedException = true;
        base.SelectionMode = SelectionUnit is TableViewSelectionUnit.Cell ? ListViewSelectionMode.None : SelectionMode;
        _shouldThrowSelectionModeChangedException = false;
    }

    public event EventHandler<TableViewAutoGeneratingColumnEventArgs>? AutoGeneratingColumn;
    public event EventHandler<TableViewExportRowsContentEventArgs>? ExportAllRowsContent;
    public event EventHandler<TableViewExportRowsContentEventArgs>? ExportSelectedRowsContent;
    public event EventHandler<TableViewCopyToClipboardEventArgs>? CopyToClipboard;
    internal event EventHandler<CellSelectionChangedEvenArgs>? SelectedCellsChanged;
}

internal class CellSelectionChangedEvenArgs : EventArgs
{
    public CellSelectionChangedEvenArgs(HashSet<TableViewCellSlot> oldSelection,
                                        HashSet<TableViewCellSlot> newSelection)
    {
        OldSelection = oldSelection;
        NewSelection = newSelection;
    }

    public HashSet<TableViewCellSlot> OldSelection { get; }
    public HashSet<TableViewCellSlot> NewSelection { get; }
}