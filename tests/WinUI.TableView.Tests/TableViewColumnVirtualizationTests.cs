using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewColumnVirtualizationTests
{
    private const int ColumnCount = 200;
    private const double ColumnWidth = 100d;
    private const double TableWidth = 900d;

    // The viewport and the lead kept on both sides of it, in columns, with room for the edges.
    private const int MaxRealizedColumns = (int)((TableWidth + 400d) / ColumnWidth) + 2;

    [UITestMethod]
    public async Task WideTable_HasCellsOnlyForColumnsNearTheViewport()
    {
        var tableView = await CreateTableViewAsync();

        foreach (var row in Rows(tableView))
        {
            Assert.IsTrue(row.Cells.Count >= 9, $"{row.Cells.Count} cells do not cover the viewport");
            Assert.IsTrue(row.Cells.Count <= MaxRealizedColumns, $"{row.Cells.Count} cells for a {TableWidth}px viewport");
            Assert.IsNotNull(row.GetCell(0));
            Assert.IsNull(row.GetCell(ColumnCount - 1));
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollingSideways_MovesTheCellsToTheColumnsInView()
    {
        var tableView = await CreateTableViewAsync();

        await tableView.ScrollCellIntoView(new TableViewCellSlot(0, 120));
        tableView.UpdateLayout();

        var columns = tableView.Columns.VisibleColumns;

        foreach (var row in Rows(tableView))
        {
            Assert.IsTrue(row.Cells.Count <= MaxRealizedColumns, $"{row.Cells.Count} cells after scrolling");
            Assert.IsNull(row.GetCell(0), "the first column is far out of view");

            var cell = row.GetCell(120);
            Assert.IsNotNull(cell, "the column scrolled to has no cell");
            Assert.AreEqual(120, cell.Index);
            Assert.AreEqual(120 * ColumnWidth, cell.ActualOffset.X, 0.01);

            foreach (var realized in row.Cells)
            {
                // A reused cell shows the value of the column it has now.
                var index = columns.IndexOf(realized.Column!);
                Assert.AreEqual(index, realized.Index);

                // Uno leaves the content of a cell outside the viewport unbound until it is nearly in view.
                if (index is >= 113 and <= 120)
                {
                    var expected = index % 2 == 0 ? ((HostItem)row.Content).Name : ((HostItem)row.Content).Category;
                    Assert.AreEqual(expected, (realized.Content as TextBlock)?.Text, $"column {index}");
                }
            }
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task FrozenColumns_KeepTheirCells_WhenScrolledSideways()
    {
        var tableView = await CreateTableViewAsync(frozenColumnCount: 2);

        await tableView.ScrollCellIntoView(new TableViewCellSlot(0, 150));
        tableView.UpdateLayout();

        foreach (var row in Rows(tableView))
        {
            Assert.IsNotNull(row.GetCell(0));
            Assert.IsNotNull(row.GetCell(1));
            Assert.IsNull(row.GetCell(2));
            Assert.IsNotNull(row.GetCell(150));
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CurrentCell_KeepsItsCell_WhenScrolledOutOfView()
    {
        var tableView = await CreateTableViewAsync();
        tableView.SelectionUnit = TableViewSelectionUnit.Cell;
        tableView.CurrentCellSlot = new TableViewCellSlot(0, 3);
        await Task.Delay(100);

        await tableView.ScrollCellIntoView(new TableViewCellSlot(1, 150));
        tableView.UpdateLayout();

        var rows = Rows(tableView).OrderBy(x => x.Index).ToList();

        Assert.IsNotNull(rows[0].GetCell(3), "the current cell lost its cell");
        Assert.IsNull(rows[1].GetCell(3));

        tableView.CurrentCellSlot = null;
        await Task.Delay(100);
        tableView.UpdateLayout();

        Assert.IsNull(rows[0].GetCell(3), "the cell is no longer current and its column is out of view");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ColumnChanges_KeepCellsInLineWithTheColumnsInView()
    {
        var tableView = await CreateTableViewAsync();
        var row = Rows(tableView).First();
        var hidden = (TableViewColumn)tableView.Columns[2];

        hidden.Visibility = Visibility.Collapsed;
        tableView.Columns.Insert(0, new TableViewTextColumn { Header = "New", Width = new GridLength(ColumnWidth) });
        tableView.Columns.RemoveAt(5);
        tableView.UpdateLayout();

        var columns = tableView.Columns.VisibleColumns;

        Assert.IsNull(row.GetCell(hidden));
        Assert.IsTrue(row.Cells.Count <= MaxRealizedColumns);

        for (var i = 0; i < row.Cells.Count; i++)
        {
            Assert.AreSame(columns[i], row.Cells[i].Column, $"cell {i}");
            Assert.AreEqual(i * ColumnWidth, row.Cells[i].ActualOffset.X, 0.01, $"cell {i}");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static List<TableViewRow> Rows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(x => x.IsLoaded)];
    }

    private static async Task<TableView> CreateTableViewAsync(int frozenColumnCount = 0)
    {
        var tableView = new TableView
        {
            Width = TableWidth,
            Height = 300,
            AutoGenerateColumns = false,
            FrozenColumnCount = frozenColumnCount
        };

        for (var i = 0; i < ColumnCount; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(ColumnWidth),
                Binding = new Binding { Path = new PropertyPath(i % 2 == 0 ? nameof(HostItem.Name) : nameof(HostItem.Category)) }
            });
        }

        var items = new List<HostItem>();

        for (var i = 0; i < 50; i++)
        {
            items.Add(new HostItem { Id = i, Name = $"Item {i}", Category = $"Category {i}" });
        }

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.UpdateLayout();

        return tableView;
    }
}
