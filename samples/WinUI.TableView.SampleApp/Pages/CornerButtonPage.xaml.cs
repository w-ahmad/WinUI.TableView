using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class CornerButtonPage : Page
{
    public CornerButtonPage()
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
