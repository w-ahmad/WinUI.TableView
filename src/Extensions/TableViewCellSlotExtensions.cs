namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for the TableViewCellSlot type.
/// </summary>
internal static class TableViewCellSlotExtensions
{
    /// <summary>
    /// Determines whether the row index of the slot is valid within the TableView.
    /// </summary>
    /// <param name="slot">The TableViewCellSlot to check.</param>
    /// <param name="tableView">The TableView to check against.</param>
    /// <returns>True if the row index of TableView is valid; otherwise, false.</returns>
    public static bool IsValidRow(this TableViewCellSlot slot, TableView tableView)
    {
        return slot.Row >= 0 && slot.Row < tableView?.Items.Count;
    }

    /// <summary>
    /// Determines whether the column index of the slot is valid within the TableView.
    /// </summary>
    /// <param name="slot">The TableViewCellSlot to check.</param>
    /// <param name="tableView">The TableView to check against.</param>
    /// <returns>True if the column index TableView is valid; otherwise, false.</returns>
    public static bool IsValidColumn(this TableViewCellSlot slot, TableView tableView)
    {
        return slot.Column >= 0 && slot.Column < tableView?.Columns.VisibleColumns.Count;
    }

    /// <summary>
    /// Determines whether both the row and column indices of the slot are valid within the TableView.
    /// </summary>
    /// <param name="slot">The TableViewCellSlot to check.</param>
    /// <param name="tableView">The TableView to check against.</param>
    /// <returns>True if both the row and column indices of TableView are valid; otherwise, false.</returns>
    public static bool IsValid(this TableViewCellSlot slot, TableView tableView)
    {
        return slot.IsValidRow(tableView) && slot.IsValidColumn(tableView);
    }
}
