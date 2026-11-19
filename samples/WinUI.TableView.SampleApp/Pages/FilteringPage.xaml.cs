using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class FilteringPage : Page
{
    public FilteringPage()
    {
        InitializeComponent();

        tableView.CollectionView.VectorChanged += (_, _) => UpdateVisibleCount();
        tableView.Loaded += (_, _) => UpdateVisibleCount();
    }

    private void OnClearFiltersClicked(object sender, RoutedEventArgs e)
    {
        tableView.ClearAllFilters();
    }

    private void UpdateVisibleCount()
    {
        visibleCount.Text = $"{tableView.CollectionView.Count:N0} of {ExampleViewModel.ItemsList.Count:N0} rows";
    }
}
