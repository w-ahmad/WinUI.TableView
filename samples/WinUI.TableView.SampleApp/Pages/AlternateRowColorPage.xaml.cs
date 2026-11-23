using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class AlternateRowColorPage : Page
{
    public AlternateRowColorPage()
    {
        InitializeComponent();
    }

    private void OnPresetClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var colors = ((string)((Button)sender).Tag).Split('|');

        tableView.AlternateRowBackground = ToBrush(colors[0]);
        tableView.AlternateRowForeground = colors.Length > 1 ? ToBrush(colors[1]) : null!;
    }

    private static SolidColorBrush ToBrush(string hex)
    {
        var argb = Convert.ToUInt32(hex.TrimStart('#'), 16);

        return new SolidColorBrush(Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
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
