using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class IncrementalLoadingPage : Page
{
    private readonly IncrementalLoadingSource _source = [];

    public IncrementalLoadingPage()
    {
        InitializeComponent();

        _source.LoadingStateChanged += OnLoadingStateChanged;
        tableView.ItemsSource = _source;
    }

    private void OnLoadingStateChanged(object? sender, bool isLoading)
    {
        loadingBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        loadedCountRun.Text = _source.Count.ToString();
        hasMoreItemsRun.Text = _source.HasMoreItems.ToString();
    }
}
