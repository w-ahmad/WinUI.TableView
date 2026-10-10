using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests the ItemsRepeater-based row host: that realization tracks the viewport rather than the item count, that
/// rows are recycled, and that selection does not force rows to be realized.
/// </summary>
[TestClass]
public class TableViewRowHostTests
{
    private const double ViewportHeight = 400d;

    [UITestMethod]
    public async Task Realization_TracksTheViewport_NotTheItemCount()
    {
        var small = await CreateTableViewAsync(itemCount: 200);
        var smallRealized = RealizedRows(small).Count;
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(small);

        var large = await CreateTableViewAsync(itemCount: 100_000);
        var largeRealized = RealizedRows(large).Count;
        var maxExpected = GetMaxRealizedRows(large, ViewportHeight, 40d);
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(large);

        Assert.IsTrue(smallRealized > 0, "some rows should be realized");

        // The whole point of the migration: 500x the items must not mean more row elements.
        Assert.IsTrue(largeRealized <= smallRealized + 2,
            $"realized rows grew with the item count: {smallRealized} for 200 items vs {largeRealized} for 100,000");

        // And the realized count has to stay in the region the viewport and its cache cover.
        Assert.IsTrue(largeRealized <= maxExpected,
            $"{largeRealized} rows realized for a {ViewportHeight}px viewport, expected at most {maxExpected}");
    }

