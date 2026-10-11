using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewHeaderRowTests
{
    [UITestMethod]
    public async Task HeaderClip_StaysAValidRect_WhenOffsetExceedsTheHeadersPanelExtent()
    {
        // Frozen columns wider than the viewport let HorizontalOffset run past the scrollable
        // headers panel's own extent, which is what drives the clip width below zero.
        var tableView = await CreateTableViewAsync(frozenColumnCount: 2);
        try
        {
            var panel = GetScrollableHeadersPanel(tableView);

            tableView.SetValue(TableView.HorizontalOffsetProperty, 5000d);
            tableView.UpdateLayout();

            Assert.IsTrue(tableView.HorizontalOffset > panel.ActualWidth,
                $"Precondition: the offset ({tableView.HorizontalOffset}) is past the panel's extent ({panel.ActualWidth})");

            var clip = panel.Clip;
            Assert.IsNotNull(clip, "A clip is applied once the headers are scrolled");
            Assert.IsTrue(clip!.Rect.Width >= 0,
                $"Header clip width must never be negative (was {clip.Rect.Width})");
            Assert.IsTrue(clip.Rect.X >= 0 && clip.Rect.X <= panel.ActualWidth,
                $"Header clip X must stay inside the panel's extent (was {clip.Rect.X} for a panel {panel.ActualWidth} wide)");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task HeaderClip_IsUnchanged_WhenScrolledWithinTheHeadersPanelExtent()
    {
        var tableView = await CreateTableViewAsync(frozenColumnCount: 0);
        var panel = GetScrollableHeadersPanel(tableView);

        const double offset = 60d;
        Assert.IsTrue(panel.ActualWidth > offset, "Precondition: the offset is inside the panel's extent");

        tableView.SetValue(TableView.HorizontalOffsetProperty, offset);
        tableView.UpdateLayout();

        var clip = panel.Clip;
        Assert.IsNotNull(clip, "A clip is applied once the headers are scrolled");
        Assert.AreEqual(offset, clip!.Rect.X, 0.01, "Clip X is the horizontal offset");
        Assert.AreEqual(panel.ActualWidth - offset, clip.Rect.Width, 0.01, "Clip width is the unscrolled remainder");
    }

    private static StackPanel GetScrollableHeadersPanel(TableView tableView)
    {
        var headerRow = tableView.FindDescendants().OfType<TableViewHeaderRow>().FirstOrDefault();
        Assert.IsNotNull(headerRow, "Precondition: the header row is in the visual tree");

        var panel = headerRow!.FindDescendant<StackPanel>(x => x.Name is "ScrollableHeadersPanel");
        Assert.IsNotNull(panel, "Precondition: the scrollable headers panel is in the visual tree");

        return panel!;
    }

    private static async Task<TableView> CreateTableViewAsync(int frozenColumnCount)
    {
        var tableView = new TableView
        {
            Width = 250,
            Height = 300,
            AutoGenerateColumns = false,
            FrozenColumnCount = frozenColumnCount
        };

        for (var i = 0; i < 5; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(i < frozenColumnCount ? 200 : 60),
                Binding = new Binding { Path = new PropertyPath(nameof(HeaderRowTestItem.Name)) }
            });
        }

        tableView.ItemsSource = new[]
        {
            new HeaderRowTestItem { Name = "Alpha" },
            new HeaderRowTestItem { Name = "Beta" },
            new HeaderRowTestItem { Name = "Gamma" }
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class HeaderRowTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
