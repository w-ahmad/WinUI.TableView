using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class CornerButtonPage : Page
{
    public CornerButtonPage()
    {
        InitializeComponent();

        selectionModes.ItemsSource = Enum.GetNames<ListViewSelectionMode>();
    }
}
