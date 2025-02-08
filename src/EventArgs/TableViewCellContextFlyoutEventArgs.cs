using Microsoft.UI.Xaml.Controls.Primitives;
using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the CellContextFlyout event.
/// </summary>
public partial class TableViewCellContextFlyoutEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewCellContextFlyoutEventArgs class.
    /// </summary>
    /// <param name="slot">The slot of the cell for which the context flyout is being shown.</param>
    /// <param name="cell">The cell for which the context flyout is being shown.</param>
    /// <param name="item">The item associated with the cell.</param>
    /// <param name="flyout">The context flyout to be shown.</param>
    public TableViewCellContextFlyoutEventArgs(TableViewCellSlot slot, TableViewCell cell, object item, FlyoutBase flyout)
    {
        Slot = slot;
        Cell = cell;
        Item = item;
        Flyout = flyout;
    }

    /// <summary>
    /// Gets the slot of the cell for which the context flyout is being shown.
    /// </summary>
    public TableViewCellSlot Slot { get; }

    /// <summary>
    /// Gets the cell for which the context flyout is being shown.
    /// </summary>
    public TableViewCell Cell { get; }

    /// <summary>
    /// Gets the item associated with the cell.
    /// </summary>
    public object Item { get; }

    /// <summary>
    /// Gets the context flyout to be shown.
    /// </summary>
    public FlyoutBase Flyout { get; }
}
