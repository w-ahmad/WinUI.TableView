namespace WinUI.TableView;

/// <summary>
/// Specifies the selection unit for a TableView.
/// </summary>
public enum TableViewSelectionUnit
{
    /// <summary>
    /// Selection can be either a cell or a row, but selecting a cell does not select the owning row.
    /// </summary>
    CellOrRow,

    /// <summary>
    /// Selection can be either a cell or a row, but selecting a cell also selects the owning row.
    /// </summary>
    CellWithRow,

    /// <summary>
    /// Selection is limited to cells.
    /// </summary>
    Cell,

    /// <summary>
    /// Selection is limited to rows.
    /// </summary>
    Row,
}
