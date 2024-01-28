using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections;
using System.Threading.Tasks;

namespace WinUI3.TableView;

[StyleTypedProperty(Property = nameof(HeaderRowStyle), StyleTargetType = typeof(TableViewHeaderRow))]
[StyleTypedProperty(Property = nameof(RowStyle), StyleTargetType = typeof(TableViewRow))]
public class TableView : ListView
{
    public TableView()
    {
        DefaultStyleKey = typeof(TableView);
        Columns = [];
        base.ItemsSource = CollectionView;
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

    public TableViewColumnsColection Columns
    {
        get => (TableViewColumnsColection)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public Style HeaderRowStyle
    {
        get => (Style)GetValue(HeaderRowStyleProperty);
        set => SetValue(HeaderRowStyleProperty, value);
    }

    public Style RowStyle
    {
        get => (Style)GetValue(RowStyleProperty);
        set => SetValue(RowStyleProperty, value);
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
    public static readonly DependencyProperty RowStyleProperty = DependencyProperty.Register(nameof(RowStyle), typeof(Style), typeof(TableView), new PropertyMetadata(default));
    public static readonly DependencyProperty HeaderRowStyleProperty = DependencyProperty.Register(nameof(HeaderRowStyle), typeof(Style), typeof(TableView), new PropertyMetadata(default));
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(nameof(RowHeight), typeof(double), typeof(TableView), new PropertyMetadata(40d));
    public static readonly DependencyProperty RowMaxHeightProperty = DependencyProperty.Register(nameof(RowMaxHeight), typeof(double), typeof(TableView), new PropertyMetadata(double.PositiveInfinity));
}
