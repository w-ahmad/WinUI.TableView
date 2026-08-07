using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewColumnResizingTests
{
    // ── Direct (non-drag) header/column width propagation ───────────────────

    [UITestMethod]
    public async Task HeaderWidthChange_UpdatesColumnActualWidth_Immediately()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];
        var header = column.HeaderControl!;

        var originalWidth = header.Width;
        var newWidth = originalWidth + 50;

        header.Width = newWidth;

        Assert.AreEqual(newWidth, column.ActualWidth, 0.01,
            "Column.ActualWidth should update immediately via OnWidthChanged when header.Width changes outside of a resize-drag preview");
    }

    [UITestMethod]
    public async Task HeaderWidthChange_DoesNotChange_ColumnGridLengthType()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];
        var header = column.HeaderControl!;

        Assert.IsTrue(column.Width.IsAuto, "Precondition: column starts with Auto width");

        header.Width = 250;

        Assert.IsTrue(column.Width.IsAuto,
            "Column.Width GridLength must remain Auto until a resize is actually committed");
    }

    [UITestMethod]
    public async Task HeaderWidthChange_Propagates_ToCellWidths()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];
        var header = column.HeaderControl!;

        const double newWidth = 220d;
        header.Width = newWidth;

        var rows = tableView.FindDescendants().OfType<TableViewRow>().Where(r => r.IsLoaded).ToList();
        Assert.IsTrue(rows.Count > 0, "Precondition: at least one rendered row exists");

        foreach (var row in rows)
        {
            var cell = row.Cells.FirstOrDefault(c => c.Column == column);
            if (cell is null) continue;

            Assert.AreEqual(newWidth, cell.Width, 0.01,
                $"Cell width must match new header width for row index {row.Index}");
        }
    }

    [UITestMethod]
    public async Task CommittedResize_Stores_PixelGridLength()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];
        var header = column.HeaderControl!;

        Assert.IsTrue(column.Width.IsAuto, "Precondition: column starts with Auto width");

        const double finalWidth = 300d;
        header.Width = finalWidth;
        column.Width = new GridLength(finalWidth, GridUnitType.Pixel);

        await Task.Yield();

        Assert.IsTrue(column.Width.IsAbsolute, "Column.Width must be Absolute (Pixel) after resize commit");
        Assert.AreEqual(finalWidth, column.Width.Value, 0.01);
        Assert.AreEqual(finalWidth, header.Width, 0.01);
    }

    // ── Resize-drag preview: layout must stay frozen while active ───────────

    [UITestMethod]
    public async Task WhileResizePreviewActive_ColumnLayout_DoesNotChange()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];
        var originalActualWidth = column.ActualWidth;
        var originalGridLength = column.Width;

        var rows = tableView.FindDescendants().OfType<TableViewRow>().Where(r => r.IsLoaded).ToList();
        Assert.IsTrue(rows.Count > 0, "Precondition: at least one rendered row exists");
        var originalCellWidths = rows
            .Select(r => r.Cells.FirstOrDefault(c => c.Column == column))
            .Where(c => c is not null)
            .ToDictionary(c => c!, c => c!.Width);
        Assert.IsTrue(originalCellWidths.Count > 0, "Precondition: at least one realized cell for the column");

        tableView.BeginColumnResizePreview(column);
        try
        {
            foreach (var w in new[] { originalActualWidth + 40, originalActualWidth - 20, originalActualWidth + 100 })
            {
                tableView.UpdateColumnResizePreview(w);

                Assert.AreEqual(originalActualWidth, column.ActualWidth, 0.01,
                    "Column.ActualWidth must not change while a resize preview is active");
                Assert.AreEqual(originalGridLength.GridUnitType, column.Width.GridUnitType,
                    "Column.Width's GridLength type must not change while a resize preview is active");

                foreach (var (cell, originalWidth) in originalCellWidths)
                {
                    Assert.AreEqual(originalWidth, cell.Width, 0.01,
                        "A cell's real Width DP must not change while a resize preview is active — " +
                        "the live look comes entirely from Clip/RenderTransform, not real layout");
                }
            }
        }
        finally
        {
            tableView.EndColumnResizePreview(null);
        }
    }

    [UITestMethod]
    public async Task ResizePreview_Cancel_LeavesColumnCompletelyUnchanged()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];
        var originalActualWidth = column.ActualWidth;
        var originalGridLength = column.Width;

        tableView.BeginColumnResizePreview(column);
        tableView.UpdateColumnResizePreview(originalActualWidth + 75);
        tableView.EndColumnResizePreview(null); // cancel — e.g. a click without an actual drag

        Assert.AreEqual(originalActualWidth, column.ActualWidth, 0.01,
            "Cancelling a preview (no commit width) must leave ActualWidth untouched");
        Assert.AreEqual(originalGridLength.GridUnitType, column.Width.GridUnitType,
            "Cancelling a preview must not convert an Auto column to Pixel");
        Assert.IsFalse(tableView.IsColumnResizing, "IsColumnResizing must be cleared after End, even on cancel");
        Assert.IsFalse(column.IsResizing, "Column.IsResizing must be cleared after End, even on cancel");
    }

    // ── Resize-drag preview: the illusion's numbers must be correct ─────────

    [UITestMethod]
    public async Task ResizePreview_UpdatesClipAndDownstreamShift_ToMatchLiveWidth()
    {
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0]; // "Name" — has a downstream column ("Value") in the same row
        var originalWidth = column.ActualWidth;
        const double liveWidth = 260d;

        var row = tableView.FindDescendants().OfType<TableViewRow>().First(r => r.IsLoaded);
        var resizedCell = row.Cells.First(c => c.Column == column);
        var downstreamCell = row.Cells.First(c => c.Column == tableView.Columns[1]);

        tableView.BeginColumnResizePreview(column);
        tableView.UpdateColumnResizePreview(liveWidth);

        Assert.IsInstanceOfType<RectangleGeometry>(resizedCell.Clip);
        var clip = (RectangleGeometry)resizedCell.Clip;
        var expectedClip = TableViewCell.ComputeClipRect(liveWidth, clip.Rect.Height);
        Assert.AreEqual(expectedClip.Width, clip.Rect.Width, 0.01,
            "The resized cell's Clip width must track the live drag width via ComputeClipRect");

        Assert.IsInstanceOfType<TranslateTransform>(downstreamCell.RenderTransform);
        var shift = (TranslateTransform)downstreamCell.RenderTransform;
        Assert.AreEqual(liveWidth - originalWidth, shift.X, 0.01,
            "A downstream cell's shift must equal (liveWidth - originalWidth)");

        tableView.EndColumnResizePreview(null);
    }

    [UITestMethod]
    public async Task ResizePreview_EachRowGetsItsOwnClipAndShiftInstance()
    {
        // WinUI throws if the same Clip (RectangleGeometry) or RenderTransform instance is assigned
        // to more than one UIElement at a time, so each row's cells must get their own instance —
        // this test guards against reintroducing the shared-instance crash.
        var tableView = await CreateTableViewAsync();
        var column = tableView.Columns[0];

        var rows = tableView.FindDescendants().OfType<TableViewRow>().Where(r => r.IsLoaded).ToList();
        Assert.IsTrue(rows.Count > 1, "Precondition: more than one realized row");

        tableView.BeginColumnResizePreview(column);
        tableView.UpdateColumnResizePreview(column.ActualWidth + 30);

        var clips = rows.Select(r => r.Cells.First(c => c.Column == column).Clip).ToList();
        Assert.AreEqual(clips.Count, clips.Distinct().Count(),
            "Every realized row's resized cell must have its OWN Clip instance, not a shared one");

        var shifts = rows
            .SelectMany(r => r.Cells.Where(c => c.Column == tableView.Columns[1]))
            .Select(c => c.RenderTransform)
            .ToList();
        Assert.AreEqual(shifts.Count, shifts.Distinct().Count(),
            "Every realized row's downstream cell must have its OWN RenderTransform instance, not a shared one");

        tableView.EndColumnResizePreview(null);
    }

    // ── ColumnResizeMode toggle: Live mode relayouts for real, every frame ──

    [UITestMethod]
    public async Task ColumnResizeMode_DefaultsToLive()
    {
        var tableView = await CreateTableViewAsync();
        Assert.AreEqual(TableViewColumnResizeMode.Live, tableView.ColumnResizeMode);
    }

    [UITestMethod]
    public async Task LiveResize_UpdatesColumnActualWidth_OnEveryFrame()
    {
        var tableView = await CreateTableViewAsync();
        tableView.ColumnResizeMode = TableViewColumnResizeMode.Live;
        var column = tableView.Columns[0];
        var originalWidth = column.ActualWidth;

        tableView.BeginColumnResizeLive(column);
        try
        {
            foreach (var w in new[] { originalWidth + 40, originalWidth - 10, originalWidth + 90 })
            {
                tableView.UpdateColumnResizeLive(w);

                Assert.AreEqual(w, column.ActualWidth, 0.01,
                    "Unlike Preview mode, Live mode must update Column.ActualWidth on every frame");
                Assert.IsTrue(column.Width.IsAuto,
                    "Live mode must not touch Column.Width (GridLength) until commit");
            }
        }
        finally
        {
            tableView.EndColumnResizeLive(null);
        }
    }

    [UITestMethod]
    public async Task LiveResize_Commit_StoresPixelGridLength()
    {
        var tableView = await CreateTableViewAsync();
        tableView.ColumnResizeMode = TableViewColumnResizeMode.Live;
        var column = tableView.Columns[0];
        const double finalWidth = 245d;

        tableView.BeginColumnResizeLive(column);
        tableView.UpdateColumnResizeLive(finalWidth);
        tableView.EndColumnResizeLive(finalWidth);

        Assert.IsTrue(column.Width.IsAbsolute && column.Width.Value == finalWidth,
            "Live mode must commit the same Pixel GridLength as Preview mode does");
        Assert.AreEqual(finalWidth, column.ActualWidth, 0.01);
        Assert.IsFalse(tableView.IsColumnResizing);
        Assert.IsFalse(column.IsResizing);
    }

    [UITestMethod]
    public async Task LiveResize_Cancel_LeavesColumnWidthUnchanged()
    {
        var tableView = await CreateTableViewAsync();
        tableView.ColumnResizeMode = TableViewColumnResizeMode.Live;
        var column = tableView.Columns[0];
        var originalGridLength = column.Width;

        tableView.BeginColumnResizeLive(column);
        tableView.UpdateColumnResizeLive(column.ActualWidth + 55);
        tableView.EndColumnResizeLive(null);

        Assert.AreEqual(originalGridLength.GridUnitType, column.Width.GridUnitType,
            "Cancelling a Live resize must not convert an Auto column to Pixel");
    }

    // ── Resize-drag preview: commit must match a direct (non-drag) resize ───

    [UITestMethod]
    public async Task ResizePreview_Commit_MatchesDirectResizeExactly()
    {
        const double finalWidth = 275d;

        var dragged = await CreateTableViewAsync();
        var draggedColumn = dragged.Columns[0];
        dragged.BeginColumnResizePreview(draggedColumn);
        dragged.UpdateColumnResizePreview(draggedColumn.ActualWidth + 10);
        dragged.UpdateColumnResizePreview(finalWidth);
        dragged.EndColumnResizePreview(finalWidth);

        var direct = await CreateTableViewAsync();
        var directColumn = direct.Columns[0];
        directColumn.Width = new GridLength(finalWidth, GridUnitType.Pixel);
        await Task.Yield();

        Assert.AreEqual(directColumn.HeaderControl!.Width, draggedColumn.HeaderControl!.Width, 0.01,
            "A committed drag must produce the same header width as a direct resize");
        Assert.AreEqual(directColumn.ActualWidth, draggedColumn.ActualWidth, 0.01,
            "A committed drag must produce the same Column.ActualWidth as a direct resize");
        Assert.IsTrue(draggedColumn.Width.IsAbsolute && draggedColumn.Width.Value == finalWidth,
            "A committed drag must store the same Pixel GridLength as a direct resize");

        var draggedRows = dragged.FindDescendants().OfType<TableViewRow>().Where(r => r.IsLoaded);
        foreach (var row in draggedRows)
        {
            var cell = row.Cells.FirstOrDefault(c => c.Column == draggedColumn);
            if (cell is null) continue;

            Assert.AreEqual(finalWidth, cell.Width, 0.01, $"Cell width mismatch after commit, row {row.Index}");
            Assert.IsNull(cell.Clip, "Clip must be cleared once the drag commits");
            Assert.IsNull(cell.RenderTransform, "RenderTransform must be cleared once the drag commits");
        }
    }

    // ── Pure-function helpers (directly testable, no UI/pointer simulation needed) ──

    [UITestMethod]
    public void ClampWidth_ClampsToMinAndMax()
    {
        Assert.AreEqual(50d, TableViewColumnHeader.ClampWidth(10, 50, 500), 0.01, "Below min clamps to min");
        Assert.AreEqual(500d, TableViewColumnHeader.ClampWidth(900, 50, 500), 0.01, "Above max clamps to max");
        Assert.AreEqual(200d, TableViewColumnHeader.ClampWidth(200, 50, 500), 0.01, "In-range passes through unchanged");
        Assert.AreEqual(50d, TableViewColumnHeader.ClampWidth(50, 50, 500), 0.01, "Exactly at min stays at min");
        Assert.AreEqual(500d, TableViewColumnHeader.ClampWidth(500, 50, 500), 0.01, "Exactly at max stays at max");
    }

    [UITestMethod]
    public void ComputeClipRect_MatchesLiveWidthAndHeight()
    {
        var rect = TableViewCell.ComputeClipRect(180d, 32d);
        Assert.AreEqual(0d, rect.X, 0.01);
        Assert.AreEqual(0d, rect.Y, 0.01);
        Assert.AreEqual(180d, rect.Width, 0.01);
        Assert.AreEqual(32d, rect.Height, 0.01);
    }

    [UITestMethod]
    public void ComputeClipRect_NeverReturnsNegativeSize()
    {
        var rect = TableViewCell.ComputeClipRect(-10d, -5d);
        Assert.AreEqual(0d, rect.Width, 0.01, "A negative live width must clamp to zero, not a negative Rect size");
        Assert.AreEqual(0d, rect.Height, 0.01, "A negative height must clamp to zero, not a negative Rect size");
    }

    // ── Bug 3 regression: CalculateHeaderWidths stability / alignment after sort ────

    [UITestMethod]
    public async Task CalculateHeaderWidths_IsIdempotent()
    {
        var tableView = await CreateTableViewAsync();
        var headerRow = tableView.FindDescendant<TableViewHeaderRow>();

        Assert.IsNotNull(headerRow, "TableViewHeaderRow must be present in the visual tree");

        headerRow.CalculateHeaderWidths();
        var widths1 = tableView.Columns.Select(c => c.HeaderControl!.Width).ToArray();

        headerRow.CalculateHeaderWidths();
        var widths2 = tableView.Columns.Select(c => c.HeaderControl!.Width).ToArray();

        for (var i = 0; i < widths1.Length; i++)
        {
            Assert.AreEqual(widths1[i], widths2[i], 0.01,
                $"Column[{i}] width changed between consecutive CalculateHeaderWidths calls — indicates oscillation");
        }
    }

    [UITestMethod]
    public async Task AfterSort_CellWidths_MatchColumnActualWidths()
    {
        var tableView = await CreateTableViewAsync();

        var boundColumn = tableView.Columns.OfType<TableViewBoundColumn>().First();
        tableView.SortDescriptions.Add(
            new ColumnSortDescription(boundColumn, boundColumn.PropertyPath, SortDirection.Ascending));

        // Wait for the 250ms debounce timer + at least one layout pass to settle. This also exercises
        // the container-recycling width resync (TableViewRow.OnContentChanged) since sorting reorders
        // items behind already-realized row containers.
        await Task.Delay(600);

        var rows = tableView.FindDescendants().OfType<TableViewRow>().Where(r => r.IsLoaded).ToList();
        Assert.IsTrue(rows.Count > 0, "Precondition: at least one rendered row exists after sort");

        foreach (var column in tableView.Columns)
        {
            foreach (var row in rows)
            {
                var cell = row.Cells.FirstOrDefault(c => c.Column == column);
                if (cell is null) continue;

                Assert.AreEqual(column.ActualWidth, cell.Width, 0.01,
                    $"Cell width mismatch after sort: column '{column.Header}', row {row.Index}");
            }
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static async Task<TableView> CreateTableViewAsync()
    {
        var tableView = new TableView
        {
            Width = 800,
            Height = 400,
            AutoGenerateColumns = false
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(ResizingTestItem.Name)) }
        });
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Value",
            Binding = new Binding { Path = new PropertyPath(nameof(ResizingTestItem.Value)) }
        });
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Description",
            Binding = new Binding { Path = new PropertyPath(nameof(ResizingTestItem.Description)) }
        });

        tableView.ItemsSource = new[]
        {
            new ResizingTestItem { Name = "Alpha",  Value = 3, Description = "First item" },
            new ResizingTestItem { Name = "Beta",   Value = 1, Description = "Second item" },
            new ResizingTestItem { Name = "Gamma",  Value = 2, Description = "Third item" }
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class ResizingTestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
