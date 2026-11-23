using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class LargeDataPage : Page
{
    public LargeDataPage()
    {
        InitializeComponent();

        itemsInView.Text = $"{tableView.CollectionView.Count}";
        tableView.CollectionView.VectorChanged += OnCollectionViewChanged;
    }

    private TransactionsViewModel? ViewModel => DataContext as TransactionsViewModel;

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (TransactionsViewModel.TransacationsList?.Count > 0) return;

        App.Current.MainWindow.SetLoading(true);
        await TransactionsViewModel.InitializeItemsAsync();
        App.Current.MainWindow.SetLoading(false);
    }

    private void OnSetItemsSourceCliced(object sender, RoutedEventArgs e)
    {
        if (DataContext is TransactionsViewModel viewModel)
        {
            viewModel.TransacationsData = TransactionsViewModel.TransacationsList;
            ((Button)sender).IsEnabled = false;
        }
    }

    private void OnCollectionViewChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        itemsInView.Text = $"{sender.Count}";
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TransactionsViewModel viewModel)
        {
            viewModel.TransacationsData = null;
        }

        TransactionsViewModel.ReleaseItems();
    }
}
