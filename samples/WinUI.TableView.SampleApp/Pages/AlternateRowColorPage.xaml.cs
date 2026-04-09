using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class AlternateRowColorPage : Page
{
    public AlternateRowColorPage()
    {
        InitializeComponent();
    }

    private void ResetBackground(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        tableView.AlternateRowBackground = null!;
    }

    private void ResetForeground(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        tableView.AlternateRowForeground = null!;
    }
}
