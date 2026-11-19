using Microsoft.UI.Xaml.Controls;
using WinUI.TableView.SampleApp.Showcases;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class SalesPipelinePage : Page
{
    private const string AllRegions = "All regions";
    private string _searchText = string.Empty;
    private string _region = AllRegions;

    public SalesPipelinePage()
    {
        InitializeComponent();

        // One predicate serves the search box and the region picker; RefreshFilter() re-runs it.
        tableView.FilterDescriptions.Add(new FilterDescription(null, IsMatch));
        UpdateSummary();
    }

    public IReadOnlyList<Deal> Deals { get; } = DealData.Deals;

    public List<string> RegionOptions { get; } = [AllRegions, .. DealData.Regions];

    private bool IsMatch(object? item)
    {
        return item is Deal deal
            && (_region == AllRegions || deal.Region == _region)
            && (_searchText.Length == 0
                || deal.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || deal.Account.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || deal.Owner.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || deal.Id.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _searchText = sender.Text.Trim();
        tableView.RefreshFilter();
        UpdateCount();
    }

    private void OnRegionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tableView is null) return;

        _region = regionFilter.SelectedItem as string ?? AllRegions;
        tableView.RefreshFilter();
        UpdateSummary();
    }

    private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tableView is null) return;

        tableView.UngroupAll();

        // A value delegate keeps grouping free of reflection, so it also works when trimmed.
        GroupDescription? description = groupBy.SelectedIndex switch
        {
            1 => new(nameof(Deal.Stage), valueDelegate: item => (item as Deal)?.Stage),
            2 => new(nameof(Deal.Region), valueDelegate: item => (item as Deal)?.Region),
            3 => new(nameof(Deal.Owner), valueDelegate: item => (item as Deal)?.Owner),
            _ => null
        };

        if (description is not null)
        {
            tableView.GroupDescriptions.Add(description);
        }
    }

    private void OnDensityChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (tableView is not null && sender.SelectedItem?.Tag is string height)
        {
            tableView.RowHeight = double.Parse(height);
        }
    }

    private void UpdateSummary()
    {
        var deals = Deals.Where(deal => _region == AllRegions || deal.Region == _region).ToList();
        var open = deals.Where(deal => deal.IsOpen).OrderBy(deal => deal.Amount).ToList();
        var won = deals.Where(deal => deal.Stage is DealStage.ClosedWon).ToList();
        var lost = deals.Count(deal => deal.Stage is DealStage.ClosedLost);
        var quarterStart = new DateOnly(DealData.Today.Year, (DealData.Today.Month - 1) / 3 * 3 + 1, 1);
        var wonThisQuarter = won.Where(deal => deal.CloseDate >= quarterStart).ToList();
        var winRate = won.Count + lost == 0 ? 0 : 100 * won.Count / (won.Count + lost);

        openPipelineValue.Text = Format.Compact(open.Sum(deal => deal.Amount));
        openPipelineCaption.Text = $"{open.Count:N0} open deals · {Format.Compact(open.Sum(deal => deal.Amount * deal.Probability / 100))} weighted";
        wonValue.Text = Format.Compact(wonThisQuarter.Sum(deal => deal.Amount));
        wonCaption.Text = $"{wonThisQuarter.Count:N0} deals closed since {Format.Date(quarterStart)}";
        averageValue.Text = Format.Currency(open.Count == 0 ? 0 : open.Average(deal => deal.Amount));
        averageCaption.Text = $"Median {Format.Currency(open.Count == 0 ? 0 : open[open.Count / 2].Amount)} · open deals";
        winRateValue.Text = Format.Percent(winRate);
        winRateBar.Value = winRate;
        winRateCaption.Text = $"{won.Count:N0} won · {lost:N0} lost";

        UpdateCount();
    }

    private void UpdateCount()
    {
        dealCount.Text = $"{Deals.Count(IsMatch):N0} deals";
    }
}
