using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class PaginationPage : Page
{
    public PaginationPage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        UpdatePages();
    }

    private void OnTableViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePages();
    }

    private void UpdatePages()
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        var rowHeight = tableView.RowHeight is not double.NaN ? tableView.RowHeight : tableView.RowMinHeight;
        PageSize = (int)Math.Floor(rowHeight - 32 / rowHeight);

        pageList.ItemsSource = Enumerable.Range(1, (int)Math.Ceiling(ExampleViewModel.ItemsList.Count / (double)PageSize));
        pageList.SelectedItem = 1;

        viewModel.Items = new(ExampleViewModel.ItemsList.Take(PageSize));
    }

    private void OnPageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ExampleViewModel viewModel) return;
        var currentPage = (pageList.SelectedItem as int?) ?? 1;

        viewModel.Items = new(ExampleViewModel.ItemsList.Skip((currentPage - 1) * PageSize).Take(PageSize));
    }

    public int PageSize { get; set; } = 10;
}
