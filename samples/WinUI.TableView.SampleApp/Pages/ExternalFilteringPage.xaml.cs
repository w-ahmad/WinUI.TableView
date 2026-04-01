using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ExternalFilteringPage : Page
{
    private CancellationTokenSource? _token;

    public ExternalFilteringPage()
    {
        InitializeComponent();

        tableView.FilterDescriptions.Add(new FilterDescription(string.Empty, Filter));
    }

    private bool Filter(object? item)
    {
        if (string.IsNullOrWhiteSpace(filterText.Text)) return true;
        if (item is null) return false;

        var model = (ExampleModel)item;

        return model.FirstName?.Contains(filterText.Text, StringComparison.OrdinalIgnoreCase) is true ||
               model.LastName?.Contains(filterText.Text, StringComparison.OrdinalIgnoreCase) is true ||
               model.Email?.Contains(filterText.Text, StringComparison.OrdinalIgnoreCase) is true ||
               model.Gender?.Contains(filterText.Text, StringComparison.OrdinalIgnoreCase) is true ||
               model.Department?.Contains(filterText.Text, StringComparison.OrdinalIgnoreCase) is true ||
               model.Designation?.Contains(filterText.Text, StringComparison.OrdinalIgnoreCase) is true ||
               model.Address?.Contains(filterText.Text, StringComparison.OrdinalIgnoreCase) is true;
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_token is not null)
        {
            _token.Cancel();
        }

        _token = new CancellationTokenSource();
        await RefreshFilter(_token.Token);
    }

    private async Task RefreshFilter(CancellationToken token)
    {
        try
        {
            await Task.Delay(200, token);
        }
        catch
        {
            return;
        }

        _token = null;
        tableView.RefreshFilter();
    }
}
