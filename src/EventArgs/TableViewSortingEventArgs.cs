using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the event that is raised when a column is being sorted in a TableView.
/// </summary>
public partial class TableViewSortingEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewSortingEventArgs"/> class.
    /// </summary>
    /// <param name="column">The column that is being sorted.</param>
    public TableViewSortingEventArgs(TableViewColumn column)
    {
        Column = column;
    }

    /// <summary>
    /// Gets the column that is being sorted.
    /// </summary>
    public TableViewColumn Column { get; }
}
