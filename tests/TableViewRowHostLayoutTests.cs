using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests what the row host's layout decides: where a row scrolled into view lands, how wide the rows are, and how
/// many rows stay realized. The layout only knows the heights of the rows it has realized, so rows far away are
/// reached from an estimate.
/// </summary>
[TestClass]
public class TableViewRowHostLayoutTests
{
    private const double ViewportHeight = 400d;

    [UITestMethod]
    public async Task ScrollRowIntoView_FarBelow_RealizesTheRowAndTheRowsAroundIt()
    {
        var tableView = await CreateTableViewAsync(CreateItems(100_000));

        var row = await tableView.ScrollRowIntoView(80_000);
        await WaitForScrollToSettleAsync(tableView);

        Assert.IsNotNull(row);
        Assert.AreSame(tableView.Items[80_000], row!.Content);
        AssertFullyInView(tableView, row);
        Assert.IsTrue(tableView.Rows.Count > 5, "the rows around the target should be realized too");
        Assert.IsTrue(tableView.Rows.All(x => Math.Abs(x.Index - 80_000) < 50), "only rows around the target should be realized");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_FarAbove_BringsTheRowToTheTop()
    {
        var tableView = await CreateTableViewAsync(CreateItems(100_000));
        await ScrollToAsync(tableView, GetScrollViewer(tableView).ScrollableHeight);

        var row = await tableView.ScrollRowIntoView(1_000);
        await WaitForScrollToSettleAsync(tableView);

        Assert.IsNotNull(row);
        Assert.AreSame(tableView.Items[1_000], row!.Content);
        Assert.AreEqual(0d, row.ActualOffset.Y - tableView.RowsViewportTop, 1d, "a row brought in from above should stop at the top");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_WithRowsOfDifferentHeights_ShowsTheWholeRow()
    {
        // Every seventh row is three lines tall, so the layout's estimate of where a far row is is off.
        var items = CreateItems(5_000);

        foreach (var item in items.Where(x => x.Id % 7 is 0))
        {
            item.Name = "one\ntwo\nthree";
        }

        var tableView = await CreateTableViewAsync(items, rowHeight: double.NaN);

        foreach (var index in new[] { 4_004, 700, 4_998, 0 })
        {
            var row = await tableView.ScrollRowIntoView(index);
            await WaitForScrollToSettleAsync(tableView);

            Assert.IsNotNull(row, $"row {index}");
            Assert.AreSame(tableView.Items[index], row!.Content, $"row {index}");
            AssertFullyInView(tableView, row);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_TheLastRow_ShowsAllOfIt()
    {
        var tableView = await CreateTableViewAsync(CreateItems(10_000));
        var scrollViewer = GetScrollViewer(tableView);

        var row = await tableView.ScrollRowIntoView(9_999);
        await WaitForScrollToSettleAsync(tableView);

        Assert.IsNotNull(row);
        AssertFullyInView(tableView, row!);
        Assert.AreEqual(scrollViewer.ScrollableHeight, scrollViewer.VerticalOffset, 1d, "the last row should be at the very bottom");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task HorizontalExtent_IsTheWidthOfTheColumns_WithOrWithoutRows()
    {
        foreach (var itemCount in new[] { 0, 50 })
        {
            var tableView = await CreateTableViewAsync(CreateItems(itemCount), columnWidth: 300d, columnCount: 3);
            var scrollViewer = GetScrollViewer(tableView);

            Assert.AreEqual(tableView.CellsHorizontalOffset + 900d, scrollViewer.ExtentWidth, 1d, $"{itemCount} items");
            Assert.IsTrue(scrollViewer.ScrollableWidth > 0d, $"{itemCount} items: the columns should scroll sideways");

            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task HorizontalExtent_FollowsAColumnsWidth()
    {
        var tableView = await CreateTableViewAsync(CreateItems(50), columnWidth: 300d, columnCount: 3);
        var scrollViewer = GetScrollViewer(tableView);

        tableView.Columns[0].Width = new GridLength(500d);
        await WaitForLayoutAsync(tableView);
        Assert.AreEqual(tableView.CellsHorizontalOffset + 1_100d, scrollViewer.ExtentWidth, 1d, "wider");

        tableView.Columns[0].Width = new GridLength(100d);
        await WaitForLayoutAsync(tableView);
        Assert.AreEqual(tableView.CellsHorizontalOffset + 700d, scrollViewer.ExtentWidth, 1d, "narrower");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Rows_SpanTheViewport_WhenTheColumnsAreNarrower()
    {
        var tableView = await CreateTableViewAsync(CreateItems(50), columnWidth: 100d);
        var viewportWidth = GetScrollViewer(tableView).ViewportWidth;

        Assert.IsTrue(tableView.Rows.Count > 0);

        foreach (var row in tableView.Rows)
        {
            Assert.AreEqual(viewportWidth, row.ActualWidth, 1d, "a row's background and selection should reach the edge");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task FarJumps_KeepTheRowsBounded()
    {
        var tableView = await CreateTableViewAsync(CreateItems(100_000));
        var repeater = GetRowsRepeater(tableView);
        var maxRealized = GetMaxRealizedRows(tableView, ViewportHeight, 40d);
        var scrollViewer = GetScrollViewer(tableView);

        foreach (var fraction in new[] { 0.5, 0.01, 0.9, 0.25, 1d, 0d })
        {
            await ScrollToAsync(tableView, scrollViewer.ScrollableHeight * fraction);

            Assert.IsTrue(tableView.Rows.Count <= maxRealized, $"{tableView.Rows.Count} rows realized at {fraction:P0}");
            Assert.IsTrue(VisualTreeHelper.GetChildrenCount(repeater) <= maxRealized * 2,
                $"{VisualTreeHelper.GetChildrenCount(repeater)} children in the repeater at {fraction:P0}");
        }

        Assert.IsTrue(tableView.RowElementFactory!.CreatedRowCount <= maxRealized * 2,
            $"{tableView.RowElementFactory.CreatedRowCount} rows built for at most {maxRealized} realized at once");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Reload_LetsGoOfTheRows_AndRealizesThemAgain()
    {
        var tableView = await CreateTableViewAsync(CreateItems(10_000));
        await ScrollToAsync(tableView, 20_000d);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        Assert.AreEqual(0, tableView.Rows.Count, "an unloaded table lets go of its rows");

        // The rows stay pooled in the repeater for the next load, but let go of their items once the unload is over.
        await Task.Delay(50);
        var repeater = GetRowsRepeater(tableView);
        var pooled = Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(repeater)).Select(i => VisualTreeHelper.GetChild(repeater, i)).OfType<TableViewRow>().ToList();
        Assert.IsTrue(pooled.Count > 0);
        Assert.IsTrue(pooled.All(x => x.Content is null), "pooled rows should not keep the items they showed");

        // The table empties its items while unloaded, as it always has, so it comes back at the top.
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForScrollToSettleAsync(tableView);

        Assert.IsTrue(tableView.Rows.Count > 5, "the rows should be realized again");
        Assert.AreSame(tableView.Items[0], ((TableViewRow)tableView.RealizedRows.Get(0)!).Content);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static void AssertFullyInView(TableView tableView, TableViewRow row)
    {
        var top = row.ActualOffset.Y - tableView.RowsViewportTop;
        var viewportHeight = GetScrollViewer(tableView).ViewportHeight;

        Assert.IsTrue(top >= -1d && top + row.ActualHeight <= viewportHeight + 1d,
            $"row {row.Index} spans {top} to {top + row.ActualHeight} in a {viewportHeight}px viewport");
    }

    private static async Task<TableView> CreateTableViewAsync(
        ObservableCollection<HostItem> items,
        double rowHeight = 40d,
        double columnWidth = 200d,
        int columnCount = 2)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = ViewportHeight,
            RowHeight = rowHeight,
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended
        };

        for (var i = 0; i < columnCount; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = i is 0 ? "Name" : $"Category {i}",
                Width = new GridLength(columnWidth),
                Binding = new Binding { Path = new PropertyPath(i is 0 ? nameof(HostItem.Name) : nameof(HostItem.Category)) }
            });
        }

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }

    private static ObservableCollection<HostItem> CreateItems(int count)
    {
        return new(Enumerable.Range(0, count).Select(i => new HostItem
        {
            Id = i,
            Name = $"Item {i}",
            Category = ((char)('A' + (i % 3))).ToString()
        }));
    }
}
