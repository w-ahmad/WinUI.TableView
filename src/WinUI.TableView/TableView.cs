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
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;
public class TableView : ListView
{
    public TableView()
    {
        DefaultStyleKey = typeof(TableView);

        Columns = new();
        CollectionView.Filter = Filter;
        base.ItemsSource = CollectionView;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (GetTemplateChild("ScrollViewer") is not ScrollViewer scrollViewer)
        {
            return;
        }

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
            foreach (var column in Columns.OfType<TableViewBoundColumn>())
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
        return string.Join(separator, Columns.OfType<TableViewBoundColumn>().Select(x => x.Header));
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

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.OnItemsSourceChanged(e);
        }
    }

    private static void OnHeaderRowHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        (d as TableView)?.UpdateVerticalScrollBarMargin();
    }

    private static void OnAutoGenerateColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            if (tableView.AutoGenerateColumns)
            {
                tableView.GenerateColumns();
            }
            else
            {
                tableView.RemoveAutoGeneratedColumns();
            }
        }
    }

    private static void OnCanSortColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView && e.NewValue is false)
        {
            tableView.ClearSorting();
        }
    }

    private static void OnCanFilterColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView && e.NewValue is false)
        {
            tableView.ClearFilters();
        }
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

    public IAdvancedCollectionView CollectionView { get; private set; } = new AdvancedCollectionView();

    internal IDictionary<string, Predicate<object>> ActiveFilters { get; } = new Dictionary<string, Predicate<object>>();

    public TableViewColumnsCollection Columns
    {
        get => (TableViewColumnsCollection)GetValue(ColumnsProperty);
        private set => SetValue(ColumnsProperty, value);
    }

    public double HeaderRowHeight
    {
        get => (double)GetValue(HeaderRowHeightProperty);
        set => SetValue(HeaderRowHeightProperty, value);
    }

    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public double RowMaxHeight
    {
        get => (double)GetValue(RowMaxHeightProperty);
        set => SetValue(RowMaxHeightProperty, value);
    }

    public new IList ItemsSource
    {
        get => (IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public bool ShowExportOptions
    {
        get => (bool)GetValue(ShowExportOptionsProperty);
        set => SetValue(ShowExportOptionsProperty, value);
    }

    public bool AutoGenerateColumns
    {
        get => (bool)GetValue(AutoGenerateColumnsProperty);
        set => SetValue(AutoGenerateColumnsProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool ShowOptionsButton
    {
        get => (bool)GetValue(ShowOptionsButtonProperty);
        set => SetValue(ShowOptionsButtonProperty, value);
    }

    public bool CanResizeColumns
    {
        get => (bool)GetValue(CanResizeColumnsProperty);
        set => SetValue(CanResizeColumnsProperty, value);
    }

    public bool CanSortColumns
    {
        get => (bool)GetValue(CanSortColumnsProperty);
        set => SetValue(CanSortColumnsProperty, value);
    }

    public bool CanFilterColumns
    {
        get => (bool)GetValue(CanFilterColumnsProperty);
        set => SetValue(CanFilterColumnsProperty, value);
    }

    public static readonly new DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(TableView), new PropertyMetadata(null, OnItemsSourceChanged));
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(TableViewColumnsCollection), typeof(TableView), new PropertyMetadata(null));
    public static readonly DependencyProperty HeaderRowHeightProperty = DependencyProperty.Register(nameof(HeaderRowHeight), typeof(double), typeof(TableView), new PropertyMetadata(32d, OnHeaderRowHeightChanged));
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(TableView), new PropertyMetadata(40d));
    public static readonly DependencyProperty RowMaxHeightProperty = DependencyProperty.Register(nameof(RowMaxHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity));
    public static readonly DependencyProperty ShowExportOptionsProperty = DependencyProperty.Register(nameof(ShowExportOptions), typeof(bool), typeof(TableView), new PropertyMetadata(false));
    public static readonly DependencyProperty AutoGenerateColumnsProperty = DependencyProperty.Register(nameof(AutoGenerateColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true, OnAutoGenerateColumnsChanged));
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TableView), new PropertyMetadata(false));
    public static readonly DependencyProperty ShowOptionsButtonProperty = DependencyProperty.Register(nameof(ShowOptionsButton), typeof(bool), typeof(TableView), new PropertyMetadata(true));
    public static readonly DependencyProperty CanResizeColumnsProperty = DependencyProperty.Register(nameof(CanResizeColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true));
    public static readonly DependencyProperty CanSortColumnsProperty = DependencyProperty.Register(nameof(CanSortColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true, OnCanSortColumnsChanged));
    public static readonly DependencyProperty CanFilterColumnsProperty = DependencyProperty.Register(nameof(CanFilterColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true, OnCanFilterColumnsChanged));

    public event EventHandler<TableViewAutoGeneratingColumnEventArgs>? AutoGeneratingColumn;
    public event EventHandler<TableViewExportRowsContentEventArgs>? ExportAllRowsContent;
    public event EventHandler<TableViewExportRowsContentEventArgs>? ExportSelectedRowsContent;
    public event EventHandler<TableViewCopyToClipboardEventArgs>? CopyToClipboard;
}
