using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class SelectionPage : Page
{
    public SelectionPage()
    {
        InitializeComponent();

        selectionModes.ItemsSource = Enum.GetNames<ListViewSelectionMode>();
    }
}
