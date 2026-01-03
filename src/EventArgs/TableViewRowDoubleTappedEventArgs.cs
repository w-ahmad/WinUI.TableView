using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the RowDoubleTapped event.
/// </summary>
public partial class TableViewRowDoubleTappedEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewRowDoubleTappedEventArgs class.
    /// </summary>
    /// <param name="index">The index of the row.</param>
    /// <param name="row">The TableViewRow associated with the event.</param>
    /// <param name="item">The row associated with double tap/click.</param>
    public TableViewRowDoubleTappedEventArgs(int index, TableViewRow row, object item)
    {
        Index = index;
        Row = row;
        Item = item;
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
}
