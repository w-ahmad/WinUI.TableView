using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

/// <summary>
/// Covers row selection, cell selection, and <see cref="TableView.ScrollRowIntoView(int)"/>, both ungrouped
/// and grouped. <see cref="TableView.Items"/> is pruned by grouping - a collapsed group's items are removed
/// from it entirely and later items shift down to fill the gap (see <see cref="CollectionViewTests"/> for the
/// equivalent <see cref="CollectionView"/>-level coverage) - so every index used here is already in the one
/// space <see cref="TableView.SelectRange"/>, <see cref="TableView.SelectCellRange"/>, and
/// <see cref="TableView.ContainerFromIndex(int)"/> expect; there's no separate "visible" index space to
/// reconcile.
/// </summary>
[TestClass]
public class TableViewGroupingSelectionTests
{
    [UITestMethod]
    public async Task RowSelection_Ungrouped_SelectsExpectedItems()
    {
        var tableView = await CreateTableViewAsync(CreateItems());

        tableView.SelectRange(new ItemIndexRange(1, 2)); // Rows 1-2.

        Assert.AreEqual(2, tableView.SelectedItems.Count);
        CollectionAssert.AreEquivalent(new object[] { tableView.Items[1]!, tableView.Items[2]! }, tableView.SelectedItems.ToArray());
    }

    [UITestMethod]
    public async Task RowSelection_Grouped_SelectsExpectedItems()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        await GroupByCategoryAsync(tableView);

        // Nothing is collapsed, so Items still lines up with source order - rows 2-3 are "B"'s items.
        tableView.SelectRange(new ItemIndexRange(2, 2));

