using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// Helpers for tests that drive the row host: settling layout and scrolling the rows area.
/// </summary>
internal static class TableViewRowHostTestHelpers
{
    /// <summary>
    /// Returns the ScrollViewer hosting the table's rows.
    /// </summary>
    public static ScrollViewer GetScrollViewer(TableView tableView)
    {
        return tableView.FindDescendants().OfType<ScrollViewer>().First();
    }

    /// <summary>
    /// Returns the repeater hosting the table's rows.
    /// </summary>
    public static ItemsRepeater GetRowsRepeater(TableView tableView)
    {
        return tableView.FindDescendants().OfType<ItemsRepeater>().First(x => x.Name is "RowsRepeater");
    }

    /// <summary>
    /// Returns the most rows of a given height the repeater should hold for a viewport: the viewport and the cache
    /// kept around it, which is a multiple of the viewport split between both sides, plus the rows cut by the edges.
    /// </summary>
    public static int GetMaxRealizedRows(TableView tableView, double viewportHeight, double rowHeight)
    {
        var cacheLength = GetRowsRepeater(tableView).VerticalCacheLength;

        return (int)System.Math.Ceiling(viewportHeight * (1d + cacheLength) / rowHeight) + 4;
    }

    /// <summary>
    /// Waits for a scroll the table started itself to be applied: the offset moves only once the scroll is rendered.
    /// </summary>
    public static async Task WaitForScrollToSettleAsync(TableView tableView)
    {
        var scrollViewer = GetScrollViewer(tableView);
        var offset = double.NaN;

        for (int frame = 0, stableFrames = 0; frame < 100 && stableFrames < 3; frame++)
        {
            await Task.Delay(16);
            await WaitForLayoutAsync(tableView);

            stableFrames = scrollViewer.VerticalOffset == offset ? stableFrames + 1 : 0;
            offset = scrollViewer.VerticalOffset;
        }
    }

    /// <summary>
    /// Scrolls the rows area to a vertical offset and lets the row host realize the new viewport.
    /// </summary>
    public static async Task ScrollToAsync(TableView tableView, double verticalOffset)
    {
        var scrollViewer = GetScrollViewer(tableView);
        var startOffset = scrollViewer.VerticalOffset;
        scrollViewer.ChangeView(null, verticalOffset, null, true);

        // ChangeView returns before VerticalOffset moves, and the repeater only hears about the new viewport once the
        // scroll has been rendered, keeping the old rows until then. Wait for both, or the caller sees the old rows.
        // The offset is not compared with the target: scroll anchoring can nudge it once measured heights replace
        // estimated ones.
        for (var frame = 0; frame < 100; frame++)
        {
            var stays = System.Math.Abs(System.Math.Min(verticalOffset, scrollViewer.ScrollableHeight) - startOffset) < 1d;
            var moved = System.Math.Abs(scrollViewer.VerticalOffset - startOffset) >= 1d;

            if ((stays || moved) && IsRealizedAt(tableView, tableView.RowsViewportTop))
            {
                break;
            }

            await Task.Delay(16);
            await WaitForLayoutAsync(tableView);
        }

        // The repeater then grows its cache around the viewport over the next few frames. Let that settle too.
        var realized = -1;

        for (var stableFrames = 0; stableFrames < 3;)
        {
            var count = GetRealizedElements(tableView).Count();
            stableFrames = count == realized ? stableFrames + 1 : 0;
            realized = count;

            await Task.Delay(16);
            await WaitForLayoutAsync(tableView);
        }
    }

    /// <summary>
    /// Returns whether the repeater has realized an element at a vertical position in its own coordinates.
    /// </summary>
    private static bool IsRealizedAt(TableView tableView, double top)
    {
        return GetRealizedElements(tableView).Any(x => x.ActualOffset.Y <= top && x.ActualOffset.Y + x.ActualSize.Y > top);
    }

    /// <summary>
    /// Returns the rows and group headers the repeater currently has realized, leaving out pooled ones.
    /// </summary>
    private static IEnumerable<UIElement> GetRealizedElements(TableView tableView)
    {
        var repeater = GetRowsRepeater(tableView);

        return Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(repeater))
                         .Select(i => (UIElement)VisualTreeHelper.GetChild(repeater, i))
                         .Where(x => repeater.GetElementIndex(x) >= 0);
    }

    /// <summary>
    /// Runs layout until the repeater has realized and arranged what the last change asked for.
    /// </summary>
    public static async Task WaitForLayoutAsync(TableView tableView)
    {
        tableView.UpdateLayout();
        await Task.Yield();
        tableView.UpdateLayout();
        await Task.Yield();
        tableView.UpdateLayout();
    }
}
