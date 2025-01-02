namespace WinUI.TableView;

/// <summary>
/// Specifies the selection unit for a TableView.
/// </summary>
public enum TableViewSelectionUnit
{
    /// <summary>
    /// Selection can be either a cell or a row.
    /// </summary>
    CellOrRow,

    /// <summary>
    /// Selection is limited to cells.
    /// </summary>
    Cell,

    /// <summary>
    /// Selection is limited to rows.
    /// </summary>
    Row,
}
