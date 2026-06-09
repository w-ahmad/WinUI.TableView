namespace WinUI.TableView;

/// <summary>
/// Specifies the behavior for automatic column width.
/// </summary>
public enum ColumnAutoWidthMode
{
    /// <summary>
    /// Column width is adjusted to both header and maximum cell width.
    /// </summary>
    Both,

    /// <summary>
    /// Column width is adjusted to maximum cell width.
    /// </summary>
    Cells,

    /// <summary>
    /// Column width is adjusted to header width.
    /// </summary>
    Header
}
