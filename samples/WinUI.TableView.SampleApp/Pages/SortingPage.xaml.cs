using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class SortingPage : Page
{
    public SortingPage()
    {
        InitializeComponent();
    }

    private void OnSortByDepartmentClicked(object sender, RoutedEventArgs e)
    {
        Sort(new SortDescription(nameof(ExampleModel.Department)),
             new SortDescription(nameof(ExampleModel.LastName), SortDirection.Descending));
    }

    private void OnSortByAgeClicked(object sender, RoutedEventArgs e)
    {
        Sort(new SortDescription(nameof(ExampleModel.Dob), SortDirection.Descending));
    }

    private void OnClearSortingClicked(object sender, RoutedEventArgs e)
    {
        Sort();
    }

    private void Sort(params SortDescription[] descriptions)
    {
        tableView.ClearAllSorting();

        foreach (var description in descriptions)
        {
            tableView.SortDescriptions.Add(description);
        }

        sortState.Text = descriptions.Length == 0
            ? "Not sorted in code."
            : "Sorted by " + string.Join(", then ", descriptions.Select(description => $"{description.PropertyName} ({description.Direction})"));
    }
}
