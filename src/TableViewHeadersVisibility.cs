namespace WinUI.TableView;

/// <summary>
/// Specifies the visibility of the row or column headers in TableView.
/// </summary>
public enum TableViewHeadersVisibility
{
    /// <summary>
    /// Both row and column headers are visible. 
    /// </summary>
    All,

    /// <summary>
    /// Only column headers are visible.
    /// </summary>
    Columns,

    /// <summary>
    /// Only row headers are visible.
    /// </summary>
    Rows,

    /// <summary>
    /// No headers are visible.
    /// </summary>
    None,
}
