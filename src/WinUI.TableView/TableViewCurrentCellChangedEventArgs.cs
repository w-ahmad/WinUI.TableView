using System;

namespace WinUI.TableView;

internal class TableViewCurrentCellChangedEventArgs : EventArgs
{
    public TableViewCurrentCellChangedEventArgs(TableViewCellSlot? oldSlot, TableViewCellSlot? newSlot)
    {
        OldSlot = oldSlot;
        NewSlot = newSlot;
    }

    public TableViewCellSlot? OldSlot { get; }
    public TableViewCellSlot? NewSlot { get; }
}