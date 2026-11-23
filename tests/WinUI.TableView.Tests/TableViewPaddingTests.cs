using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// A TableView's horizontal padding insets its rows, so it must inset the header row as well.
/// </summary>
[TestClass]
public class TableViewPaddingTests
{
    private TableView? _tableView;

    [TestCleanup]
    public async Task CleanupAsync()
    {
        if (_tableView is not null)
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(_tableView);
        }
    }

    [UITestMethod]
    public async Task Padding_HeadersLineUpWithCells()
    {
        var tableView = await CreateTableViewAsync(new Thickness(12, 0, 20, 0));

        AssertHeadersLineUpWithCells(tableView);
    }

    [UITestMethod]
    public async Task Padding_SelectAllCheckBoxLinesUpWithTheRows()
    {
        var tableView = await CreateTableViewAsync(new Thickness(12, 0, 20, 0), ListViewSelectionMode.Multiple);
        var headerRow = tableView.FindDescendant<TableViewHeaderRow>()!;
        var row = (TableViewRow)tableView.ContainerFromIndex(0);

        Assert.AreEqual(GetLeft(row, tableView), GetLeft(headerRow, tableView), 1d,
            "The header row must start where the rows start");
        AssertHeadersLineUpWithCells(tableView);
    }

    [UITestMethod]
    public async Task Padding_ChangedAfterLoad_HeadersLineUpWithCells()
    {
        var tableView = await CreateTableViewAsync(default);

        tableView.Padding = new Thickness(16, 0, 8, 0);
        await WaitForRenderedFramesAsync(10);
        AssertHeadersLineUpWithCells(tableView);

        tableView.Padding = default;
        await WaitForRenderedFramesAsync(10);
        AssertHeadersLineUpWithCells(tableView);
        Assert.AreEqual(0d, tableView.FindDescendant<TableViewHeaderRow>()!.Margin.Left, "Removing the padding removes the header inset");
    }

    [UITestMethod]
    public async Task Padding_WithStarColumns_DoesNotOverflow()
    {
        var tableView = await CreateTableViewAsync(new Thickness(12, 0, 20, 0), starColumns: true);
        var scrollViewer = tableView.FindDescendant<ScrollViewer>()!;

        Assert.AreEqual(0d, scrollViewer.ScrollableWidth, 0.5, "Star columns must not overflow a padded table");
        AssertHeadersLineUpWithCells(tableView);
    }

    private static void AssertHeadersLineUpWithCells(TableView tableView)
    {
        var row = tableView.ContainerFromIndex(0) as TableViewRow;
        Assert.IsNotNull(row, "Precondition: a row is realized");

        foreach (var cell in row!.Cells)
        {
            var header = cell.Column!.HeaderControl!;

            Assert.AreEqual(GetLeft(cell, tableView), GetLeft(header, tableView), 1d,
                $"Header '{cell.Column.Header}' must start where its cells start");
            Assert.AreEqual(cell.ActualWidth, header.ActualWidth, 1d,
                $"Header '{cell.Column.Header}' must be as wide as its cells");
        }
    }

    private static double GetLeft(FrameworkElement element, TableView tableView)
    {
        return element.TransformToVisual(tableView).TransformPoint(default).X;
    }

    private async Task<TableView> CreateTableViewAsync(
        Thickness padding,
        ListViewSelectionMode selectionMode = ListViewSelectionMode.Extended,
        bool starColumns = false)
    {
        var tableView = new TableView
        {
            Width = 500,
            Height = 300,
            Padding = padding,
            AutoGenerateColumns = false,
            SelectionMode = selectionMode,
            ItemsSource = Enumerable.Range(0, 5).Select(i => new Item { Name = $"Item {i}" }).ToList()
        };

        foreach (var header in new[] { "A", "B", "C" })
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = header,
                Width = starColumns && header != "A" ? new GridLength(1, GridUnitType.Star) : new GridLength(100),
                Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) }
            });
        }

        _tableView = tableView;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForRenderedFramesAsync(10);

        return tableView;
    }

    private static async Task WaitForRenderedFramesAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var taskCompletionSource = new TaskCompletionSource<object?>();

            void Callback(object? sender, object args)
            {
                CompositionTarget.Rendering -= Callback;
                taskCompletionSource.SetResult(null);
            }

            CompositionTarget.Rendering += Callback;
            await taskCompletionSource.Task;
        }
    }

    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
    }
}
