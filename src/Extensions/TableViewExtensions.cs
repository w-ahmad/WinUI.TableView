namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="TableView"/> class.
/// </summary>
internal static class TableViewExtensions
{
    /// <summary>
    /// Gets the height of the horizontal gridlines in the specified <see cref="TableView"/>.
    /// </summary>
    internal static double GetHorizontalGridlineHeight(this TableView? tableView)
    {
        return tableView?.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
            ? tableView.HorizontalGridLinesStrokeThickness : 0d;
    }
}
