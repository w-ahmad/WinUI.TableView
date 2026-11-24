using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class PaginationPage : Page
{
    public PaginationPage()
    {
        InitializeComponent();
    }

    private int _pageCount;
    private int _currentPage;

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        UpdatePages();
    }

    private void OnTableViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePages();
    }

    private void UpdatePages()
    {
        if (DataContext is not ExampleViewModel) return;

        var rowHeight = tableView.RowHeight is not double.NaN ? tableView.RowHeight : tableView.RowMinHeight;
        var headerHeight = tableView.HeaderRowHeight is not double.NaN ? tableView.HeaderRowHeight : tableView.HeaderRowMinHeight;
        var availableHeight = tableView.ActualHeight - headerHeight;
        var pageSize = Math.Max(1, (int)Math.Floor(availableHeight / (rowHeight + tableView.HorizontalGridLinesStrokeThickness)));

        if (pageSize == PageSize && _pageCount > 0) return;

        // Stay on the page that holds the first row shown.
        var firstRow = _currentPage * PageSize;

        PageSize = pageSize;
        _pageCount = Math.Max(1, (int)Math.Ceiling(ExampleViewModel.ItemsList.Count / (double)PageSize));
        ShowPage(firstRow / PageSize);
    }

    private void ShowPage(int page)
    {
        if (DataContext is not ExampleViewModel viewModel || _pageCount == 0) return;

        _currentPage = Math.Clamp(page, 0, _pageCount - 1);

        var total = ExampleViewModel.ItemsList.Count;
        var firstRow = _currentPage * PageSize;
        viewModel.Items = new(ExampleViewModel.ItemsList.Skip(firstRow).Take(PageSize));

        pageNumber.Maximum = _pageCount;
        pageNumber.Value = _currentPage + 1;
        pageCountText.Text = $"of {_pageCount}";
        rowsText.Text = $"Rows {Math.Min(firstRow + 1, total)} to {Math.Min(firstRow + PageSize, total)} of {total}";

        firstPage.IsEnabled = previousPage.IsEnabled = _currentPage > 0;
        nextPage.IsEnabled = lastPage.IsEnabled = _currentPage < _pageCount - 1;
    }

    private void OnFirstPageClicked(object sender, RoutedEventArgs e) => ShowPage(0);

    private void OnPreviousPageClicked(object sender, RoutedEventArgs e) => ShowPage(_currentPage - 1);

    private void OnNextPageClicked(object sender, RoutedEventArgs e) => ShowPage(_currentPage + 1);

    private void OnLastPageClicked(object sender, RoutedEventArgs e) => ShowPage(_pageCount - 1);

    private void OnPageNumberChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        // An emptied box goes back to the page shown.
        ShowPage(double.IsNaN(args.NewValue) ? _currentPage : (int)args.NewValue - 1);
    }

    public int PageSize { get; set; }
}
