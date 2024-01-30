using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

namespace WinUI3.TableView;
public class TableView : ListView
{
    public TableView()
    {
        DefaultStyleKey = typeof(TableView);
        Columns = new();
        Columns.CollectionChanged += OnColumnsCollectionChanged;
        base.ItemsSource = CollectionView;
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var templateBuilder = new StringBuilder();
        templateBuilder.AppendLine("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
        templateBuilder.AppendLine("xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        templateBuilder.AppendLine("xmlns:local=\"using:WinUI3.TableView\"");
        templateBuilder.AppendLine("xmlns:ui=\"using:CommunityToolkit.WinUI\">");
        templateBuilder.AppendLine("<local:TableViewRowPresenter x:Name=\"stackPanel\"");
        templateBuilder.AppendLine("Orientation=\"Horizontal\"");
        templateBuilder.AppendLine("ui:FrameworkElementExtensions.AncestorType=\"local:TableView\"");
        templateBuilder.AppendLine("Tag=\"{Binding (ui:FrameworkElementExtensions.Ancestor).Columns, RelativeSource={RelativeSource Self}}\">");

        for (int i = 0; i < Columns.Count; i++)
        {
            templateBuilder.AppendLine($"<local:TableViewCell Column=\"{{Binding Tag[{i}], ElementName=stackPanel}}\" />");
        }

        templateBuilder.AppendLine("</local:TableViewRowPresenter>");
        templateBuilder.AppendLine("</DataTemplate>");

        ItemTemplate = (DataTemplate)XamlReader.Load(templateBuilder.ToString());
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
        var row = listViewItem?.FindDescendant<TableViewRowPresenter>();
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
        var row = listViewItem?.FindDescendant<TableViewRowPresenter>();
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
