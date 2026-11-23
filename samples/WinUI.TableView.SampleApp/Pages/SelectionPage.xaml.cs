using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class SelectionPage : Page
{
    private int _selectedCells;

    public SelectionPage()
    {
        InitializeComponent();

        selectionModes.ItemsSource = Enum.GetNames<ListViewSelectionMode>();
    }

    private void OnSelectionModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Enum.TryParse<ListViewSelectionMode>(selectionModes.SelectedItem as string, out var selectionMode))
        {
            tableView.SelectionMode = selectionMode;
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        selectedRows.Text = tableView.SelectedItems.Count.ToString("N0");
        selectedItem.Text = tableView.SelectedItem is ExampleModel item ? $"#{item.Id} {item.FullName}" : "none";
    }

    private void OnCellSelectionChanged(object? sender, TableViewCellSelectionChangedEventArgs e)
    {
        _selectedCells += e.AddedCells.Count - e.RemovedCells.Count;
        selectedCells.Text = Math.Max(0, _selectedCells).ToString("N0");
    }

    private void OnSelectAllClicked(object sender, RoutedEventArgs e)
    {
        tableView.SelectAll();
    }

    private void OnClearSelectionClicked(object sender, RoutedEventArgs e)
    {
        tableView.DeselectAll();
    }
}
