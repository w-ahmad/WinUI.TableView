using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the CellDoubleTapped event.
/// </summary>
public partial class TableViewCellDoubleTappedEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewCellDoubleTappedEventArgs class.
    /// </summary>
    /// <param name="slot">The slot of the cell.</param>
    /// <param name="cell">The TableViewCell associated with the event.</param>
    /// <param name="item">The item associated with the cell.</param>
    public TableViewCellDoubleTappedEventArgs(TableViewCellSlot slot, TableViewCell cell, object item)
    {
        Slot = slot;
        Cell = cell;
        Item = item;
    }

    /// <summary>
    /// Gets the slot of the cell.
    /// </summary>
    public TableViewCellSlot Slot { get; }

    /// <summary>
    /// Gets the TableViewCell associated with the event.
    /// </summary>
    public TableViewCell Cell { get; }

    /// <summary>
    /// Gets the item associated with the cell.
    /// </summary>
    public object Item { get; }
}
