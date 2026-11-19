using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ClipboardActionsPage : Page
{
    public ClipboardActionsPage()
    {
        InitializeComponent();
    }

    private void OnCopyToClipboard(object? sender, TableViewCopyToClipboardEventArgs e)
    {
        copiedText.Text = tableView.GetSelectedContent(e.IncludeHeaders);
    }

    private void OnReadSelectionClicked(object sender, RoutedEventArgs e)
    {
        copiedText.Text = tableView.GetSelectedContent(includeHeaders.IsChecked is true);
    }
}
