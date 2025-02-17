using Microsoft.UI.Xaml.Controls.Primitives;
using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the RowContextFlyout event.
/// </summary>
public partial class TableViewRowContextFlyoutEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewRowContextFlyoutEventArgs class.
    /// </summary>
    /// <param name="index">The index of the row.</param>
    /// <param name="row">The TableViewRow associated with the event.</param>
    /// <param name="item">The item associated with the row.</param>
    /// <param name="flyout">The flyout to be shown.</param>
    public TableViewRowContextFlyoutEventArgs(int index, TableViewRow row, object item, FlyoutBase? flyout)
    {
        Index = index;
        Row = row;
        Item = item;
        Flyout = flyout;
    }

    /// <summary>
    /// Gets the index of the row.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the TableViewRow associated with the event.
    /// </summary>
    public TableViewRow Row { get; }

    /// <summary>
    /// Gets the item associated with the row.
    /// </summary>
    public object Item { get; }

    /// <summary>
    /// Gets the flyout to be shown.
    /// </summary>
    public FlyoutBase? Flyout { get; }
}
