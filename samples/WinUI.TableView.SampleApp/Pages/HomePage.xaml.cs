using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI.TableView.SampleApp.Controls;
using WinUI.TableView.SampleApp.Showcases;

namespace WinUI.TableView.SampleApp.Pages;

public sealed class HomeHighlight(string glyph, string title, string text)
{
    public string Glyph { get; } = glyph;

    public string Title { get; } = title;

    public string Text { get; } = text;
}

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();

        foreach (var group in SampleCatalog.Groups)
        {
            AddGroup(group);
        }
    }

    public List<Deal> PreviewDeals { get; } = [.. DealData.Deals.Take(6)];

    public List<HomeHighlight> Highlights { get; } =
    [
        new("", "Built for scale", "A million rows, virtualized in both directions."),
        new("", "Excel-like tools", "Sort, filter, group, copy, paste and export."),
        new("", "Yours to style", "Styles, templates and theme resources for every part."),
        new("", "Runs everywhere", "Windows, WebAssembly, macOS, Linux, iOS and Android.")
    ];

    private void AddGroup(SampleGroup group)
    {
        var header = new Grid { Margin = new Thickness(0, 20, 0, 0), ColumnSpacing = 12 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titles = new StackPanel { Spacing = 2 };
        titles.Children.Add(new TextBlock
        {
            Text = group.Title,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"]
        });
        titles.Children.Add(new TextBlock
        {
            Text = group.Description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        });
        header.Children.Add(titles);

        var seeAll = new HyperlinkButton { Content = "See all", VerticalAlignment = VerticalAlignment.Bottom };
        seeAll.Click += (_, _) => App.Current.MainWindow.NavigateTo(group);
        Grid.SetColumn(seeAll, 1);
        header.Children.Add(seeAll);

        content.Children.Add(header);
        content.Children.Add(new SampleTiles
        {
            IsFeatured = group == SampleCatalog.Showcases,
            Samples = group.Samples
        });
    }

    private void OnExploreShowcasesClicked(object sender, RoutedEventArgs e)
    {
        App.Current.MainWindow.NavigateTo(SampleCatalog.Showcases);
    }

    private void OnStartWithBasicsClicked(object sender, RoutedEventArgs e)
    {
        App.Current.MainWindow.NavigateTo(SampleCatalog.Fundamentals.Samples[0]);
    }
}
