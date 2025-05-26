using System;
using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the cell selection changed event.
/// </summary>
public class TableViewCellSelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewCellSelectionChangedEventArgs class.
    /// </summary>
    /// <param name="removedCells">The list that contains the cells that were unselected.</param>
    /// <param name="addedCells">The list that contains the cells that were selected.</param>
    public TableViewCellSelectionChangedEventArgs(IList<TableViewCellSlot> removedCells,
                                                  IList<TableViewCellSlot> addedCells)
    {
        RemovedCells = removedCells;
        AddedCells = addedCells;
    }

    /// <summary>
    /// Gets a list that contains the cells that were unselected.
    /// </summary>
    public IList<TableViewCellSlot> RemovedCells { get; }

    /// <summary>
    /// Gets a list that contains the cells that were selected.
    /// </summary>
    public IList<TableViewCellSlot> AddedCells { get; }
}
