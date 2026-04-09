using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class CustomizeSortingPage : Page
{
    public CustomizeSortingPage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items = new(ExampleViewModel.ItemsList.Take(20));
    }

    private void OnTableViewSorting(object sender, TableViewSortingEventArgs e)
    {
        if (e.Column is not null && DataContext is ExampleViewModel viewModel)
        {
            var sd = e.Column.SortDirection;
            tableView.ClearAllSorting();
            e.Column.SortDirection = GetNextSortDirection(sd);

            var header = (string)e.Column.Header;

            viewModel.Items = e.Column.SortDirection switch
            {
                SortDirection.Ascending => new(ExampleViewModel.ItemsList.OrderBy(x => GetValue(x, header)).Take(20)),
                SortDirection.Descending => new(ExampleViewModel.ItemsList.OrderByDescending(x => GetValue(x, header)).Take(20)),
                _ => new(ExampleViewModel.ItemsList.Take(20)),
            };
        }

        e.Handled = true;

        static object? GetValue(ExampleModel item, string header) => header switch
        {
            "First Name" => item.FirstName,
            "Last Name" => item.LastName,
            "Email" => item.Email,
            "Gender" => item.Gender,
            "Dob" => item.Dob,
            "Id" or _ => (object)item.Id,
        };
    }

    private SortDirection? GetNextSortDirection(SortDirection? sortDirection)
    {
        return sortDirection switch
        {
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => null,
            _ => SortDirection.Ascending,
        };
    }

    private void OnTableViewClearSorting(object sender, TableViewClearSortingEventArgs e)
    {
        if (e.Column is not null)
        {
            e.Column.SortDirection = null;
        }
        else
        {
            tableView.ClearAllSorting();
        }
    }
}