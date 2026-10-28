using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

/// <summary>
/// Covers <see cref="TableViewColumn.Sort"/>, <see cref="TableViewColumn.Group"/> and
/// <see cref="TableViewColumn.Ungroup"/>: the public way to do what the column header's commands do.
/// </summary>
[TestClass]
public class TableViewColumnSortingAndGroupingTests
{
    [UITestMethod]
    public async Task Sort_OrdersTheItems_AndShowsTheDirection()
    {
        var (tableView, _, valueColumn) = await CreateTableViewAsync();

        Assert.IsTrue(valueColumn.Sort(SortDirection.Descending));

        Assert.AreEqual(SortDirection.Descending, valueColumn.SortDirection);
        CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, tableView.CollectionView.Cast<TestItem>().Select(x => x.Value).ToArray());
    }

    [UITestMethod]
    public async Task Sort_ReplacesOtherSorting_UnlessAskedToKeepIt()
    {
        var (_, categoryColumn, valueColumn) = await CreateTableViewAsync();

        categoryColumn.Sort(SortDirection.Ascending);
        valueColumn.Sort(SortDirection.Ascending, clearOtherSorting: false);
        Assert.AreEqual(SortDirection.Ascending, categoryColumn.SortDirection);

        valueColumn.Sort(SortDirection.Descending);
        Assert.IsNull(categoryColumn.SortDirection);
        Assert.AreEqual(SortDirection.Descending, valueColumn.SortDirection);
    }

    [UITestMethod]
    public async Task Sort_WithNullDirection_RemovesTheColumnsSort()
    {
        var (tableView, _, valueColumn) = await CreateTableViewAsync();
        valueColumn.Sort(SortDirection.Descending);

        Assert.IsTrue(valueColumn.Sort(null));

        Assert.IsNull(valueColumn.SortDirection);
        Assert.AreEqual(0, tableView.SortDescriptions.Count);
    }

    [UITestMethod]
    public async Task Sort_ReturnsFalse_WhenTheColumnCantSort()
    {
        var (_, _, valueColumn) = await CreateTableViewAsync();
        valueColumn.CanSort = false;

        Assert.IsFalse(valueColumn.Sort(SortDirection.Ascending));
        Assert.IsNull(valueColumn.SortDirection);
    }

    [UITestMethod]
    public async Task Sort_ReturnsFalse_WhenASortingHandlerHandlesIt()
    {
        var (tableView, _, valueColumn) = await CreateTableViewAsync();
        tableView.Sorting += (_, e) => e.Handled = true;

        Assert.IsFalse(valueColumn.Sort(SortDirection.Ascending));
        Assert.IsNull(valueColumn.SortDirection);
    }

#if WINDOWS
    [UITestMethod]
    public async Task Group_GroupsByTheColumn_LikeTheHeadersGroupCommand()
    {
        var (tableView, categoryColumn, _) = await CreateTableViewAsync();

        Assert.IsTrue(categoryColumn.Group());

        Assert.IsTrue(categoryColumn.IsGrouped);
        Assert.IsTrue(tableView.IsGrouped);
        Assert.AreEqual(SortDirection.Ascending, categoryColumn.SortDirection);
        Assert.AreEqual(2, tableView.CollectionView.CollectionGroups?.Count);
    }

    [UITestMethod]
    public async Task Group_ReturnsFalse_WhenAlreadyGrouped()
    {
        var (tableView, categoryColumn, _) = await CreateTableViewAsync();
        categoryColumn.Group();

        Assert.IsFalse(categoryColumn.Group());
        Assert.AreEqual(1, tableView.GroupDescriptions.Count);
    }

    [UITestMethod]
    public async Task Group_ReturnsFalse_WhenGroupingIsOff()
    {
        var (tableView, categoryColumn, _) = await CreateTableViewAsync();
        tableView.CanGroupColumns = false;

        Assert.IsFalse(categoryColumn.Group());
        Assert.IsFalse(categoryColumn.IsGrouped);
    }

    [UITestMethod]
    public async Task Group_ReturnsFalse_WhenAGroupingHandlerHandlesIt()
    {
        var (tableView, categoryColumn, _) = await CreateTableViewAsync();
        tableView.Grouping += (_, e) => e.Handled = true;

        Assert.IsFalse(categoryColumn.Group());
        Assert.IsFalse(categoryColumn.IsGrouped);
    }

    [UITestMethod]
    public async Task Group_NestsInsideAnExistingGrouping()
    {
        var (tableView, categoryColumn, valueColumn) = await CreateTableViewAsync();

        categoryColumn.Group();
        valueColumn.Group();

        CollectionAssert.AreEqual(
            new TableViewColumn[] { categoryColumn, valueColumn },
            tableView.GroupDescriptions.OfType<ColumnGroupDescription>().Select(x => x.Column).ToArray());
    }

    [UITestMethod]
    public async Task Ungroup_RemovesTheGroupingAndTheOrderItAdded()
    {
        var (tableView, categoryColumn, _) = await CreateTableViewAsync();
        categoryColumn.Group();

        Assert.IsTrue(categoryColumn.Ungroup());

        Assert.IsFalse(categoryColumn.IsGrouped);
        Assert.IsFalse(tableView.IsGrouped);
        Assert.IsNull(categoryColumn.SortDirection);
        Assert.AreEqual(0, tableView.SortDescriptions.Count);
    }

    [UITestMethod]
    public async Task Ungroup_KeepsASortTheColumnHadBeforeGrouping()
    {
        var (_, categoryColumn, _) = await CreateTableViewAsync();
        categoryColumn.Sort(SortDirection.Descending);
        categoryColumn.Group();

        categoryColumn.Ungroup();

        Assert.AreEqual(SortDirection.Descending, categoryColumn.SortDirection);
    }

    [UITestMethod]
    public async Task Ungroup_ReturnsFalse_WhenNotGrouped()
    {
        var (_, categoryColumn, _) = await CreateTableViewAsync();

        Assert.IsFalse(categoryColumn.Ungroup());
    }
#endif

    private static async Task<(TableView TableView, TableViewTextColumn CategoryColumn, TableViewTextColumn ValueColumn)> CreateTableViewAsync()
    {
        var items = Enumerable.Range(0, 4)
            .Select(i => new TestItem { Category = i % 2 == 0 ? "A" : "B", Value = i })
            .ToArray();

        var tableView = new TableView { CanGroupColumns = true };
        var categoryColumn = new TableViewTextColumn { Header = "Category", Binding = new Binding { Path = new PropertyPath(nameof(TestItem.Category)) } };
        var valueColumn = new TableViewTextColumn { Header = "Value", Binding = new Binding { Path = new PropertyPath(nameof(TestItem.Value)) } };
        tableView.Columns.Add(categoryColumn);
        tableView.Columns.Add(valueColumn);
        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return (tableView, categoryColumn, valueColumn);
    }

    private sealed class TestItem
    {
        public string Category { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
