using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewRowDetailsScrollTests
{
    private const double Tolerance = 1.5;

    [UITestMethod]
    public async Task ExpandingDetails_ViaShowDetailPane_BringsTheRowFullyIntoView()
    {
        var tableView = await CreateTableViewAsync(TableViewRowDetailsVisibilityMode.VisibleWhenExpanded);
        try
        {
            var row = await GetLastFullyVisibleRowAsync(tableView);
            var scrollViewer = GetScrollViewer(tableView);
            Assert.IsFalse(IsBelowViewport(row, scrollViewer), "Precondition: the row is fully visible before expanding");

            row.RowPresenter!.ShowDetailPane(visible: true);
            await Task.Delay(500);

            Assert.IsTrue(row.RowPresenter.IsDetailsPanelVisible, "Precondition: the details panel is open");
            Assert.IsFalse(IsBelowViewport(row, scrollViewer),
                "Expanding the details pane must scroll the row so its details are visible");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task CollapsingDetails_DoesNotScroll()
    {
        var tableView = await CreateTableViewAsync(TableViewRowDetailsVisibilityMode.VisibleWhenExpanded);
        try
        {
            var row = await GetLastFullyVisibleRowAsync(tableView);
            var scrollViewer = GetScrollViewer(tableView);

            row.RowPresenter!.ShowDetailPane(visible: true);
            await Task.Delay(500);

            var offset = scrollViewer.VerticalOffset;

            row.RowPresenter.ShowDetailPane(visible: false);
            await Task.Delay(500);

            Assert.IsFalse(row.RowPresenter.IsDetailsPanelVisible, "Precondition: the details panel is closed");
            Assert.AreEqual(offset, scrollViewer.VerticalOffset, Tolerance, "Collapsing the details pane must not scroll");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task SelectingRow_InVisibleWhenSelectedMode_BringsTheRowFullyIntoView()
    {
        var tableView = await CreateTableViewAsync(TableViewRowDetailsVisibilityMode.VisibleWhenSelected);
        try
        {
            var row = await GetLastFullyVisibleRowAsync(tableView);
            var scrollViewer = GetScrollViewer(tableView);
            Assert.IsFalse(IsBelowViewport(row, scrollViewer), "Precondition: the row is fully visible before selecting");

            tableView.SelectedIndex = row.Index;
            await Task.Delay(500);

            Assert.IsTrue(row.RowPresenter!.IsDetailsPanelVisible, "Precondition: the selected row shows its details");
            Assert.IsFalse(IsBelowViewport(row, scrollViewer),
                "Selecting a row that opens its details must scroll the row so its details are visible");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task RealizingRow_WithAlreadyOpenDetails_DoesNotScroll()
    {
        var tableView = await CreateTableViewAsync(TableViewRowDetailsVisibilityMode.VisibleWhenExpanded);
        try
        {
            var scrollViewer = GetScrollViewer(tableView);
            var row = tableView.FindDescendants().OfType<TableViewRow>().First(r => r.Index == 3);

            row.RowPresenter!.ShowDetailPane(visible: true);
            await Task.Delay(500);

            // Leave only the lower part of the expanded row visible, so a spurious scroll-into-view
            // would visibly move the offset.
            var rowTop = row.TransformToVisual(scrollViewer).TransformPoint(new(0, 0)).Y + scrollViewer.VerticalOffset;
            var targetOffset = rowTop + 20;
            scrollViewer.ChangeView(null, targetOffset, null, true);
            await Task.Delay(500);

            Assert.AreEqual(targetOffset, scrollViewer.VerticalOffset, Tolerance, "Precondition: the row is partly scrolled out of view");

            // This is what preparing (realizing or recycling) a container does for a row whose details are open.
            row.RowPresenter.ApplyDetailsPaneState(row.Content);
            await Task.Delay(500);

            Assert.AreEqual(targetOffset, scrollViewer.VerticalOffset, Tolerance,
                "Restoring an already open details pane on a realized row must not scroll it into view");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }
    private static ScrollViewer GetScrollViewer(TableView tableView)
    {
        var scrollViewer = tableView.FindDescendant<ScrollViewer>();
        Assert.IsNotNull(scrollViewer, "Precondition: the scroll viewer is in the visual tree");
        return scrollViewer!;
    }

    private static bool IsBelowViewport(TableViewRow row, ScrollViewer scrollViewer)
    {
        var top = row.TransformToVisual(scrollViewer).TransformPoint(new(0, 0)).Y;
        return top + row.ActualHeight > scrollViewer.ActualHeight + Tolerance;
    }

    // Picks the realized row sitting lowest while still fully inside the viewport, so expanding it pushes
    // its details past the bottom edge unless the table scrolls.
    private static async Task<TableViewRow> GetLastFullyVisibleRowAsync(TableView tableView)
    {
        await Task.Delay(300);

        var scrollViewer = GetScrollViewer(tableView);
        var row = tableView.FindDescendants().OfType<TableViewRow>()
            .Where(r => r.IsLoaded && r.Index >= 0 && !IsBelowViewport(r, scrollViewer))
            .OrderByDescending(r => r.Index)
            .FirstOrDefault();

        Assert.IsNotNull(row, "Precondition: a fully visible row exists");
        return row!;
    }

    private static async Task<TableView> CreateTableViewAsync(TableViewRowDetailsVisibilityMode mode)
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false,
            RowDetailsVisibilityMode = mode,
            RowDetailsTemplate = (DataTemplate)XamlReader.Load(
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border Height=\"200\" /></DataTemplate>")
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(DetailsTestItem.Name)) }
        });

        tableView.ItemsSource = Enumerable.Range(0, 30)
            .Select(i => new DetailsTestItem { Name = $"Item {i}" })
            .ToArray();

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class DetailsTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
