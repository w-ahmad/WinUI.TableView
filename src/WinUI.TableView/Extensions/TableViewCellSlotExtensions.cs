namespace WinUI.TableView.Extensions;

internal static class TableViewCellSlotExtensions
{
    public static bool IsValidRow(this TableViewCellSlot slot, TableView tableView)
    {
        return slot.Row >= 0 && slot.Row < tableView?.Items.Count;
    }

    public static bool IsValidColumn(this TableViewCellSlot slot, TableView tableView)
    {
        return slot.Column >= 0 && slot.Column < tableView?.Columns.VisibleColumns.Count;
    }

    public static bool IsValid(this TableViewCellSlot slot, TableView tableView)
    {
        return slot.IsValidRow(tableView) && slot.IsValidColumn(tableView);
    }
}
