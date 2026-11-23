using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.AutomationPeers;
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
        tableView.CompleteColumnJump();
        tableView.UpdateLayout();

        // Uno gives a cell that has just been created its bound value a tick later.
        await Task.Delay(50);
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
        tableView.CompleteColumnJump();
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
        tableView.CompleteColumnJump();
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

    [UITestMethod]
    public async Task CurrentCell_OutOfView_GetsACellAndIsScrolledIntoView()
    {
        var tableView = await CreateTableViewAsync();
        tableView.SelectionUnit = TableViewSelectionUnit.Cell;

        tableView.CurrentCellSlot = new TableViewCellSlot(1, 160);
        await Task.Delay(200);
        tableView.UpdateLayout();

        var row = Rows(tableView).Single(x => x.Index == 1);
        var cell = row.GetCell(160);

        Assert.IsNotNull(cell, "the current cell has no cell");
        Assert.IsTrue(tableView.HorizontalOffset > 0d, "the table did not scroll to the current cell");
        Assert.IsTrue(cell.ActualOffset.X + cell.ActualWidth <= tableView.HorizontalOffset + TableWidth, "the current cell is not in view");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GridPattern_GetItem_ReachesAColumnOutOfView()
    {
        var tableView = await CreateTableViewAsync();
        var peer = new TableViewAutomationPeer(tableView);

        Assert.IsNull(Rows(tableView).Single(x => x.Index == 2).GetCell(180));
        Assert.IsNotNull(peer.GetItem(2, 180), "the Grid pattern should reach a cell of a column out of view");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Editing_CanMoveToACellOutOfView()
    {
        var tableView = await CreateTableViewAsync();
        tableView.SelectionUnit = TableViewSelectionUnit.Cell;
        var slot = new TableViewCellSlot(0, 170);

        var cell = tableView.GetOrRealizeCellFromSlot(slot);

        Assert.IsNotNull(cell, "no cell was created for the column out of view");
        Assert.IsTrue(cell.BeginCellEditing(new RoutedEventArgs()), "the cell did not start editing");

        tableView.MakeSelection(slot, false);
        await Task.Delay(200);
        tableView.UpdateLayout();

        Assert.AreSame(cell, Rows(tableView).Single(x => x.Index == 0).GetCell(170), "the cell being edited was replaced");
        Assert.IsInstanceOfType<TextBox>(cell.Content);
        Assert.IsTrue(tableView.HorizontalOffset > 0d);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ResizePreview_GivesCellsToTheColumnsANarrowerColumnBringsIntoView()
    {
        var tableView = await CreateTableViewAsync();
        var row = Rows(tableView).First();
        var column = (TableViewColumn)tableView.Columns[1];
        column.Width = new GridLength(600);
        tableView.UpdateLayout();

        var lastBefore = row.Cells[^1].Index;

        tableView.BeginColumnResizePreview(column);

        // Narrowing by 500px slides five more columns into view, with their lead.
        Assert.IsTrue(row.Cells[^1].Index >= lastBefore + 5, $"last cell {row.Cells[^1].Index}, was {lastBefore}");

        tableView.UpdateColumnResizePreview(100);
        tableView.EndColumnResizePreview(100);
        tableView.UpdateLayout();

        Assert.IsTrue(row.Cells.Count <= MaxRealizedColumns, $"{row.Cells.Count} cells after the resize");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task AddingColumnsToALoadedTable_KeepsTheCellsToTheViewport()
    {
        var tableView = await CreateTableViewAsync();

        for (var i = 0; i < 100; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn { Header = $"Added {i}", Width = new GridLength(ColumnWidth) });
        }

        tableView.UpdateLayout();

        foreach (var row in Rows(tableView))
        {
            Assert.IsTrue(row.Cells.Count <= MaxRealizedColumns, $"{row.Cells.Count} cells after adding columns");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task WideTable_HasHeadersOnlyForColumnsNearTheViewport()
    {
        var tableView = await CreateTableViewAsync(frozenColumnCount: 1);
        var headerRow = tableView.FindDescendants().OfType<TableViewHeaderRow>().Single();
        var columns = tableView.Columns.VisibleColumns;

        Assert.IsTrue(headerRow.Headers.Count >= 9, $"{headerRow.Headers.Count} headers do not cover the viewport");
        Assert.IsTrue(headerRow.Headers.Count <= MaxRealizedColumns + 1, $"{headerRow.Headers.Count} headers for a {TableWidth}px viewport");
        Assert.IsNotNull(columns[0].HeaderControl);
        Assert.IsNull(columns[ColumnCount - 1].HeaderControl);

        // A column is sized whether or not it has a header.
        Assert.AreEqual(ColumnWidth, columns[ColumnCount - 1].ActualWidth);

        await tableView.ScrollCellIntoView(new TableViewCellSlot(0, 130));
        tableView.UpdateLayout();
        tableView.CompleteColumnJump();
        tableView.UpdateLayout();

        Assert.IsTrue(headerRow.Headers.Count <= MaxRealizedColumns + 1, $"{headerRow.Headers.Count} headers after scrolling");
        Assert.IsNotNull(columns[0].HeaderControl, "the frozen column lost its header");
        Assert.IsNull(columns[1].HeaderControl, "the first scrollable column is far out of view");

        var header = columns[130].HeaderControl;
        Assert.IsNotNull(header, "the column scrolled to has no header");
        Assert.AreEqual(129 * ColumnWidth, header.ActualOffset.X, 0.01);
        Assert.AreEqual(ColumnWidth, header.ActualWidth, 0.01);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ColumnChanges_KeepHeadersInLineWithTheColumnsInView()
    {
        var tableView = await CreateTableViewAsync();
        var headerRow = tableView.FindDescendants().OfType<TableViewHeaderRow>().Single();
        var hidden = (TableViewColumn)tableView.Columns[2];

        hidden.Visibility = Visibility.Collapsed;
        tableView.Columns.Insert(0, new TableViewTextColumn { Header = "New", Width = new GridLength(ColumnWidth) });
        tableView.Columns.Move(4, 1);
        tableView.UpdateLayout();

        var columns = tableView.Columns.VisibleColumns;
        var headers = headerRow.Headers;

        Assert.IsNull(hidden.HeaderControl);

        for (var i = 0; i < headers.Count; i++)
        {
            Assert.AreSame(columns[i], headers[i].Column, $"header {i}");
            Assert.AreSame(headers[i], columns[i].HeaderControl, $"header {i}");
            Assert.AreEqual(i * ColumnWidth, headers[i].ActualOffset.X, 0.01, $"header {i}");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task AutoWidthColumns_OutOfView_AreSizedWithoutAHeader()
    {
        var tableView = new TableView { Width = TableWidth, Height = 300, AutoGenerateColumns = false };

        for (var i = 0; i < ColumnCount; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn { Header = $"Column {i}", Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) } });
        }

        tableView.ItemsSource = new List<HostItem> { new() { Name = "One" } };
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.UpdateLayout();

        var last = tableView.Columns.VisibleColumns[ColumnCount - 1];

        Assert.IsNull(last.HeaderControl);
        Assert.IsTrue(last.ActualWidth > 0d, "a column out of view has no width");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task DraggingTheScrollBarFar_MovesTheHeadersAtOnce_AndTheCellsWhenItRests()
    {
        var tableView = await CreateTableViewAsync();
        var row = Rows(tableView).First();
        var columns = tableView.Columns.VisibleColumns;

        tableView.MaxCellsDeferral = System.TimeSpan.FromHours(1);
        tableView.FarCellsSyncCost = System.TimeSpan.FromSeconds(1);
        tableView.IsHorizontalThumbTracking = true;
        tableView.SetValue(TableView.HorizontalOffsetProperty, 140 * ColumnWidth);
        tableView.UpdateLayout();

        Assert.IsTrue(tableView.AreCellsDeferred, "the cells should wait while the thumb is dragged far");
        Assert.IsNotNull(columns[142].HeaderControl, "the headers should follow the thumb");
        Assert.IsNull(row.GetCell(142), "the cells should not follow the thumb yet");

        tableView.IsHorizontalThumbTracking = false;
        tableView.UpdateRealizedColumns(flush: true);
        tableView.UpdateLayout();

        Assert.IsFalse(tableView.AreCellsDeferred);
        Assert.IsNotNull(row.GetCell(142), "the cells should follow once the thumb is let go");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task DraggingTheScrollBarFar_MovesTheCellsAtOnce_WhenThatIsCheap()
    {
        var tableView = await CreateTableViewAsync();
        var row = Rows(tableView).First();

        tableView.FarCellsSyncCost = System.TimeSpan.Zero;
        tableView.IsHorizontalThumbTracking = true;
        tableView.SetValue(TableView.HorizontalOffsetProperty, 140 * ColumnWidth);
        tableView.UpdateLayout();

        Assert.IsFalse(tableView.AreCellsDeferred);
        Assert.IsNotNull(row.GetCell(142), "the cells should follow the thumb");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task DraggingTheScrollBarALittle_MovesTheCellsAtOnce()
    {
        var tableView = await CreateTableViewAsync();
        var row = Rows(tableView).First();
        var last = row.Cells[^1].Index;

        tableView.MaxCellsDeferral = System.TimeSpan.FromHours(1);
        tableView.FarCellsSyncCost = System.TimeSpan.FromSeconds(1);
        tableView.IsHorizontalThumbTracking = true;
        tableView.SetValue(TableView.HorizontalOffsetProperty, 3 * ColumnWidth);
        tableView.UpdateLayout();

        Assert.IsFalse(tableView.AreCellsDeferred, "a short drag keeps most cells, so nothing waits");
        Assert.IsTrue(row.Cells[^1].Index > last, "the cells did not follow a short drag");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ReusedHeadersAndCells_ShowTheColumnTheyHaveNow()
    {
        var tableView = await CreateTableViewAsync();
        var headerRow = tableView.FindDescendants().OfType<TableViewHeaderRow>().Single();
        var columns = tableView.Columns.VisibleColumns;
        var firstHeader = columns[0].HeaderControl;

        tableView.SetValue(TableView.HorizontalOffsetProperty, 100 * ColumnWidth);
        tableView.UpdateLayout();
        tableView.CompleteColumnJump();
        tableView.UpdateLayout();

        // Headers are taken over, not created, when the view moves.
        Assert.IsTrue(headerRow.Headers.Any(x => ReferenceEquals(x, firstHeader)), "the first header was not reused");
        Assert.IsNull(columns[0].HeaderControl);

        foreach (var header in headerRow.Headers)
        {
            var index = columns.IndexOf(header.Column!);

            Assert.AreSame(header, columns[index].HeaderControl, $"header {index}");
            Assert.AreEqual($"Column {index}", header.Content, $"header {index}");
            Assert.AreEqual(index * ColumnWidth, header.ActualOffset.X, 0.01, $"header {index}");
        }

        foreach (var cell in Rows(tableView).First().Cells)
        {
            Assert.AreSame(columns[cell.Index], cell.Column);
            Assert.AreEqual(cell.Index * ColumnWidth, cell.ActualOffset.X, 0.01, $"cell {cell.Index}");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task AJumpAcrossManyColumns_FillsRowsInViewAtOnce_AndTheRestWhenTheViewRests()
    {
        var tableView = await CreateTableViewAsync();
        var rows = Rows(tableView).OrderBy(x => x.Index).ToList();
        var inView = rows[0];
        var outOfView = rows[^1];

        tableView.SetValue(TableView.HorizontalOffsetProperty, 140 * ColumnWidth);
        tableView.UpdateLayout();

        Assert.IsNotNull(inView.GetCell(142), "a row in view should have its cells at once");
        Assert.IsTrue(outOfView.HasStaleCells, "a row kept below the view should wait");
        Assert.IsNull(outOfView.GetCell(142));

        var cellsAfterJump = inView.Cells.Count;

        tableView.CompleteColumnJump();
        tableView.UpdateLayout();

        Assert.IsFalse(outOfView.HasStaleCells);
        Assert.IsNotNull(outOfView.GetCell(142), "the row should be filled in once the view rests");
        Assert.IsTrue(inView.Cells.Count > cellsAfterJump, "the columns either side of the view should follow");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ColumnAddedTwice_HasACellAndAHeaderInBothPlaces()
    {
        var tableView = new TableView { Width = 500, Height = 300, AutoGenerateColumns = false };
        var name = new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(ColumnWidth),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        };
        var category = new TableViewTextColumn
        {
            Header = "Category",
            Width = new GridLength(ColumnWidth),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Category)) }
        };

        tableView.Columns.Add(name);
        tableView.Columns.Add(category);
        tableView.Columns.Add(name);
        tableView.ItemsSource = new List<HostItem> { new() { Id = 1, Name = "Item 1", Category = "Category 1" } };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.UpdateLayout();

        var row = Rows(tableView)[0];
        var cells = row.Cells;

        Assert.AreEqual(3, cells.Count);
        Assert.AreSame(name, cells[0].Column);
        Assert.AreSame(category, cells[1].Column);
        Assert.AreSame(name, cells[2].Column);
        Assert.AreNotSame(cells[0], cells[2]);
        Assert.AreSame(cells[0], row.GetCell(0));
        Assert.AreSame(cells[2], row.GetCell(2));
        Assert.AreEqual(2 * ColumnWidth, cells[2].TransformToVisual(cells[0]).TransformPoint(default).X, 0.5);

        var headers = tableView.HeaderRow!.Headers;

        Assert.AreEqual(3, headers.Count);
        Assert.AreSame(headers[0], name.HeaderControl);
        Assert.AreSame(name, headers[2].Column);

        name.Width = new GridLength(150);
        tableView.UpdateLayout();

        Assert.AreEqual(150d, cells[0].ActualWidth, 0.5);
        Assert.AreEqual(150d, cells[2].ActualWidth, 0.5);
        Assert.AreEqual(150d, headers[2].ActualWidth, 0.5);

        tableView.Columns.RemoveAt(0);
        tableView.UpdateLayout();

        Assert.AreEqual(2, row.Cells.Count);
        Assert.AreSame(category, row.Cells[0].Column);
        Assert.AreSame(name, row.Cells[1].Column);
        Assert.AreEqual(2, tableView.HeaderRow!.Headers.Count);
        Assert.IsNotNull(name.HeaderControl);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ColumnAddedTwice_InAWideTable_GetsItsSecondCellWhenScrolledTo()
    {
        var tableView = await CreateTableViewAsync();
        var first = tableView.Columns[0];

        tableView.Columns.Add(first);
        tableView.UpdateLayout();

        var row = Rows(tableView).OrderBy(x => x.Index).First();

        Assert.IsNotNull(row.GetCell(0));
        Assert.IsNull(row.GetCell(ColumnCount), "the second place is out of view");

        await tableView.ScrollCellIntoView(new TableViewCellSlot(0, ColumnCount));
        tableView.UpdateLayout();
        tableView.CompleteColumnJump();
        tableView.UpdateLayout();

        var cell = row.GetCell(ColumnCount);

        Assert.IsNotNull(cell);
        Assert.AreSame(first, cell.Column);
        Assert.AreEqual(1, row.Cells.Count(x => x.Column == first), "the first place is out of view now");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task MovingAColumn_MovesItsHeaderAndCells()
    {
        var tableView = await CreateTableViewAsync();
        var moved = tableView.Columns.VisibleColumns[1];

        tableView.Columns.Move(1, 3);
        tableView.UpdateLayout();

        Assert.AreSame(moved, tableView.Columns.VisibleColumns[3]);
        Assert.AreEqual(3 * ColumnWidth, moved.HeaderControl!.ActualOffset.X, 0.01);

        foreach (var row in Rows(tableView))
        {
            Assert.AreEqual(3 * ColumnWidth, row.GetCell(moved)!.ActualOffset.X, 0.01);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task FrozenColumns_StayInColumnOrder_WhenMovedOrHidden()
    {
        var tableView = await CreateTableViewAsync(frozenColumnCount: 3);
        var headerRow = tableView.FindDescendants().OfType<TableViewHeaderRow>().Single();

        tableView.Columns.Move(0, 1);
        tableView.UpdateLayout();
        AssertFrozenInOrder();

        tableView.Columns.VisibleColumns[0].Visibility = Visibility.Collapsed;
        tableView.UpdateLayout();
        AssertFrozenInOrder();

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);

        void AssertFrozenInOrder()
        {
            var columns = tableView.Columns.VisibleColumns;

            for (var i = 0; i < 3; i++)
            {
                Assert.AreSame(columns[i], headerRow.Headers[i].Column, $"header {i}");
                Assert.AreEqual(i * ColumnWidth, headerRow.Headers[i].ActualOffset.X, 0.01, $"header {i}");
            }

            foreach (var row in Rows(tableView))
            {
                for (var i = 0; i < 3; i++)
                {
                    Assert.AreSame(columns[i], row.Cells[i].Column, $"cell {i}");
                    Assert.AreEqual(i * ColumnWidth, row.Cells[i].ActualOffset.X, 0.01, $"cell {i}");
                }
            }
        }
    }

    [UITestMethod]
    public async Task ScrollingSideways_WhileEditing_GivesReusedCellsTheirColumnsContent()
    {
        var tableView = await CreateTableViewAsync();

        tableView.SetIsEditing(true);
        tableView.SetValue(TableView.HorizontalOffsetProperty, 5 * ColumnWidth);
        tableView.UpdateLayout();
        tableView.SetIsEditing(false);

        await Task.Delay(50);
        tableView.UpdateLayout();

        var columns = tableView.Columns.VisibleColumns;

        foreach (var row in Rows(tableView))
        {
            for (var index = 5; index <= 12; index++)
            {
                var expected = index % 2 == 0 ? ((HostItem)row.Content).Name : ((HostItem)row.Content).Category;
                Assert.AreEqual(expected, (row.GetCell(index)?.Content as TextBlock)?.Text, $"column {index}");
            }
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CellsCreatedOnDemand_DoNotPileUp()
    {
        var tableView = await CreateTableViewAsync();
        var row = Rows(tableView).OrderBy(x => x.Index).First();

        for (var index = 0; index < ColumnCount; index++)
        {
            Assert.IsNotNull(row.GetOrRealizeCell(index));
        }

        Assert.IsTrue(row.RowPresenter!.Cells.Count < ColumnCount / 2, $"{row.RowPresenter.Cells.Count} cells");

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
