using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ColumnTypesPage : Page
{
    public ColumnTypesPage()
    {
        InitializeComponent();
    }

    public List<string?> Genders { get; } = [.. ExampleViewModel.ItemsList.Select(item => item.Gender).Distinct().Order()];

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ExampleViewModel viewModel)
        {
            viewModel.Items = [.. ExampleViewModel.ItemsList.Take(100)];
        }
    }
}
