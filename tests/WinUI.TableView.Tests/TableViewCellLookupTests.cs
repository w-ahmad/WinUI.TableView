using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewCellLookupTests
{
    [UITestMethod]
    public async Task GetCell_ReturnsTheCellOfTheColumn_FrozenOrNot()
    {
        var tableView = await CreateTableViewAsync(columnCount: 6, frozenColumnCount: 2);
        var row = tableView.FindDescendants().OfType<TableViewRow>().First(x => x.IsLoaded);
        var columns = tableView.Columns.VisibleColumns;

        Assert.AreEqual(columns.Count, row.Cells.Count);

        for (var i = 0; i < columns.Count; i++)
        {
            Assert.AreSame(columns[i], row.GetCell(columns[i])?.Column);
            Assert.AreSame(row.Cells[i], row.GetCell(i));
        }

        Assert.IsNull(row.GetCell(-1));
        Assert.IsNull(row.GetCell(columns.Count));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GetCell_FollowsHiddenAndMovedColumns()
    {
        var tableView = await CreateTableViewAsync(columnCount: 5, frozenColumnCount: 0);
        var row = tableView.FindDescendants().OfType<TableViewRow>().First(x => x.IsLoaded);
        var hidden = (TableViewColumn)tableView.Columns[1];
        var moved = (TableViewColumn)tableView.Columns[4];

        hidden.Visibility = Visibility.Collapsed;
        tableView.Columns.Move(4, 0);
        tableView.UpdateLayout();

        Assert.IsNull(row.GetCell(hidden));
        Assert.AreSame(moved, row.GetCell(0)?.Column);

        var columns = tableView.Columns.VisibleColumns;

        for (var i = 0; i < columns.Count; i++)
        {
            Assert.AreSame(columns[i], row.GetCell(i)?.Column);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GetVisibleColumnOffset_TracksWidthsAndVisibility()
    {
        var tableView = await CreateTableViewAsync(columnCount: 4, frozenColumnCount: 0);
        var columns = (TableViewColumnsCollection)tableView.Columns;

        Assert.AreEqual(0d, columns.GetVisibleColumnOffset(0));
        Assert.AreEqual(200d, columns.GetVisibleColumnOffset(2));
        Assert.AreEqual(400d, columns.GetVisibleColumnOffset(4));

        ((TableViewColumn)columns[0]).Width = new GridLength(150);
        tableView.UpdateLayout();

        Assert.AreEqual(250d, columns.GetVisibleColumnOffset(2));

        ((TableViewColumn)columns[1]).Visibility = Visibility.Collapsed;
        tableView.UpdateLayout();

        Assert.AreEqual(250d, columns.GetVisibleColumnOffset(2));
        Assert.AreEqual(350d, columns.GetVisibleColumnOffset(3));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static async Task<TableView> CreateTableViewAsync(int columnCount, int frozenColumnCount)
    {
        var tableView = new TableView
        {
            Width = 900,
            Height = 300,
            AutoGenerateColumns = false,
            FrozenColumnCount = frozenColumnCount
        };

        for (var i = 0; i < columnCount; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(100),
                Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
            });
        }

        tableView.ItemsSource = new List<HostItem> { new() { Name = "One" }, new() { Name = "Two" } };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.UpdateLayout();

        return tableView;
    }
}
