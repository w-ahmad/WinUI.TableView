using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Showcases;

public enum DealStage
{
    Prospecting,
    Qualified,
    Proposal,
    Negotiation,
    ClosedWon,
    ClosedLost
}

#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class Deal : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Account { get; set; } = string.Empty;

    public int AccountSeed { get; set; }

    public string Owner { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string NextStep { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateOnly CloseDate { get; set; }

    [ObservableProperty]
    public partial DealStage Stage { get; set; }

    [ObservableProperty]
    public partial int Probability { get; set; }

    public bool IsOpen => Stage is not (DealStage.ClosedWon or DealStage.ClosedLost);
}

/// <summary>
/// What a stage looks like. Templates bind to these with x:Bind, e.g.
/// Background="{x:Bind data:DealVisuals.Tint(Stage), Mode=OneWay}".
/// </summary>
public static class DealVisuals
{
    public static Color Color(DealStage stage) => stage switch
    {
        DealStage.Prospecting => Palette.Gray,
        DealStage.Qualified => Palette.Blue,
        DealStage.Proposal => Palette.Purple,
        DealStage.Negotiation => Palette.Amber,
        DealStage.ClosedWon => Palette.Green,
        _ => Palette.Red
    };

    public static string Label(DealStage stage) => stage switch
    {
        DealStage.ClosedWon => "Closed won",
        DealStage.ClosedLost => "Closed lost",
        _ => stage.ToString()
    };

    public static Brush Accent(DealStage stage) => Palette.Solid(Color(stage));

    public static Brush Tint(DealStage stage) => Palette.Tint(Color(stage));

    public static Visibility OverdueVisibility(DealStage stage, DateOnly closeDate)
    {
        var isOverdue = stage is not (DealStage.ClosedWon or DealStage.ClosedLost) && closeDate < DealData.Today;

        return isOverdue ? Visibility.Visible : Visibility.Collapsed;
    }

    // Group headers hand the key over as an object: a stage when grouped by stage, text otherwise.
    public static string GroupLabel(object? key) => key is DealStage stage ? Label(stage) : $"{key}";

    public static Brush GroupAccent(object? key) => key is DealStage stage ? Accent(stage) : Palette.Avatar($"{key}".Length);
}

public static class DealData
{
    private static readonly string[] _names =
    [
        "Premier support renewal", "Onsite engineer, 12 months", "Landing zone build-out", "Cloud migration, phase {0}",
        "Seat expansion ({1} seats)", "Rugged tablet rollout ({1} units)", "Customer 360 dashboard", "Zero-trust rollout",
        "Kubernetes platform", "Data center exit", "Data warehouse modernization", "Analytics workspace",
        "Identity consolidation", "ERP integration", "Contact center upgrade", "Device refresh ({1} units)",
        "Security assessment", "Training bundle", "Disaster recovery plan", "Edge network upgrade"
    ];

    private static readonly string[] _accounts =
    [
        "Wingtip Toys", "Fourth Coffee", "Lamna Healthcare", "Litware", "Proseware", "First Up Consultants",
        "Munson's Pickles", "Coho Winery", "Southridge Video", "Blue Yonder Airlines", "Tailspin Toys", "Contoso",
        "Fabrikam", "Northwind Traders", "Adventure Works", "Woodgrove Bank", "Alpine Ski House", "Trey Research",
        "VanArsdel", "Wide World Importers", "Relecloud", "Humongous Insurance", "Nod Publishers", "Bellows College"
    ];

    private static readonly string[] _owners =
    [
        "Tom Gallagher", "Emma Walsh", "Diego Morales", "Aisha Khan", "Ravi Iyer", "Priya Raman",
        "Hannah Schultz", "Kwame Mensah", "Sofia Rossi", "Jun Bae", "Kai Müller", "Elena Petrova"
    ];

    private static readonly string[] _nextSteps =
    [
        "Provision licenses", "Send revised quote", "Schedule technical deep-dive", "Legal review of MSA",
        "Confirm budget holder", "Book executive briefing", "Share reference customer", "Run proof of concept",
        "Collect security questionnaire", "Finalize statement of work"
    ];

    public static readonly string[] Regions =
        ["Northern California", "Southern California", "Pacific Northwest", "Southwest", "Mountain", "Texas"];

    public static readonly string[] Categories = ["Support", "Cloud", "Licensing", "Services", "Hardware"];

    public static readonly DateOnly Today = new(2026, 9, 30);

    private static List<Deal>? _deals;

    public static IReadOnlyList<Deal> Deals => _deals ??= Create(5_000);

    public static IReadOnlyList<string> Owners => _owners;

    private static List<Deal> Create(int count)
    {
        var random = new Random(24018);
        var deals = new List<Deal>(count);

        for (var i = 0; i < count; i++)
        {
            var account = random.Next(_accounts.Length);
            var stage = random.NextDouble() switch
            {
                < 0.22 => DealStage.Prospecting,
                < 0.40 => DealStage.Qualified,
                < 0.56 => DealStage.Proposal,
                < 0.69 => DealStage.Negotiation,
                < 0.89 => DealStage.ClosedWon,
                _ => DealStage.ClosedLost
            };
            var probability = stage switch
            {
                DealStage.Prospecting => 5 * random.Next(2, 5),
                DealStage.Qualified => 5 * random.Next(5, 9),
                DealStage.Proposal => 5 * random.Next(9, 13),
                DealStage.Negotiation => 5 * random.Next(13, 18),
                DealStage.ClosedWon => 100,
                _ => 0
            };
            var isClosed = stage is DealStage.ClosedWon or DealStage.ClosedLost;
            var amount = Math.Round((decimal)Math.Pow(random.NextDouble(), 2.2) * 320_000m / 250m) * 250m + 6_500m;

            deals.Add(new Deal
            {
                Id = $"D-{24018 + i}",
                Name = string.Format(_names[random.Next(_names.Length)], random.Next(1, 4), 10 * random.Next(12, 96)),
                Account = _accounts[account],
                AccountSeed = account,
                Owner = _owners[random.Next(_owners.Length)],
                Region = Regions[random.Next(Regions.Length)],
                Category = Categories[random.Next(Categories.Length)],
                NextStep = _nextSteps[random.Next(_nextSteps.Length)],
                Amount = amount,
                Stage = stage,
                Probability = probability,
                CloseDate = Today.AddDays(isClosed ? -random.Next(1, 270) : random.Next(-12, 240))
            });
        }

        return deals;
    }
}