        Assert.AreEqual(2, tableView.SelectedItems.Count);
        Assert.IsTrue(tableView.SelectedItems.Cast<SelectionTestItem>().All(i => i.Category == "B"));
    }

    [UITestMethod]
    public async Task CollapsingAGroup_RemovesItsItemsFromItems_AndShiftsLaterIndicesDown()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        var collectionView = await GroupByCategoryAsync(tableView);

        Assert.AreEqual(6, tableView.Items.Count);

        SetGroupExpanded(collectionView, "A", isExpanded: false); // Hides rows 0-1.

        Assert.AreEqual(4, tableView.Items.Count);
        Assert.IsFalse(tableView.Items.Cast<SelectionTestItem>().Any(i => i.Category == "A"));
        // "B"'s first item, previously at index 2, is now at index 0 - Items has no gaps.
        Assert.AreEqual("B", ((SelectionTestItem)tableView.Items[0]!).Category);
    }

    [UITestMethod]
    public async Task CurrentCellSlot_Grouped_IsCleared_WhenItsOwnGroupCollapses()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        var collectionView = await GroupByCategoryAsync(tableView);

        tableView.CurrentCellSlot = new TableViewCellSlot(2, 1); // "B"'s first item.

        SetGroupExpanded(collectionView, "B", isExpanded: false); // Hides the item the current cell points at.

        Assert.IsNull(tableView.CurrentCellSlot);
    }

    [UITestMethod]
    public async Task CellSelection_Ungrouped_SelectsExpectedCells()
    {
        var tableView = await CreateTableViewAsync(CreateItems());

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(1, 0), new TableViewCellSlot(2, 1)));
        await Task.Yield(); // Allow selection to propagate.

        Assert.AreEqual(4, tableView.SelectedCells.Count);
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(1, 0)));
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(2, 1)));
    }

    [UITestMethod]
    public async Task CellSelection_Grouped_SelectsExpectedCells()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        await GroupByCategoryAsync(tableView);

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(2, 0), new TableViewCellSlot(3, 1)));
        await Task.Yield();

        Assert.AreEqual(4, tableView.SelectedCells.Count);
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(2, 0)));
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(3, 1)));
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_Ungrouped_ReturnsTheRealizedRow()
    {
        var tableView = await CreateTableViewAsync(CreateItems());

        var row = await tableView.ScrollRowIntoView(3);

        Assert.IsNotNull(row);
        Assert.AreSame(tableView.Items[3], row!.Content);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_Grouped_ReturnsTheRealizedRow()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        await GroupByCategoryAsync(tableView);

        var row = await tableView.ScrollRowIntoView(3);

        Assert.IsNotNull(row);
        Assert.AreSame(tableView.Items[3], row!.Content);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_Grouped_AfterACollapse_ReturnsTheShiftedRow()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        var collectionView = await GroupByCategoryAsync(tableView);

        SetGroupExpanded(collectionView, "A", isExpanded: false); // "B" shifts down to index 0.

        var row = await tableView.ScrollRowIntoView(0);

        Assert.IsNotNull(row);
        Assert.AreSame(tableView.Items[0], row!.Content);
        Assert.AreEqual("B", ((SelectionTestItem)row.Content!).Category);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_NegativeIndex_ReturnsNull()
    {
        var tableView = await CreateTableViewAsync(CreateItems());

        var row = await tableView.ScrollRowIntoView(-1);

        Assert.IsNull(row);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_OverlappingCalls_DoNotCrash()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        await GroupByCategoryAsync(tableView);

        // Two calls started without awaiting the first in between (e.g. two SelectionChanged events firing
        // in quick succession) used to crash the whole process - an unhandled native exception, not a
        // catchable one - rather than merely misbehave.
        var first = tableView.ScrollRowIntoView(2);
        var second = tableView.ScrollRowIntoView(4);

        await Task.WhenAll(first, second);

        Assert.IsTrue(true, "Did not crash.");
    }

    [UITestMethod]
    public async Task GetCellFromSlot_Grouped_ReturnsTheCorrectCellAfterACollapse()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        var collectionView = await GroupByCategoryAsync(tableView);

        SetGroupExpanded(collectionView, "A", isExpanded: false); // "B" shifts down to index 0.

        await tableView.ScrollRowIntoView(0);
        var cell = tableView.GetCellFromSlot(new TableViewCellSlot(0, 0));

        Assert.IsNotNull(cell);
        Assert.AreSame(tableView.Items[0], cell!.Row?.Content);
        Assert.AreEqual("B", ((SelectionTestItem)cell.Row!.Content!).Category);
    }

    [UITestMethod]
    public async Task RowIndex_MatchesItsPositionInItems_AfterACollapseShiftsIt()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        var collectionView = await GroupByCategoryAsync(tableView);

        SetGroupExpanded(collectionView, "A", isExpanded: false); // "B" shifts down to index 0.

        var row = await tableView.ScrollRowIntoView(0);

        Assert.IsNotNull(row);
        Assert.AreEqual(0, row!.Index);
        Assert.AreSame(tableView.Items[0], row.Content);
    }

    private static async Task<TableView> CreateTableViewAsync(SelectionTestItem[] items, ListViewSelectionMode selectionMode = ListViewSelectionMode.Extended)
    {
        var tableView = new TableView { SelectionMode = selectionMode };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Category",
            Binding = new Binding { Path = new PropertyPath(nameof(SelectionTestItem.Category)) }
        });
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Value",
            Binding = new Binding { Path = new PropertyPath(nameof(SelectionTestItem.Value)) }
        });

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    /// <summary>
    /// 3 categories ("A", "B", "C"), 2 items each, laid out contiguously (rows 0-1 = A, 2-3 = B, 4-5 = C).
    /// </summary>
    private static SelectionTestItem[] CreateItems()
    {
        var categories = new[] { "A", "B", "C" };

        return Enumerable.Range(0, 6)
            .Select(i => new SelectionTestItem { Id = i, Category = categories[i / 2], Value = i })
            .ToArray();
    }

    /// <summary>
    /// Adding a <see cref="GroupDescription"/> to an already-loaded <see cref="TableView"/> makes it
    /// asynchronously unbind and rebind the base <c>ItemsSource</c>, so callers must await this before
    /// reading <see cref="TableView.Items"/> - otherwise it's transiently empty.
    /// </summary>
    private static async Task<CollectionView> GroupByCategoryAsync(TableView tableView, TableViewGroupState defaultGroupState = TableViewGroupState.Expanded)
    {
        var collectionView = (CollectionView)tableView.CollectionView;
        collectionView.DefaultGroupState = defaultGroupState;
        collectionView.GroupDescriptions.Add(new GroupDescription(nameof(SelectionTestItem.Category)));

        await Task.Yield();

        return collectionView;
    }

    private static void SetGroupExpanded(CollectionView collectionView, object key, bool isExpanded)
    {
        foreach (var groupObject in collectionView.CollectionGroups!)
        {
            var group = (CollectionViewGroup)groupObject;
            var info = (TableViewGroupInfo)group.Group!;
            if (Equals(info.Key, key))
            {
                info.IsExpanded = isExpanded;
                return;
            }
        }

        Assert.Fail($"No group found for key '{key}'.");
    }

    private sealed class SelectionTestItem
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
