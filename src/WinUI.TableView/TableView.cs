using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations.Expressions;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
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
            return;

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
    }

    private bool Filter(object obj)
    {
        return ActiveFilters.All(item => item.Value(obj));
    }

    internal void CopyToClipboard(bool includeHeaders)
    {
        var package = new DataPackage();
        package.SetText(GetSelectedRowsContent(includeHeaders));
        Clipboard.SetContent(package);
    }

    public string GetSelectedRowsContent(bool includeHeaders, char separator = '\t')
    {
        return GetRowsContent(SelectedItems, includeHeaders, separator);
    }

    public string GetAllRowsContent(bool includeHeaders, char separator = '\t')
    {
        return GetRowsContent(Items, includeHeaders, separator);
    }

    private string GetRowsContent(IList<object> items, bool includeHeaders, char separator)
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
                if (!properties.TryGetValue(property, out (PropertyInfo, object?)[]? pis))
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

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableView tableView)
        {
            tableView.OnItemsSourceChanged(e);
        }
    }

    public IAdvancedCollectionView CollectionView { get; private set; } = new AdvancedCollectionView();

    internal IDictionary<string, Predicate<object>> ActiveFilters { get; } = new Dictionary<string, Predicate<object>>();

    public TableViewColumnsColection Columns
    {
        get => (TableViewColumnsColection)GetValue(ColumnsProperty);
        private set => SetValue(ColumnsProperty, value);
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


    public static readonly new DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(TableView), new PropertyMetadata(null, OnItemsSourceChanged));
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(TableViewColumnsColection), typeof(TableView), new PropertyMetadata(null));
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(TableView), new PropertyMetadata(40d));
    public static readonly DependencyProperty RowMaxHeightProperty = DependencyProperty.Register(nameof(RowMaxHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity));
    public static readonly DependencyProperty ShowExportOptionsProperty = DependencyProperty.Register(nameof(ShowExportOptions), typeof(bool), typeof(TableView), new PropertyMetadata(false));
    public static readonly DependencyProperty AutoGenerateColumnsProperty = DependencyProperty.Register(nameof(AutoGenerateColumns), typeof(bool), typeof(TableView), new PropertyMetadata(true));
    
    public event EventHandler<TableViewAutoGeneratingColumnEventArgs>? AutoGeneratingColumn;
}