    [UITestMethod]
    public async Task RowTemplate_AppliesAndBuildsCells()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);

        var rows = RealizedRows(tableView);
        Assert.IsTrue(rows.Count > 0);

        foreach (var row in rows)
        {
            // The row template has to have been applied for the presenter part to exist, and the presenter is
            // what builds a cell per visible column.
            Assert.IsNotNull(row.RowPresenter, "the row template should have been applied");
            Assert.AreEqual(tableView.Columns.VisibleColumns.Count, row.Cells.Count);
            Assert.IsTrue(row.ActualHeight > 0, "a realized row should have been arranged");

            foreach (var cell in row.Cells)
            {
                Assert.IsNotNull(cell.Column);
                Assert.AreEqual(cell.Column!.ActualWidth, cell.Width);
            }
        }

        // Rows are stacked in order with no gaps, which is what the layout's offset arithmetic produces.
        var ordered = rows.Where(x => x.Index >= 0).OrderBy(x => x.Index).ToList();

        for (var i = 1; i < ordered.Count; i++)
        {
            Assert.AreEqual(ordered[i - 1].Index + 1, ordered[i].Index, "realized rows should be a contiguous run");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Rows_AreRecycled_WhileScrolling()
    {
        var tableView = await CreateTableViewAsync(itemCount: 5_000);

        // Start away from the top, so the cache above the viewport is realized too and both windows have one shape.
        await ScrollToAsync(tableView, 2_000d);

        var before = RealizedRows(tableView).ToHashSet();
        Assert.IsTrue(before.Count > 0);

        // Far enough that the two windows share no row, so every row at the new offset has to be one left behind.
        await ScrollToAsync(tableView, 4_000d);

        var after = RealizedRows(tableView).ToList();

        Assert.IsTrue(after.Count > 0, "rows should be realized at the new offset");
        Assert.IsTrue(after.Where(x => x.Index >= 0).All(x => x.Index * 40d >= 2_000d + ViewportHeight), "the new window should not overlap the old one");
        Assert.AreEqual(before.Count, after.Count, "the number of row elements should not change while scrolling");
        Assert.IsTrue(after.All(before.Contains),
            $"{after.Count(x => !before.Contains(x))} of {after.Count} rows were built new instead of reusing the rows scrolled away from");

        // Reused rows must be showing the items at their new position, not the ones they were built for.
        foreach (var row in after)
        {
            if (row.Index >= 0)
            {
                Assert.AreSame(tableView.Items[row.Index], row.Content, $"row {row.Index} shows the wrong item");
            }
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task UnloadedTable_IsCollected()
    {
        var table = await CreateScrollAndUnloadAsync();

        // XAML lets go of an unloaded tree over the next few ticks, so allow a few rounds.
        for (var round = 0; round < 10 && table.IsAlive; round++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(100);
        }

        // A table that outlives its page keeps every row and cell alive with it, and each garbage collection then
        // has to walk all of them, which is what made scrolling stall once a few pages had been visited.
        Assert.IsFalse(table.IsAlive, "the unloaded table is still alive; something outside it keeps it rooted");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> CreateScrollAndUnloadAsync()
    {
        var tableView = await CreateTableViewAsync(itemCount: 2_000);
        await ScrollToAsync(tableView, 4_000d);
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);

        return new WeakReference(tableView);
    }

    [UITestMethod]
    public async Task CollectedTables_AreNotCalledBackIntoByTheRepeater()
    {
        // A repeater that calls into the layout of a table that has been collected fails inside the interop layer
        // with a NullReferenceException, which the debugger reports even when nothing crashes.
        var interopFailures = new List<string>();

        void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is NullReferenceException && e.Exception.StackTrace?.Contains("ComWrappers") is true)
            {
                interopFailures.Add(e.Exception.StackTrace);
            }
        }

        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

        try
        {
            for (var i = 0; i < 3; i++)
            {
                await CreateScrollAndUnloadAsync();
            }

            for (var round = 0; round < 5; round++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await Task.Delay(100);
            }

            var tableView = await CreateTableViewAsync(itemCount: 2_000);
            await ScrollToAsync(tableView, 2_000d);
            await ScrollToAsync(tableView, 0d);
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
        }

        Assert.AreEqual(0, interopFailures.Count, "a collected table was called back into: " + string.Join(" --- ", interopFailures));
    }

    [UITestMethod]
    public async Task ReloadedTable_RealizesItsRowsAgain()
    {
        var tableView = await CreateTableViewAsync(itemCount: 2_000);
        await ScrollToAsync(tableView, 2_000d);

        // Unloading takes the layout off the repeater, which recycles every row; loading again has to bring them back.
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
        await ScrollToAsync(tableView, 4_000d);

        var rows = RealizedRows(tableView).Where(x => x.Index >= 0).ToList();
        Assert.IsTrue(rows.Count > 0, "the reloaded table should realize rows");
        Assert.IsTrue(rows.Any(x => x.Index * 40d >= 4_000d - 40d && x.Index * 40d <= 4_000d + ViewportHeight), "the rows should be the ones at the new offset");

        foreach (var row in rows)
        {
            Assert.AreSame(tableView.Items[row.Index], row.Content, $"row {row.Index} shows the wrong item");
            Assert.AreEqual(tableView.Columns.VisibleColumns.Count, row.Cells.Count);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task SelectingALargeRange_DoesNotRealizeRows()
    {
        var tableView = await CreateTableViewAsync(itemCount: 100_000);
        var realizedBefore = RealizedRows(tableView).Count;

        tableView.SelectRange(new ItemIndexRange(0, 100_000));
        await Task.Yield();

        Assert.AreEqual(100_000, tableView.SelectedItems.Count);
        Assert.AreEqual(1, tableView.SelectedRanges.Count, "a contiguous selection should be one range");
        Assert.IsTrue(tableView.IsRowSelected(50_000));

        var realizedAfter = RealizedRows(tableView).Count;
        Assert.IsTrue(realizedAfter <= realizedBefore + 2,
            $"selecting everything realized rows: {realizedBefore} -> {realizedAfter}");

        // The rows that are on screen must still show as selected.
        Assert.IsTrue(RealizedRows(tableView).All(row => row.IsSelected));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task SelectedItems_ProjectsWithoutMaterialising()
    {
        var tableView = await CreateTableViewAsync(itemCount: 10_000);

        tableView.SelectRange(new ItemIndexRange(10, 90));

        Assert.AreEqual(90, tableView.SelectedItems.Count);
        Assert.AreSame(tableView.Items[10], tableView.SelectedItems[0]);
        Assert.AreSame(tableView.Items[99], tableView.SelectedItems[89]);
        Assert.AreSame(tableView.Items[10], tableView.SelectedItem);
        Assert.AreEqual(10, tableView.SelectedIndex);

        tableView.DeselectRange(new ItemIndexRange(50, 1));

        Assert.AreEqual(89, tableView.SelectedItems.Count);
        Assert.AreEqual(2, tableView.SelectedRanges.Count, "removing one row from the middle splits the range");
        Assert.IsFalse(tableView.IsRowSelected(50));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ContainerFromIndex_And_IndexFromContainer_RoundTrip()
    {
        var tableView = await CreateTableViewAsync(itemCount: 1_000);

        var rows = RealizedRows(tableView);
        Assert.IsTrue(rows.Count > 0);

        foreach (var row in rows)
        {
            Assert.AreEqual(row.Index, tableView.IndexFromContainer(row));
            Assert.AreSame(row, tableView.ContainerFromIndex(row.Index));
            Assert.AreSame(tableView.Items[row.Index], tableView.ItemFromContainer(row));
        }

        // A row far outside the viewport is not realized, so there is no container for it.
        Assert.IsNull(tableView.ContainerFromIndex(999));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }


    [UITestMethod]
    public async Task AddingAndRemovingOneItem_KeepsTheRealizedSetSmall()
    {
        var source = CreateItems(1_000);
        var tableView = await CreateTableViewAsync(source);
        var realizedBefore = RealizedRows(tableView).Count;

        source.Insert(0, new HostItem { Id = -1, Name = "Inserted", Category = "A" });
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(1_001, tableView.Items.Count);
        Assert.IsTrue(RealizedRows(tableView).Count <= realizedBefore + 2);

        source.RemoveAt(0);
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(1_000, tableView.Items.Count);
        Assert.IsTrue(RealizedRows(tableView).Count <= realizedBefore + 2);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task IncrementalLoading_LoadsPagesRatherThanEverything()
    {
        var source = new CountingIncrementalSource();
        var tableView = await CreateTableViewAsync(source);

        await WaitForLayoutAsync(tableView);

        Assert.IsTrue(source.LoadCount > 0, "the source should be asked for its first page");
        Assert.IsTrue(source.Count < CountingIncrementalSource.MaxItems,
            $"the whole source was pulled in: {source.Count} of {CountingIncrementalSource.MaxItems}");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static List<TableViewRow> RealizedRows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(x => x.IsLoaded)];
    }

    private static Task<TableView> CreateTableViewAsync(int itemCount) => CreateTableViewAsync(CreateItems(itemCount));

    private static async Task<TableView> CreateTableViewAsync(object source)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = ViewportHeight,
            RowHeight = 40,
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        });

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Category",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Category)) }
        });

        tableView.ItemsSource = source;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }

    private static ObservableCollection<HostItem> CreateItems(int count)
    {
        var items = new ObservableCollection<HostItem>();

        for (var i = 0; i < count; i++)
        {
            items.Add(new HostItem
            {
                Id = i,
                Name = $"Item {i}",
                Category = ((char)('A' + (i % 3))).ToString()
            });
        }

        return items;
    }
}

/// <summary>
/// An incrementally loading source that records how many times it was asked for more items.
/// </summary>
internal sealed class CountingIncrementalSource : ObservableCollection<HostItem>, ISupportIncrementalLoading
{
    public const int MaxItems = 5_000;
    private const uint PageSize = 50;

    public int LoadCount { get; private set; }

    public bool HasMoreItems => Count < MaxItems;

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return AsyncInfo.Run(_ =>
        {
            LoadCount++;

            for (var i = 0; i < PageSize && Count < MaxItems; i++)
            {
                Add(new HostItem { Id = Count, Name = $"Item {Count}", Category = ((char)('A' + (Count % 3))).ToString() });
            }

            return Task.FromResult(new LoadMoreItemsResult { Count = PageSize });
        });
    }
}

internal sealed class HostItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public override string ToString() => Name;
}
