using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewResizeScrollTests
{
    [UITestMethod]
    public async Task ResizingColumn_DoesNotScrollHorizontally_WhenItPushesTheCurrentCellOutOfView()
    {
        var tableView = await CreateTableViewAsync(columnCount: 3, columnWidth: 100);
        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(0, 2);
            await Task.Delay(300);

            Assert.AreEqual(0d, tableView.HorizontalOffset, 0.01, "Precondition: the current cell is fully in view, so nothing scrolled");

            tableView.Columns[0].Width = new GridLength(300, GridUnitType.Pixel);
            await Task.Delay(500);

            var scrollViewer = tableView.FindDescendant<ScrollViewer>();
            Assert.IsNotNull(scrollViewer);
            Assert.IsTrue(scrollViewer!.ScrollableWidth > 0, "Precondition: the resized content is wider than the viewport");

            Assert.AreEqual(0d, tableView.HorizontalOffset, 0.01,
                "Resizing a column must not scroll the table to keep the current cell in view");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task ResizingColumn_PreservesHorizontalOffset_WhenTheCurrentCellIsScrolledOutOfView()
    {
        var tableView = await CreateTableViewAsync(columnCount: 5, columnWidth: 120);
        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(0, 0);
            await Task.Delay(300);

            const double offset = 100d;
            tableView.SetValue(TableView.HorizontalOffsetProperty, offset);
            await Task.Delay(300);

            Assert.AreEqual(offset, tableView.HorizontalOffset, 0.01, "Precondition: the table is scrolled away from the current cell");

            tableView.Columns[2].Width = new GridLength(200, GridUnitType.Pixel);
            await Task.Delay(500);

            Assert.AreEqual(offset, tableView.HorizontalOffset, 0.01,
                "Resizing a column must not snap the scroll position back to the current cell");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    private static async Task<TableView> CreateTableViewAsync(int columnCount, double columnWidth)
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false
        };

        for (var i = 0; i < columnCount; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(columnWidth, GridUnitType.Pixel),
                Binding = new Binding { Path = new PropertyPath(nameof(ResizeScrollTestItem.Name)) }
            });
        }

        tableView.ItemsSource = new[]
        {
            new ResizeScrollTestItem { Name = "Alpha" },
            new ResizeScrollTestItem { Name = "Beta" },
            new ResizeScrollTestItem { Name = "Gamma" }
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class ResizeScrollTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
