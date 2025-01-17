using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the event that is raised when sorting is cleared from TableView.
/// </summary>
public partial class TableViewClearSortingEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewClearSortingEventArgs"/> class.
    /// </summary>
    /// <param name="column">The column from which the sorting is being cleared.</param>
    public TableViewClearSortingEventArgs(TableViewColumn? column = default)
    {
        Column = column;
    }

    /// <summary>
    /// Gets the column from which the sorting is being cleared.
    /// </summary>
    public TableViewColumn? Column { get; }
}