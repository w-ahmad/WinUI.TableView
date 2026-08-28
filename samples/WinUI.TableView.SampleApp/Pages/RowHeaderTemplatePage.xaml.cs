using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class RowHeaderTemplatePage : Page
{
    public RowHeaderTemplatePage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items = [.. ExampleViewModel.ItemsList.Take(40)];
    }

    private void OnRowHeaderContentChanged(object sender, SelectionChangedEventArgs e)
    {
        tableView.RowHeaderTemplate = rowHeaderContent.SelectedIndex switch
        {
            1 => (DataTemplate)Resources["RowNumberHeaderTemplate"],
            2 => (DataTemplate)Resources["AvatarHeaderTemplate"],
            _ => null
        };
    }
}
