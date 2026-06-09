using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ColumnSizingPage : Page
{
    public ColumnSizingPage()
    {
        InitializeComponent();

    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items = new(ExampleViewModel.ItemsList.Take(20));
    }
}