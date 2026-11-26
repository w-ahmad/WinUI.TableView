using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;

namespace WinUI.TableView.Tests;

/// <summary>
/// Covers the text content the table returns for copy and export.
/// </summary>
[TestClass]
public class TableViewContentTests
{
    [UITestMethod]
    public void All_Content_Has_Every_Row_In_Order()
    {
        var tableView = CreateTableView(3);

        Assert.AreEqual("Item0\t0\nItem1\t10\nItem2\t20", tableView.GetAllContent(false));
    }

    [UITestMethod]
    public void All_Content_Can_Include_Headers_And_Use_A_Separator()
    {
        var tableView = CreateTableView(2);

        Assert.AreEqual("Name,Value\nItem0,0\nItem1,10", tableView.GetAllContent(true, ','));
    }

    [UITestMethod]
    public void All_Content_Of_An_Empty_Table_Is_Empty()
    {
        var tableView = CreateTableView(0);

        Assert.AreEqual(string.Empty, tableView.GetAllContent(true));
    }

    [UITestMethod]
    public void Rows_Content_Is_Ordered_By_Row_Without_Repeats()
    {
        var tableView = CreateTableView(5);

        Assert.AreEqual("Item1\t10\nItem3\t30", tableView.GetRowsContent([3, 1, 3], false));
    }

    [UITestMethod]
    public void Cells_Content_Leaves_Unlisted_Cells_Empty()
    {
        var tableView = CreateTableView(3);
        TableViewCellSlot[] slots = [new(0, 0), new(2, 1)];

        Assert.AreEqual("Name\tValue\nItem0\t\n\t20", tableView.GetCellsContent(slots, true));
    }

    [UITestMethod]
    public void Cells_Content_Spans_Only_The_Listed_Columns()
    {
        var tableView = CreateTableView(3);
        TableViewCellSlot[] slots = [new(1, 1), new(2, 1)];

        Assert.AreEqual("Value\n10\n20", tableView.GetCellsContent(slots, true));
    }

    [UITestMethod]
    public void All_Content_Of_Many_Rows_Is_Complete()
    {
        var tableView = CreateTableView(20_000);

        var lines = tableView.GetAllContent(false).Split('\n');

        Assert.AreEqual(20_000, lines.Length);
        Assert.AreEqual("Item19999\t199990", lines[^1]);
    }

    private static TableView CreateTableView(int count)
    {
        var tableView = new TableView { AutoGenerateColumns = false };

        tableView.Columns.Add(new TableViewTextColumn { Header = "Name", Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) } });
        tableView.Columns.Add(new TableViewTextColumn { Header = "Value", Binding = new Binding { Path = new PropertyPath(nameof(Item.Value)) } });
        tableView.ItemsSource = Enumerable.Range(0, count).Select(i => new Item { Name = $"Item{i}", Value = i * 10 }).ToList();

        return tableView;
    }

    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }
}
