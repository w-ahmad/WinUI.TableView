using Microsoft.UI.Xaml.Controls;
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
    /// Scrolls the rows area to a vertical offset and lets the row host realize the new viewport.
    /// </summary>
    public static async Task ScrollToAsync(TableView tableView, double verticalOffset)
    {
        GetScrollViewer(tableView).ChangeView(null, verticalOffset, null, true);

        await WaitForLayoutAsync(tableView);
        await WaitForLayoutAsync(tableView);
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
