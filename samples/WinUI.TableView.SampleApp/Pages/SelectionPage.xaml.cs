using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class SelectionPage : Page
{
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
}
