using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

/// <summary>
/// Shift+Click range selection (issue #470): the range must run from the most recently clicked
/// row or cell to the Shift+Clicked one.
/// </summary>
[TestClass]
public class TableViewShiftClickSelectionTests
{
    [UITestMethod]
    public async Task ShiftClick_Row_ExtendsFromTheMostRecentlyClickedRow()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 2);
        Click(tableView, 5);
        ShiftClick(tableView, 7);
        CollectionAssert.AreEqual(new[] { 5, 6, 7 }, SelectedRows(tableView));

        Click(tableView, 9);
        ShiftClick(tableView, 10);
        CollectionAssert.AreEqual(new[] { 9, 10 }, SelectedRows(tableView));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ShiftClick_Row_OnTheOtherSideOfTheAnchor_ReplacesThePreviousRange()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 5);
        ShiftClick(tableView, 8);
        ShiftClick(tableView, 2);
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, SelectedRows(tableView));

        ShiftClick(tableView, 10);
        CollectionAssert.AreEqual(new[] { 5, 6, 7, 8, 9, 10 }, SelectedRows(tableView));

        ShiftClick(tableView, 6);
        CollectionAssert.AreEqual(new[] { 5, 6 }, SelectedRows(tableView));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CtrlShiftClick_Row_AddsTheRangeToTheExistingSelection()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 1);
        // A Ctrl+Click adds the row to the selection and makes it the anchor.
        tableView.SelectedItems.Add(tableView.Items[4]);
        tableView.SelectionStartRowIndex = 4;
        ShiftClick(tableView, 6, ctrlKey: true);

        CollectionAssert.AreEqual(new[] { 1, 4, 5, 6 }, SelectedRows(tableView));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CtrlShiftClick_Row_InsideTheSelectedRange_KeepsTheSelection()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 5);
        ShiftClick(tableView, 8);
        ShiftClick(tableView, 6, ctrlKey: true);

        CollectionAssert.AreEqual(new[] { 5, 6, 7, 8 }, SelectedRows(tableView));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ShiftClick_Row_MakesTheClickedRowCurrentAndKeepsTheAnchor()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 5);
        ShiftClick(tableView, 8);

        Assert.AreEqual(8, tableView.CurrentRowIndex);
        Assert.AreEqual(5, tableView.SelectionStartRowIndex);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ShiftClick_Cell_ExtendsFromTheMostRecentlyClickedCell()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Cell);

        Click(tableView, 2, 0);
        // A finished pointer click leaves the clicked cell current, with no cell anchor.
        Click(tableView, 5, 0);
        tableView.SelectionStartCellSlot = null;

        ShiftClick(tableView, 7, 1);

        Assert.AreEqual(0, tableView.SelectedItems.Count);
        CollectionAssert.AreEquivalent(
            Slots(5, 0, 7, 1),
            tableView.SelectedCells.ToArray());

        ShiftClick(tableView, 3, 0);

        CollectionAssert.AreEquivalent(
            Slots(3, 0, 5, 0),
            tableView.SelectedCells.ToArray());

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ShiftClick_CellWithRow_ExtendsCellsAndTheirRows()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.CellWithRow);

        Click(tableView, 9, 0);
        tableView.SelectionStartCellSlot = null;

        ShiftClick(tableView, 10, 1);

        CollectionAssert.AreEqual(new[] { 9, 10 }, SelectedRows(tableView));
        CollectionAssert.AreEquivalent(
            Slots(9, 0, 10, 1),
            tableView.SelectedCells.ToArray());

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ShiftClick_OnEmptySpace_IsNotHandled()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 3);

        Assert.IsFalse(tableView.ExtendSelectionTo(tableView, ctrlKey: false));
        CollectionAssert.AreEqual(new[] { 3 }, SelectedRows(tableView));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Click_Row_RaisesSelectionChangedOnce()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 2);
        ShiftClick(tableView, 6);

        var raised = 0;
        tableView.SelectionChanged += (_, _) => raised++;

        Click(tableView, 9);
        Assert.AreEqual(1, raised, "A click that replaces the selection");

        ShiftClick(tableView, 4);
        Assert.AreEqual(2, raised, "A Shift+Click that extends the selection");

        ShiftClick(tableView, 11);
        Assert.AreEqual(3, raised, "A Shift+Click on the other side of the anchor");
        CollectionAssert.AreEqual(new[] { 9, 10, 11 }, SelectedRows(tableView));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Click_Row_ReportsTheWholeChangeInOneEvent()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Row);

        Click(tableView, 2);
        ShiftClick(tableView, 4);

        SelectionChangedEventArgs? args = null;
        tableView.SelectionChanged += (_, e) => args = e;

        Click(tableView, 9);

        Assert.IsNotNull(args);
        Assert.AreEqual(3, args.RemovedItems.Count);
        Assert.AreEqual(1, args.AddedItems.Count);
        Assert.AreSame(tableView.Items[9], args.AddedItems[0]);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Click_Cell_RaisesCellSelectionChangedOnce()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.CellOrRow);

        Click(tableView, 1, 0);

        var raised = 0;
        tableView.CellSelectionChanged += (_, _) => raised++;

        Click(tableView, 3, 1);
        Assert.AreEqual(1, raised, "A click that replaces the selection");

        ShiftClick(tableView, 5, 1);
        Assert.AreEqual(2, raised, "A Shift+Click that extends the selection");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    /// <summary>
    /// Selects a row or cell the way a plain click does: it becomes the only selection and the anchor.
    /// </summary>
    private static void Click(TableView tableView, int row, int column = -1)
    {
        tableView.MakeSelection(new TableViewCellSlot(row, column), shiftKey: false);
    }

    private static void ShiftClick(TableView tableView, int row, int column = -1, bool ctrlKey = false)
    {
        UIElement pressedElement = column < 0
            ? (TableViewRow)tableView.ContainerFromIndex(row)
            : tableView.GetCellFromSlot(new TableViewCellSlot(row, column))!;

        Assert.IsTrue(tableView.ExtendSelectionTo(pressedElement, ctrlKey));
    }

    private static int[] SelectedRows(TableView tableView)
    {
        // Not SelectedRanges: Uno does not implement it on ListViewBase.
        return [.. tableView.SelectedItems.Select(item => tableView.Items.IndexOf(item)).Order()];
    }

    private static TableViewCellSlot[] Slots(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        return [.. from row in Enumerable.Range(firstRow, lastRow - firstRow + 1)
                   from column in Enumerable.Range(firstColumn, lastColumn - firstColumn + 1)
                   select new TableViewCellSlot(row, column)];
    }

    private static async Task<TableView> CreateTableViewAsync(TableViewSelectionUnit selectionUnit)
    {
        var tableView = new TableView
        {
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended,
            SelectionUnit = selectionUnit,
            IsReadOnly = true,
            ItemsSource = Enumerable.Range(0, 12).Select(i => new Item { Name = "Tag" + i, Value = i }).ToList()
        };

        tableView.Columns.Add(new TableViewTextColumn { Header = "Name", Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) } });
        tableView.Columns.Add(new TableViewTextColumn { Header = "Value", Binding = new Binding { Path = new PropertyPath(nameof(Item.Value)) } });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
