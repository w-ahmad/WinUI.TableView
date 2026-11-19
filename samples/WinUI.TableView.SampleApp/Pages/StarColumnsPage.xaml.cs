using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class StarColumnsPage : Page
{
    public StarColumnsPage()
    {
        InitializeComponent();

        selectionMode.ItemsSource = Enum.GetNames<ListViewSelectionMode>();
    }

    private void OnSelectionModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Enum.TryParse<ListViewSelectionMode>(selectionMode.SelectedItem as string, out var mode))
        {
            tableView.SelectionMode = mode;
        }
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items = [.. ExampleViewModel.ItemsList.Take(20)];
    }
}
