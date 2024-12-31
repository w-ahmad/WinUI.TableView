using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the CurrentCellChanged event.
/// </summary>
internal class TableViewCurrentCellChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewCurrentCellChangedEventArgs class.
    /// </summary>
    /// <param name="oldSlot">The previous cell slot.</param>
    /// <param name="newSlot">The new cell slot.</param>
    public TableViewCurrentCellChangedEventArgs(TableViewCellSlot? oldSlot, TableViewCellSlot? newSlot)
    {
        OldSlot = oldSlot;
        NewSlot = newSlot;
    }

    /// <summary>
    /// Gets the previous cell slot.
    /// </summary>
    public TableViewCellSlot? OldSlot { get; }

    /// <summary>
    /// Gets the new cell slot.
    /// </summary>
    public TableViewCellSlot? NewSlot { get; }
}
