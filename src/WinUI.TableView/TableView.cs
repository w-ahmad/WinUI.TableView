using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations.Expressions;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
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

    protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
    {
        base.OnProcessKeyboardAccelerators(args);

        if (args.Key == VirtualKey.C && args.Modifiers is VirtualKeyModifiers.Control or (VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift))
        {
            CopyToClipboard(args.Modifiers is (VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));
            args.Handled = true;
        }
        else if (args.Key == VirtualKey.A && args.Modifiers == (VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift))
        {
            this.DeselectAll();
            args.Handled = true;
        }
    }

    internal void CopyToClipboard(bool includeHeaders)
    {
        var package = new DataPackage();
        package.SetText(GetClipboardContent(includeHeaders));
        Clipboard.SetContent(package);
    }

    private string GetClipboardContent(bool includeHeaders)
    {
        var stringBuilder = new StringBuilder();
        var properties = new Dictionary<string, (PropertyInfo, object?)[]>();

        if (includeHeaders)
        {
            stringBuilder.AppendLine(string.Join('\t', Columns.OfType<TableViewBoundColumn>().Select(x => x.Header)));
        }

        foreach (var item in SelectedItems)
        {
            var type = ItemsSource?.GetType() is { } listType && listType.IsGenericType ? listType.GetGenericArguments()[0] : item?.GetType();
            foreach (var column in Columns.OfType<TableViewBoundColumn>())
            {
                var property = column.Binding.Path.Path;
                if (!properties.TryGetValue(property, out (PropertyInfo, object?)[]? pis))
                {
                    stringBuilder.Append($"{item.GetValue(type, property, out pis)}\t");
                    properties.Add(property, pis);
                }
                else
                {
                    stringBuilder.Append($"{item.GetValue(pis)}\t");
                }
            }

            stringBuilder.Append('\n');
        }

        return stringBuilder.ToString();
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

    private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is IList source)
        {
            ((AdvancedCollectionView)CollectionView).Source = source;
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

    public static readonly new DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(TableView), new PropertyMetadata(null, OnItemsSourceChanged));
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(TableViewColumnsColection), typeof(TableView), new PropertyMetadata(null));
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(TableView), new PropertyMetadata(40d));
    public static readonly DependencyProperty RowMaxHeightProperty = DependencyProperty.Register(nameof(RowMaxHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity));
}
