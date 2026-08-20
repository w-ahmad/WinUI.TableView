using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the event that is raised when a column is being grouped in a TableView.
/// </summary>
public partial class TableViewGroupingEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewGroupingEventArgs"/> class.
    /// </summary>
    /// <param name="column">The column that is being grouped.</param>
    public TableViewGroupingEventArgs(TableViewColumn column)
    {
        Column = column;
    }

    /// <summary>
    /// Gets the column that is being grouped.
    /// </summary>
    public TableViewColumn Column { get; }
}