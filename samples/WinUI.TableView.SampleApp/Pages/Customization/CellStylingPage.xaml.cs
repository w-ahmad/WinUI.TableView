using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class CellStylingPage : Page
{
    public CellStylingPage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ExampleViewModel viewModel)
        {
            viewModel.Items = [.. ExampleViewModel.ItemsList.Take(100)];
        }
    }
}
