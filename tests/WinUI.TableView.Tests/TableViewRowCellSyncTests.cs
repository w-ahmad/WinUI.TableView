using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// Rows that are recycled while the table is empty must come back with the cells of the current columns.
/// </summary>
[TestClass]
public class TableViewRowCellSyncTests
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
    public async Task ColumnShownWhileTableIsEmpty_RowsGetItsCell()
    {
        var (tableView, items) = await CreateTableViewAsync();
        tableView.Columns[1].Visibility = Visibility.Collapsed;
        await WaitForRenderedFramesAsync(5);

        await RefillAsync(tableView, items, () => tableView.Columns[1].Visibility = Visibility.Visible);

        AssertCellsMatchColumns(tableView);
    }

    [UITestMethod]
    public async Task ColumnHiddenWhileTableIsEmpty_RowsDropItsCell()
    {
        var (tableView, items) = await CreateTableViewAsync();

        await RefillAsync(tableView, items, () => tableView.Columns[1].Visibility = Visibility.Collapsed);

        AssertCellsMatchColumns(tableView);
    }

    [UITestMethod]
    public async Task ColumnMovedWhileTableIsEmpty_RowsFollowTheOrder()
    {
        var (tableView, items) = await CreateTableViewAsync();

        await RefillAsync(tableView, items, () => tableView.Columns.Move(0, 2));

        AssertCellsMatchColumns(tableView);
    }

    [UITestMethod]
    public async Task ColumnResizedWhileTableIsEmpty_RowsFollowTheWidth()
    {
        var (tableView, items) = await CreateTableViewAsync();

        await RefillAsync(tableView, items, () => tableView.Columns[1].Width = new GridLength(213));

        Assert.AreEqual(213, tableView.Columns[1].ActualWidth, 0.5, "Precondition: the column was resized");
        AssertCellsMatchColumns(tableView);
    }

    [UITestMethod]
    public async Task CurrentCell_InAColumnShownWhileTableIsEmpty_DoesNotThrow()
    {
        var (tableView, items) = await CreateTableViewAsync();
        tableView.Columns[1].Visibility = Visibility.Collapsed;
        await WaitForRenderedFramesAsync(5);
        await RefillAsync(tableView, items, () => tableView.Columns[1].Visibility = Visibility.Visible);

        Exception? unhandled = null;

        void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            unhandled ??= e.Exception;
            e.Handled = true;
        }

        Application.Current.UnhandledException += OnUnhandledException;
        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(0, 2);
            await WaitForRenderedFramesAsync(5);
            await Task.Delay(100);
        }
        finally
        {
            Application.Current.UnhandledException -= OnUnhandledException;
        }

        Assert.IsNull(unhandled, $"Making the cell current threw: {unhandled}");
    }

    [UITestMethod]
    public async Task RecycledRows_WithUnchangedColumns_KeepTheirCells()
    {
        var (tableView, items) = await CreateTableViewAsync(itemCount: 300);
        tableView.FrozenColumnCount = 1;
        tableView.Columns.Move(2, 1);
        await WaitForRenderedFramesAsync(10);

        var before = GetRows(tableView).ToDictionary(row => row, row => row.Cells.ToList());

        tableView.ScrollIntoView(items[299]);
        await WaitForRenderedFramesAsync(10);
        tableView.ScrollIntoView(items[0]);
        await WaitForRenderedFramesAsync(10);

        foreach (var (row, cells) in before)
        {
            Assert.IsTrue(cells.SequenceEqual(row.Cells), "A recycled row must not rebuild its cells while the columns are unchanged");
        }

        AssertCellsMatchColumns(tableView);
    }

    private static async Task RefillAsync(TableView tableView, ObservableCollection<Item> items, Action changeColumns)
    {
        items.Clear();
        await WaitForRenderedFramesAsync(5);

        changeColumns();
        await WaitForRenderedFramesAsync(5);

        foreach (var item in CreateItems(10))
        {
            items.Add(item);
        }

        await WaitForRenderedFramesAsync(10);
    }

    private static void AssertCellsMatchColumns(TableView tableView)
    {
        var columns = tableView.Columns.VisibleColumns;
        var rows = GetRows(tableView).Where(row => row.TableView is not null).ToList();

        Assert.IsTrue(rows.Count > 0, "Precondition: rows are realized");

        foreach (var row in rows)
        {
            Assert.IsTrue(columns.SequenceEqual(row.Cells.Select(cell => cell.Column)),
                $"Row {row.Index} has {row.Cells.Count} cell(s) for {columns.Count} visible column(s), or in another order");

            foreach (var cell in row.Cells)
            {
                Assert.AreEqual(cell.Column!.ActualWidth, cell.Width, 0.5, $"Row {row.Index}: a cell is not as wide as its column");
            }
        }
    }

    private static List<TableViewRow> GetRows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>()];
    }

    private async Task<(TableView, ObservableCollection<Item>)> CreateTableViewAsync(int itemCount = 10)
    {
        var items = CreateItems(itemCount);
        var tableView = new TableView { Width = 500, Height = 300, AutoGenerateColumns = false, ItemsSource = items };

        foreach (var header in new[] { "A", "B", "C" })
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = header,
                Width = new GridLength(100),
                Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) }
            });
        }

        _tableView = tableView;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForRenderedFramesAsync(5);

        return (tableView, items);
    }

    private static ObservableCollection<Item> CreateItems(int count)
    {
        return new(Enumerable.Range(0, count).Select(i => new Item { Name = $"Item {i}" }));
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
