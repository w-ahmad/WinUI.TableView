using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Layout;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests finding rows from the realized rows themselves, which is what hit testing, paging and incremental loading
/// rely on instead of a layout that knows every row's height.
/// </summary>
[TestClass]
public class TableViewRealizedRowsTests
{
    [UITestMethod]
    public void FindAtOffset_ReturnsTheRowContainingTheOffset_OrTheNearest()
    {
        var rows = CreateRows((10, 400, 40), (11, 440, 40), (12, 480, 60));

        Assert.AreEqual(10, rows.FindAtOffset(400, out var exact));
        Assert.IsTrue(exact);
        Assert.AreEqual(11, rows.FindAtOffset(479.9, out exact));
        Assert.IsTrue(exact);
        Assert.AreEqual(12, rows.FindAtOffset(530, out exact));
        Assert.IsTrue(exact);

        Assert.AreEqual(10, rows.FindAtOffset(100, out exact), "above the realized rows is the first one");
        Assert.IsFalse(exact);
        Assert.AreEqual(12, rows.FindAtOffset(900, out exact), "below the realized rows is the last one");
        Assert.IsFalse(exact);
    }

    [UITestMethod]
    public void FindAtOffset_SkipsElementsThatAreNotLaidOut()
    {
        var hidden = new Border();
        var bounds = new Dictionary<UIElement, (double, double)>
        {
            [new Border()] = (0, 40),
            [hidden] = (-10_040, 0)
        };
        var entries = new List<(int, UIElement)> { (0, bounds.Keys.First()), (7, hidden) };
        var rows = new TableViewRealizedRows(() => entries, element => bounds[element]);

        Assert.AreEqual(0, rows.FindAtOffset(-10_040, out var exact), "a zero-sized element kept out of view must not win");
        Assert.IsFalse(exact);
        Assert.IsFalse(rows.TryGetBounds(7, out _, out _), "a zero-sized element has no usable bounds");
    }

    [UITestMethod]
    public void Get_FindsByVisualIndex_AndInvalidateRebuilds()
    {
        var entries = new List<(int, UIElement)> { (5, new Border()), (2, new Border()), (9, new Border()) };
        var rows = new TableViewRealizedRows(() => entries, _ => (0, 1));

        Assert.AreSame(entries[1].Item2, rows.Get(2));
        Assert.AreSame(entries[0].Item2, rows.Get(5));
        Assert.IsNull(rows.Get(4));
        Assert.AreEqual(9, rows.LastVisualIndex);
        CollectionAssert.AreEqual(new[] { entries[1].Item2, entries[0].Item2, entries[2].Item2 }, rows.InOrder.ToArray());

        entries.Add((12, new Border()));
        Assert.AreEqual(9, rows.LastVisualIndex, "the list is kept until it is invalidated");

        rows.Invalidate();
        Assert.AreEqual(12, rows.LastVisualIndex);
    }

    [UITestMethod]
    public async Task HitTest_AccountsForTheTablesTopPadding()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50, tableView => tableView.Padding = new Thickness(0, 20, 0, 0));

        // 20px of padding, then 40px rows: y = 130 is 110px into the rows, inside row 2.
        Assert.AreEqual(2, tableView.GetVisualRowIndexAtCanvasY(130));
        Assert.AreEqual(0, tableView.GetVisualRowIndexAtCanvasY(25));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task HitTest_BelowTheLastRow_FindsNoRow()
    {
        var tableView = await CreateTableViewAsync(itemCount: 3);

        Assert.AreEqual(2, tableView.GetVisualRowIndexAtCanvasY(3 * 40 - 5));
        Assert.AreEqual(-1, tableView.GetVisualRowIndexAtCanvasY(3 * 40 + 5));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task PageSize_IsTheViewportOverTheRowHeight()
    {
        var tableView = await CreateTableViewAsync(itemCount: 200);
        var viewport = GetScrollViewer(tableView).ViewportHeight;

        Assert.AreEqual((int)(viewport / 40), tableView.CalculateAvailablePageSize());

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task IncrementalLoading_LoadsAnotherPage_WhenScrolledToTheEnd()
    {
        var source = new CountingIncrementalSource();
        var tableView = await CreateTableViewAsync(source);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        var loads = source.LoadCount;
        Assert.IsTrue(loads > 0, "the empty source should be asked for its first page");

        await ScrollToAsync(tableView, GetScrollViewer(tableView).ScrollableHeight);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        Assert.IsTrue(source.LoadCount > loads, "reaching the last row should ask for the next page");
        Assert.IsTrue(source.Count < CountingIncrementalSource.MaxItems, "one scroll should not pull in the whole source");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task IncrementalLoading_WithTriggerNone_LoadsNothing()
    {
        var source = new CountingIncrementalSource();
        var tableView = await CreateTableViewAsync(source, tableView => tableView.IncrementalLoadingTrigger = IncrementalLoadingTrigger.None);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(0, source.LoadCount);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static TableViewRealizedRows CreateRows(params (int VisualIndex, double Top, double Height)[] rows)
    {
        var elements = rows.Select(row => (row.VisualIndex, (UIElement)new Border(), row.Top, row.Height)).ToList();
        var bounds = elements.ToDictionary(x => x.Item2, x => (x.Top, x.Height));

        return new TableViewRealizedRows(() => elements.Select(x => (x.VisualIndex, x.Item2)), element => bounds[element]);
    }

    private static Task<TableView> CreateTableViewAsync(int itemCount, System.Action<TableView>? configure = null)
    {
        return CreateTableViewAsync(
            new ObservableCollection<HostItem>(Enumerable.Range(0, itemCount).Select(i => new HostItem { Id = i, Name = $"Item {i}" })),
            configure);
    }

    private static async Task<TableView> CreateTableViewAsync(object source, System.Action<TableView>? configure = null)
    {
        var tableView = new TableView { Width = 600, Height = 400, RowHeight = 40, AutoGenerateColumns = false };
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        });

        configure?.Invoke(tableView);
        tableView.ItemsSource = source;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }
}
