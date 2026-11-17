using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewColumnReorderTests
{
    public TestContext TestContext { get; set; } = null!;

    [UITestMethod]
    public async Task Move_ToSameIndex_KeepsColumnPropertyNotifications()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[1];

        tableView.Columns.Move(1, 1);

        var changes = CaptureColumnPropertyChanges(tableView);
        column.ActualWidth += 40;

        Assert.Contains(nameof(TableViewColumn.ActualWidth), changes,
            "Moving a column onto its own index must not switch off ColumnPropertyChanged");
    }

    [UITestMethod]
    public async Task Move_ToSameIndex_KeepsCollectionNotifications()
    {
        var tableView = await CreateTableViewAsync();
        var headerRow = tableView.FindDescendant<TableViewHeaderRow>()!;

        tableView.Columns.Move(1, 1);

        var added = new TableViewTextColumn
        {
            Header = "Added",
            Binding = new Binding { Path = new PropertyPath(nameof(ReorderTestItem.Name)) }
        };
        tableView.Columns.Add(added);

        Assert.AreSame(tableView, added.TableView,
            "A column added after a same-index move must still be attached to the table");
        Assert.Contains(h => h.Column == added, headerRow.Headers,
            "A column added after a same-index move must still get a header");
    }

    [UITestMethod]
    public async Task Move_WithInvalidIndex_KeepsColumnPropertyNotifications()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tableView.Columns.Move(0, 99));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tableView.Columns.Move(-1, 0));

        var changes = CaptureColumnPropertyChanges(tableView);
        column.ActualWidth += 40;

        Assert.Contains(nameof(TableViewColumn.ActualWidth), changes,
            "A rejected move must not switch off ColumnPropertyChanged");
    }

    [UITestMethod]
    public async Task Move_ToDifferentIndex_KeepsHeaderControl()
    {
        var tableView = await CreateTableViewAsync();
        var headerRow = tableView.FindDescendant<TableViewHeaderRow>()!;
        var column = tableView.Columns[0];
        var header = column.HeaderControl;

        tableView.Columns.Move(0, 2);

        Assert.AreSame(column, tableView.Columns[2], "Precondition: the column moved");
        Assert.IsNotNull(header, "Precondition: the column had a header");
        Assert.AreSame(header, column.HeaderControl,
            "A moved column must keep pointing at its header");
        Assert.AreSame(header, headerRow.Headers[2],
            "The moved column's header must sit at the column's new position");
    }

#if WINDOWS
    [UITestMethod]
    public async Task DropOnOriginalPosition_ThenResize_CellsFollowHeaderWidth()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[1];

        var reorderEvents = 0;
        tableView.ColumnReordering += (_, _) => reorderEvents++;
        tableView.ColumnReordered += (_, _) => reorderEvents++;

        DragHeaderOnto(tableView, column, column, offsetFromCenter: 6);

        Assert.AreSame(column, tableView.Columns[1], "Precondition: the drop left the column where it was");
        Assert.AreEqual(0, reorderEvents, "A drop that moves nothing must not raise the reorder events");

        var newWidth = column.ActualWidth + 60;
        column.HeaderControl!.Width = newWidth;

        AssertCellsMatchWidth(tableView, column, newWidth);
    }

    [UITestMethod]
    public async Task DropOnOriginalPosition_ThenLiveResizeDrag_CellsFollowHeaderWidth()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[1];

        DragHeaderOnto(tableView, column, column, offsetFromCenter: -6);

        var newWidth = column.ActualWidth + 60;
        tableView.BeginColumnResizeLive(column);
        tableView.UpdateColumnResizeLive(newWidth);
        AssertCellsMatchWidth(tableView, column, newWidth);
        tableView.EndColumnResizeLive(newWidth);

        AssertCellsMatchWidth(tableView, column, newWidth);
        Assert.AreEqual(newWidth, column.HeaderControl!.Width, 0.01,
            "The header must keep the committed width");
    }

    [UITestMethod]
    public async Task DropOnOtherColumn_ThenResize_CellsFollowHeaderWidth()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];
        var target = tableView.Columns[2];

        DragHeaderOnto(tableView, column, target, offsetFromCenter: 6);

        Assert.AreSame(column, tableView.Columns[2], "Precondition: the drop moved the column to the end");

        await Task.Delay(100, TestContext.CancellationToken);

        const double newWidth = 240d;
        column.Width = new GridLength(newWidth, GridUnitType.Pixel);

        Assert.IsNotNull(column.HeaderControl, "A moved column must keep its header");
        Assert.AreEqual(newWidth, column.HeaderControl.Width, 0.01,
            "Setting a moved column's Width must resize its header");
        AssertCellsMatchWidth(tableView, column, newWidth);
    }

    private static void DragHeaderOnto(TableView tableView, TableViewColumn dragged, TableViewColumn target, double offsetFromCenter)
    {
        var headerRow = tableView.FindDescendant<TableViewHeaderRow>()!;
        var targetHeader = target.HeaderControl!;
        var targetX = targetHeader.TransformToVisual(headerRow).TransformPoint(new Point(0, 0)).X;
        var position = targetX + (targetHeader.ActualWidth / 2) + offsetFromCenter;

        headerRow.ShowColumnDropIndicator(position, new RenderTargetBitmap());
        headerRow.ColumnDropCompleted(dragged, applyDrop: true);
    }

    private static void AssertCellsMatchWidth(TableView tableView, TableViewColumn column, double expectedWidth)
    {
        var cells = tableView.FindDescendants().OfType<TableViewRow>()
            .Where(r => r.IsLoaded)
            .Select(r => r.Cells.FirstOrDefault(c => c.Column == column))
            .OfType<TableViewCell>()
            .ToList();

        Assert.IsNotEmpty(cells, "Precondition: at least one realized cell for the column");

        foreach (var cell in cells)
        {
            Assert.AreEqual(expectedWidth, cell.Width, 0.01,
                $"Cell width must match the header width for column '{column.Header}'");
        }
    }
#endif

    private static List<string> CaptureColumnPropertyChanges(TableView tableView)
    {
        var changes = new List<string>();
        tableView.Columns.ColumnPropertyChanged += (_, e) => changes.Add(e.PropertyName);
        return changes;
    }

    private static async Task<TableView> CreateTableViewAsync()
    {
        var tableView = new TableView
        {
            Width = 800,
            Height = 400,
            AutoGenerateColumns = false
        };

        foreach (var path in new[] { nameof(ReorderTestItem.Name), nameof(ReorderTestItem.Value), nameof(ReorderTestItem.Description) })
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = path,
                Binding = new Binding { Path = new PropertyPath(path) }
            });
        }

        tableView.ItemsSource = new[]
        {
            new ReorderTestItem { Name = "Alpha", Value = 3, Description = "First item" },
            new ReorderTestItem { Name = "Beta",  Value = 1, Description = "Second item" },
            new ReorderTestItem { Name = "Gamma", Value = 2, Description = "Third item" }
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class ReorderTestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
