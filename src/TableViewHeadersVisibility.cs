namespace WinUI.TableView;

/// <summary>
/// Specifies the visibility of the row or column headers in TableView.
/// </summary>
public enum TableViewHeadersVisibility
{
    /// <summary>
    /// No headers are visible.
    /// </summary>
    None,

    /// <summary>
    /// Only column headers are visible.
    /// </summary>
    Column,

    /// <summary>
    /// Only row headers are visible.
    /// </summary>
    Row,

    /// <summary>
    /// Both row and column headers are visible. 
    /// </summary>
    All
}
