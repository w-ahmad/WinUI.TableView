using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace WinUI.TableView.SampleApp.Pages;

/// <summary>
/// Lists the samples of one group of the catalog.
/// </summary>
public sealed partial class SectionPage : Page
{
    public SectionPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if ((e.Parameter as NavigationViewItem)?.Tag is SampleGroup group)
        {
            icon.Glyph = group.Glyph;
            title.Text = group.Title;
            description.Text = group.Description;
            tiles.IsFeatured = group == SampleCatalog.Showcases;
            tiles.Samples = group.Samples;
        }
    }
}
