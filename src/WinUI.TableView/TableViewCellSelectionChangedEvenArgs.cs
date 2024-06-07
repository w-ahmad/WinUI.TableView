using System;
using System.Collections.Generic;

namespace WinUI.TableView;

internal class TableViewCellSelectionChangedEvenArgs : EventArgs
{
    public TableViewCellSelectionChangedEvenArgs(HashSet<TableViewCellSlot> oldSelection,
                                        HashSet<TableViewCellSlot> newSelection)
    {
        OldSelection = oldSelection;
        NewSelection = newSelection;
    }

    public HashSet<TableViewCellSlot> OldSelection { get; }
    public HashSet<TableViewCellSlot> NewSelection { get; }
}