using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Controls;

/// <summary>
/// A responsive grid of sample cards; clicking one opens its page.
/// </summary>
public sealed partial class SampleTiles : UserControl
{
    private static readonly (Color Start, Color End)[] _banners =
    [
        (Color.FromArgb(255, 0, 103, 192), Color.FromArgb(255, 79, 107, 237)),
        (Color.FromArgb(255, 14, 122, 113), Color.FromArgb(255, 16, 137, 62)),
        (Color.FromArgb(255, 135, 80, 190), Color.FromArgb(255, 194, 57, 179)),
        (Color.FromArgb(255, 202, 80, 16), Color.FromArgb(255, 218, 139, 0)),
    ];

    public SampleTiles()
    {
        InitializeComponent();
    }

    public List<SampleInfo>? Samples
    {
        get => repeater.ItemsSource as List<SampleInfo>;
        set => repeater.ItemsSource = value;
    }

    public bool IsFeatured
    {
        get;
        set
        {
            field = value;
            repeater.ItemTemplate = (DataTemplate)Resources[value ? "FeaturedTileTemplate" : "TileTemplate"];
            repeater.Layout = (VirtualizingLayout)Resources[value ? "FeaturedTileLayout" : "TileLayout"];
        }
    }

    public static Brush Banner(string title)
    {
        var index = Math.Max(0, SampleCatalog.Showcases.Samples.Select(sample => sample.Title).ToList().IndexOf(title));
        var (start, end) = _banners[index % _banners.Length];

        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop { Color = start, Offset = 0 },
                new GradientStop { Color = end, Offset = 1 }
            }
        };
    }

    private void OnTileClicked(object sender, RoutedEventArgs e)
    {
        App.Current.MainWindow.NavigateTo((sender as FrameworkElement)?.Tag);
    }
}
