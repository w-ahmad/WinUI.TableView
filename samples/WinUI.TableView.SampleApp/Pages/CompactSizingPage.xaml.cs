using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class CompactSizingPage : Page
{
    public CompactSizingPage()
    {
        InitializeComponent();
    }

    private void OnCompactSizingToggled(object sender, RoutedEventArgs e)
    {
        if (compactSizing.IsOn)
        {
            tableView.RowMinHeight = 32;
            tableView.HeaderRowMinHeight = 28;
        }
        else
        {
            tableView.ClearValue(TableView.RowMinHeightProperty);
            tableView.ClearValue(TableView.HeaderRowMinHeightProperty);
        }
    }

    private void OnFontFamilyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (fontFamily.SelectedItem is string name)
        {
            tableView.FontFamily = new FontFamily(name);
        }
    }

    private void OnResetClicked(object sender, RoutedEventArgs e)
    {
        compactSizing.IsOn = false;
        fontFamily.SelectedIndex = 0;
        tableView.ClearValue(Control.FontSizeProperty);
    }
}
