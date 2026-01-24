using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.ColumnReordering"/> event.
/// </summary>
public partial class TableViewColumnReorderingEventArgs : CancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewColumnReorderingEventArgs"/> class.
    /// </summary>
    /// <param name="column">The column being reordered.</param>
    /// <param name="dropIndex">The index where the column is being dropped.</param>
    public TableViewColumnReorderingEventArgs(TableViewColumn column, int dropIndex)
    {
        Column = column;
        DropIndex = dropIndex;
    }

    /// <summary>
    /// Gets the column being reordered.
    /// </summary>
    public TableViewColumn Column { get; }

    /// <summary>
    /// Gets the index where the column is being dropped.
    /// </summary>
    public int DropIndex { get; }
}