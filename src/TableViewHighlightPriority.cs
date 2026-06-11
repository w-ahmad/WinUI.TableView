namespace WinUI.TableView;

/// <summary>
/// Determines which highlight wins where a row highlight and a column highlight overlap,
/// when <see cref="TableView.MergeOverlappingHighlights"/> is disabled.
/// </summary>
public enum TableViewHighlightPriority
{
    /// <summary>
    /// The row highlight wins at the intersection.
    /// </summary>
    Row,

    /// <summary>
    /// The column highlight wins at the intersection.
    /// </summary>
    Column
}
