using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewRowHeightTests
{
    [UITestMethod]
    public async Task NewRows_UseTheTableRowHeights()
    {
        var tableView = await CreateTableViewAsync(tableView =>
        {
            tableView.RowHeight = 50d;
            tableView.RowMinHeight = 20d;
            tableView.RowMaxHeight = 80d;
        });

        AssertRowHeights(tableView, 50d, 20d, 80d);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ChangingRowHeights_ReachesRealizedAndRecycledRows()
    {
        var tableView = await CreateTableViewAsync();

        AssertRowHeights(tableView, tableView.RowHeight, tableView.RowMinHeight, tableView.RowMaxHeight);

        // Leaves rows in the pool, which have to follow the change as well.
        await ScrollToAsync(tableView, 4_000d);
        tableView.Height = 200d;
        await WaitForLayoutAsync(tableView);

        tableView.RowMinHeight = 24d;
        tableView.RowMaxHeight = 90d;
        tableView.RowHeight = 60d;
        await WaitForLayoutAsync(tableView);

        AssertRowHeights(tableView, 60d, 24d, 90d);
        Assert.IsTrue(Rows(tableView).All(row => row.ActualHeight >= 60d));

        tableView.Height = 400d;
        await ScrollToAsync(tableView, 0d);

        AssertRowHeights(tableView, 60d, 24d, 90d);

        tableView.RowHeight = double.NaN;
        await WaitForLayoutAsync(tableView);

        AssertRowHeights(tableView, double.NaN, 24d, 90d);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static void AssertRowHeights(TableView tableView, double height, double minHeight, double maxHeight)
    {
        var rows = Rows(tableView);
        Assert.IsTrue(rows.Count > 1);

        foreach (var row in rows)
        {
            var elements = row.Cells.Cast<FrameworkElement>()
                              .Append(row.FindDescendants().OfType<TableViewRowHeader>().Single())
                              .Append(row.FindDescendants().OfType<FrameworkElement>().Single(x => x.Name is "HeaderGrid"))
                              .ToList();

            Assert.AreEqual(tableView.Columns.Count + 2, elements.Count);

            foreach (var element in elements)
            {
                Assert.AreEqual(height, element.Height, element.GetType().Name);
                Assert.AreEqual(minHeight, element.MinHeight, element.GetType().Name);
                Assert.AreEqual(maxHeight, element.MaxHeight, element.GetType().Name);
            }
        }
    }

    private static List<TableViewRow> Rows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(row => row.Index >= 0)];
    }

    private static async Task<TableView> CreateTableViewAsync(System.Action<TableView>? configure = null)
    {
        var tableView = new TableView { Width = 400, Height = 400, AutoGenerateColumns = false };

        for (var i = 0; i < 3; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(100),
                Binding = new Binding { Path = new PropertyPath(nameof(RowHeightItem.Name)) }
            });
        }

        configure?.Invoke(tableView);
        tableView.ItemsSource = Enumerable.Range(0, 500).Select(i => new RowHeightItem { Name = $"Item {i}" }).ToList();

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }

    private sealed class RowHeightItem
    {
        public string? Name { get; set; }
    }
}
