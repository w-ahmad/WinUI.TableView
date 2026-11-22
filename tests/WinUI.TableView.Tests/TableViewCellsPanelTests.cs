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
public class TableViewCellsPanelTests
{
    [UITestMethod]
    public async Task ScrollableCells_AreHostedInTheCellsPanel_AtTheirColumnOffsets()
    {
        var tableView = await CreateTableViewAsync(columnCount: 6, frozenColumnCount: 2);
        var row = FirstRow(tableView);
        var panel = row.FindDescendants().OfType<TableViewCellsPanel>().Single();

        Assert.AreEqual(4, panel.Children.Count);
        Assert.AreEqual(400d, panel.DesiredSize.Width);
        AssertCellsFollowTheirColumns(tableView, row, frozenColumnCount: 2);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollableCells_FollowWidthVisibilityAndOrderChanges()
    {
        var tableView = await CreateTableViewAsync(columnCount: 6, frozenColumnCount: 1);
        var row = FirstRow(tableView);
        var panel = row.FindDescendants().OfType<TableViewCellsPanel>().Single();

        ((TableViewColumn)tableView.Columns[1]).Width = new GridLength(170);
        tableView.UpdateLayout();
        Assert.AreEqual(570d, panel.DesiredSize.Width);
        AssertCellsFollowTheirColumns(tableView, row, frozenColumnCount: 1);

        ((TableViewColumn)tableView.Columns[2]).Visibility = Visibility.Collapsed;
        tableView.UpdateLayout();
        Assert.AreEqual(470d, panel.DesiredSize.Width);
        AssertCellsFollowTheirColumns(tableView, row, frozenColumnCount: 1);

        tableView.Columns.Move(5, 3);
        tableView.UpdateLayout();
        AssertCellsFollowTheirColumns(tableView, row, frozenColumnCount: 1);

        tableView.FrozenColumnCount = 3;
        tableView.UpdateLayout();
        AssertCellsFollowTheirColumns(tableView, row, frozenColumnCount: 3);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static void AssertCellsFollowTheirColumns(TableView tableView, TableViewRow row, int frozenColumnCount)
    {
        var columns = tableView.Columns.VisibleColumns;
        var expected = 0d;

        for (var i = frozenColumnCount; i < columns.Count; i++)
        {
            var cell = row.GetCell(columns[i]);

            Assert.IsNotNull(cell, $"no cell for column {i}");
            Assert.AreEqual(expected, cell.ActualOffset.X, 0.01, $"cell of column {i}");
            Assert.AreEqual(columns[i].ActualWidth, cell.ActualWidth, 0.01, $"width of column {i}");
            expected += columns[i].ActualWidth;
        }
    }

    private static TableViewRow FirstRow(TableView tableView)
    {
        return tableView.FindDescendants().OfType<TableViewRow>().First(x => x.IsLoaded);
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
