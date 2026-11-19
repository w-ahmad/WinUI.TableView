using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Showcases;

#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class Stock : ObservableObject
{
    public string Symbol { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Sector { get; set; } = string.Empty;

    public double PreviousClose { get; set; }

    public double MarketCap { get; set; }

    /// <summary>
    /// The most recent prices, oldest first; the sparkline draws them.
    /// </summary>
    public List<double> History { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Change), nameof(ChangePercent))]
    public partial double Price { get; set; }

    [ObservableProperty]
    public partial double DayLow { get; set; }

    [ObservableProperty]
    public partial double DayHigh { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    public double Change => Price - PreviousClose;

    public double ChangePercent => PreviousClose == 0 ? 0 : 100 * Change / PreviousClose;

    /// <summary>
    /// Moves the price by a random step, as a market feed would.
    /// </summary>
    public void Tick(Random random)
    {
        var price = Math.Max(1, Price * (1 + (random.NextDouble() - 0.49) * 0.012));

        History.Add(price);
        if (History.Count > StockVisuals.SparklinePoints) History.RemoveAt(0);

        DayLow = Math.Min(DayLow, price);
        DayHigh = Math.Max(DayHigh, price);
        Volume += random.Next(500, 40_000);
        Price = price;
    }
}

/// <summary>
/// Turns stock numbers into what the cell templates draw.
/// </summary>
public static class StockVisuals
{
    public const int SparklinePoints = 32;
    private const double _sparklineWidth = 96;
    private const double _sparklineHeight = 26;
    private const double _rangeTrackWidth = 72;

    private static readonly Color _up = Color.FromArgb(255, 30, 170, 90);
    private static readonly Color _down = Color.FromArgb(255, 226, 72, 72);

    public static Brush TrendBrush(double change) => Palette.Solid(change >= 0 ? _up : _down);

    public static Brush TrendTint(double change) => Palette.Tint(change >= 0 ? _up : _down, 40);

    public static string TrendGlyph(double change) => change >= 0 ? "" : "";

    public static string Signed(double value) => value.ToString("+0.00;-0.00");

    public static string SignedPercent(double value) => value.ToString("+0.00;-0.00") + "%";

    public static string StarGlyph(bool isFavorite) => isFavorite ? "" : "";

    public static Brush StarBrush(bool isFavorite) => Palette.Solid(isFavorite ? Palette.Amber : Palette.Gray);

    /// <summary>
    /// The price history as the points of a Polyline. Price is a parameter only so that an x:Bind
    /// with Mode=OneWay runs this again on every tick.
    /// </summary>
    public static PointCollection Sparkline(List<double> history, double price)
    {
        var points = new PointCollection();

        if (history.Count < 2) return points;

        var min = history.Min();
        var range = Math.Max(history.Max() - min, 0.0001);
        var step = _sparklineWidth / (SparklinePoints - 1);

        for (var i = 0; i < history.Count; i++)
        {
            points.Add(new Point(i * step, _sparklineHeight - 2 - (history[i] - min) / range * (_sparklineHeight - 4)));
        }

        return points;
    }

    /// <summary>
    /// Where the marker sits on the day-range track.
    /// </summary>
    public static Thickness RangeMarker(double low, double high, double price)
    {
        var position = high <= low ? 0.5 : Math.Clamp((price - low) / (high - low), 0, 1);

        return new Thickness(position * (_rangeTrackWidth - 8), 0, 0, 0);
    }
}

public static class StockData
{
    private static readonly (string Symbol, string Company, string Sector)[] _listings =
    [
        ("CTSO", "Contoso Ltd.", "Technology"), ("FABR", "Fabrikam Inc.", "Industrials"), ("NWND", "Northwind Traders", "Consumer"),
        ("ADVW", "Adventure Works", "Consumer"), ("TSPN", "Tailspin Toys", "Consumer"), ("WING", "Wingtip Toys", "Consumer"),
        ("LITW", "Litware Inc.", "Technology"), ("PRSW", "Proseware Inc.", "Technology"), ("WDGV", "Woodgrove Bank", "Financials"),
        ("FRTH", "Fourth Coffee", "Consumer"), ("BLUY", "Blue Yonder Airlines", "Industrials"), ("COHO", "Coho Winery", "Consumer"),
        ("LUCN", "Lucerne Publishing", "Media"), ("ALPN", "Alpine Ski House", "Consumer"), ("TREY", "Trey Research", "Healthcare"),
        ("VANA", "VanArsdel Ltd.", "Industrials"), ("WWIM", "Wide World Importers", "Industrials"), ("RELC", "Relecloud", "Technology"),
        ("HUMG", "Humongous Insurance", "Financials"), ("LAMN", "Lamna Healthcare", "Healthcare"), ("NODP", "Nod Publishers", "Media"),
        ("SRDG", "Southridge Video", "Media"), ("CNSM", "Consolidated Messenger", "Industrials"), ("BLWS", "Bellows College", "Education"),
        ("GDIN", "Graphic Design Institute", "Education"), ("MUNS", "Munson's Pickles", "Consumer"), ("FUPC", "First Up Consultants", "Financials"),
        ("MRGT", "Margie's Travel", "Consumer"), ("BFYO", "Best For You Organics", "Healthcare"), ("LDSB", "Liberty's Bakery", "Consumer"),
        ("TLWD", "Tailwind Traders", "Consumer"), ("WOOD", "Woodgrove Capital", "Financials"), ("CNTG", "Contoso Energy", "Energy"),
        ("FBKP", "Fabrikam Power", "Energy"), ("NWSO", "Northwind Solar", "Energy"), ("PRPH", "Proseware Pharma", "Healthcare")
    ];

    public static List<Stock> Create(Random random)
    {
        var stocks = new List<Stock>();

        foreach (var (symbol, company, sector) in _listings)
        {
            var previousClose = Math.Round(8 + Math.Pow(random.NextDouble(), 2) * 640, 2);
            var stock = new Stock
            {
                Symbol = symbol,
                Company = company,
                Sector = sector,
                PreviousClose = previousClose,
                MarketCap = previousClose * random.Next(40, 9_000) * 1_000_000d,
                Price = previousClose,
                DayLow = previousClose,
                DayHigh = previousClose,
                Volume = random.Next(200_000, 48_000_000),
                IsFavorite = random.NextDouble() < 0.2
            };

            for (var i = 0; i < StockVisuals.SparklinePoints; i++)
            {
                stock.Tick(random);
            }

            stocks.Add(stock);
        }

        return stocks;
    }
}
