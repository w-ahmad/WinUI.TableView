using System;
using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the cell selection changed event.
/// </summary>
internal class TableViewCellSelectionChangedEvenArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewCellSelectionChangedEvenArgs class.
    /// </summary>
    /// <param name="oldSelection">The old selection of cells.</param>
    /// <param name="newSelection">The new selection of cells.</param>
    public TableViewCellSelectionChangedEvenArgs(HashSet<TableViewCellSlot> oldSelection,
                                                 HashSet<TableViewCellSlot> newSelection)
    {
        OldSelection = oldSelection;
        NewSelection = newSelection;
    }

    /// <summary>
    /// Gets the old selection of cells.
    /// </summary>
    public HashSet<TableViewCellSlot> OldSelection { get; }

    /// <summary>
    /// Gets the new selection of cells.
    /// </summary>
    public HashSet<TableViewCellSlot> NewSelection { get; }
}
