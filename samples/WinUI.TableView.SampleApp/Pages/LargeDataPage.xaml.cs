using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class LargeDataPage : Page
{
    public LargeDataPage()
    {
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (TransactionsViewModel.TransacationsList?.Count > 0) return;

        App.Current.MainPage.SetLoading(true);
        await TransactionsViewModel.InitializeItemsAsync();
        App.Current.MainPage.SetLoading(false);
    }

    private void OnSetItemsSourceCliced(object sender, RoutedEventArgs e)
    {
        if (DataContext is TransactionsViewModel viewModel)
        {
            viewModel.TransacationsData = [.. TransactionsViewModel.TransacationsList];
            ((Button)sender).IsEnabled = false;
        }
    }
}
