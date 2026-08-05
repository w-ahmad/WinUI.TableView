using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewSelectionUnitTests
{
    [UITestMethod]
    public async Task CellWithRow_CellClickSelectsCellAndOwningRow()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.CellWithRow);

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(1, 0), new TableViewCellSlot(1, 0)));

        await Task.Yield(); // Allow selection to propagate

        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(1, 0)));
        Assert.AreEqual(1, tableView.SelectedItems.Count);
        Assert.AreSame(tableView.Items[1], tableView.SelectedItem);
    }

    [UITestMethod]
    public async Task CellAndRow_RowHeaderClickSelectsOnlyRow()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.CellWithRow);

        tableView.SelectRange(new ItemIndexRange(1, 1));

        Assert.AreEqual(0, tableView.SelectedCells.Count);
        Assert.AreEqual(1, tableView.SelectedItems.Count);
        Assert.AreSame(tableView.Items[1], tableView.SelectedItem);
    }

    [UITestMethod]
    public async Task CellSelectionUnitStillSelectsOnlyCells()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.Cell);

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(0, 0), new TableViewCellSlot(0, 0)));

        await Task.Yield(); // Allow selection to propagate

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(1, 1), new TableViewCellSlot(1, 1)));

        await Task.Yield(); // Allow selection to propagate

        Assert.AreEqual(0, tableView.SelectedItems.Count);
        Assert.AreEqual(2, tableView.SelectedCells.Count);
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(0, 0)));
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(1, 1)));
    }

    [UITestMethod]
    public async Task CellOrRowSelectionUnitStillUsesCellAndRowSemantics()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.CellOrRow);

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(1, 0), new TableViewCellSlot(1, 0)));
        Assert.AreEqual(0, tableView.SelectedItems.Count);

        tableView.SelectRange(new ItemIndexRange(0, 1));
        Assert.AreEqual(1, tableView.SelectedItems.Count);
        Assert.AreEqual(0, tableView.SelectedCells.Count);
    }

    [UITestMethod]
    public async Task CellWithRow_MultiSelectionAddsCellAndRowSelections()
    {
        var tableView = await CreateTableViewAsync(TableViewSelectionUnit.CellWithRow, ListViewSelectionMode.Multiple);

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(0, 0), new TableViewCellSlot(0, 0)));

        await Task.Yield(); // Allow selection to propagate

        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(1, 1), new TableViewCellSlot(1, 1)));

        await Task.Delay(200); // Allow selection to propagate

        Assert.AreEqual(2, tableView.SelectedItems.Count);
        Assert.AreEqual(2, tableView.SelectedCells.Count);
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(0, 0)));
        Assert.IsTrue(tableView.SelectedCells.Contains(new TableViewCellSlot(1, 1)));
    }

    private static async Task<TableView> CreateTableViewAsync(TableViewSelectionUnit selectionUnit, ListViewSelectionMode selectionMode = ListViewSelectionMode.Extended)
    {
        var tableView = new TableView
        {
            SelectionMode = selectionMode,
            SelectionUnit = selectionUnit
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(SelectionItem.Name)) }
        });
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Value",
            Binding = new Binding { Path = new PropertyPath(nameof(SelectionItem.Value)) }
        });
        tableView.ItemsSource = new[]
        {
            new SelectionItem { Name = "A", Value = 1 },
            new SelectionItem { Name = "B", Value = 2 }
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class SelectionItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
