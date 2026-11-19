using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ReorderColumnsPage : Page
{
    private int _reorderingFromIndex;

    public ReorderColumnsPage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items = [.. ExampleViewModel.ItemsList.Take(20)];
    }

    private void OnColumnReordering(object sender, TableViewColumnReorderingEventArgs e)
    {
        _reorderingFromIndex = tableView.Columns.VisibleColumns.IndexOf(e.Column);
    }

    private void OnColumnReordered(object sender, TableViewColumnReorderedEventArgs e)
    {
        lastReorder.Text = $"'{e.Column.Header}' moved from {_reorderingFromIndex} to {e.Index}.";
    }
}
