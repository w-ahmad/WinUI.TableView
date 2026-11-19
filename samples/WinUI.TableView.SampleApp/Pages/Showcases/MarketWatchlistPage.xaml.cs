using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI.TableView.SampleApp.Showcases;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class MarketWatchlistPage : Page
{
    private readonly Random _random = new(7);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(350) };

    public MarketWatchlistPage()
    {
        Stocks = StockData.Create(_random);
        Indices =
        [
            CreateIndex("Contoso 500", 5_482.31),
            CreateIndex("Fabrikam Tech 100", 19_204.77),
            CreateIndex("Northwind Small Cap", 2_196.08),
            CreateIndex("Woodgrove Financials", 718.42)
        ];

        InitializeComponent();

        tableView.FilterDescriptions.Add(new FilterDescription(nameof(Stock.IsFavorite),
            item => favoritesOnly.IsChecked is not true || item is Stock { IsFavorite: true }));

        _timer.Tick += OnTick;
        _timer.Start();
        UpdateStatus();
    }

    public List<Stock> Stocks { get; }

    public List<Stock> Indices { get; }

    // A change of 1.5% or more tints the Change cell; the cell re-checks these when the price moves.
    public Predicate<TableViewConditionalCellStyleContext> IsSurging =>
        static context => context.DataItem is Stock { ChangePercent: >= 1.5 };

    public Predicate<TableViewConditionalCellStyleContext> IsPlunging =>
        static context => context.DataItem is Stock { ChangePercent: <= -1.5 };

    private Stock CreateIndex(string name, double value)
    {
        var index = new Stock { Company = name, PreviousClose = value, Price = value, DayLow = value, DayHigh = value };

        for (var i = 0; i < StockVisuals.SparklinePoints; i++)
        {
            index.Tick(_random);
        }

        return index;
    }

    private void OnTick(object? sender, object e)
    {
        // A third of the list moves on every tick, as a real feed would deliver it.
        foreach (var stock in Stocks.Where(_ => _random.Next(3) == 0))
        {
            stock.Tick(_random);
        }

        foreach (var index in Indices)
        {
            index.Tick(_random);
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var gainers = Stocks.Count(stock => stock.Change >= 0);

        status.Text = $"{gainers} up · {Stocks.Count - gainers} down · {DateTime.Now:T}";
    }

    private void OnLiveToggled(object sender, RoutedEventArgs e)
    {
        if (live.IsOn) _timer.Start(); else _timer.Stop();

        liveDot.Opacity = live.IsOn ? 1 : 0.3;
    }

    private void OnFavoriteClicked(object sender, RoutedEventArgs e)
    {
        if (favoritesOnly.IsChecked is true)
        {
            tableView.RefreshFilter();
        }
    }

    private void OnFavoritesOnlyClicked(object sender, RoutedEventArgs e)
    {
        tableView.RefreshFilter();
    }

    private void OnGroupBySectorClicked(object sender, RoutedEventArgs e)
    {
        tableView.UngroupAll();

        if (groupBySector.IsChecked is true)
        {
            tableView.GroupDescriptions.Add(new GroupDescription(nameof(Stock.Sector), valueDelegate: item => (item as Stock)?.Sector));
        }
    }

    private void OnSortByMoversClicked(object sender, RoutedEventArgs e)
    {
        tableView.ClearAllSorting();
        tableView.SortDescriptions.Add(new SortDescription(nameof(Stock.ChangePercent), SortDirection.Descending,
            valueDelegate: item => (item as Stock)?.ChangePercent));
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
    }
}
