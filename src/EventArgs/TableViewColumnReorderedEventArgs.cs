using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.ColumnReordered"/> event.
/// </summary>
public partial class TableViewColumnReorderedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewColumnReorderedEventArgs"/> class.
    /// </summary>
    /// <param name="column">The column that was reordered.</param>
    /// <param name="index">The new index of the column.</param>
    public TableViewColumnReorderedEventArgs(TableViewColumn column, int index)
    {
        Column = column;
        Index = index;
    }

    /// <summary>
    /// Gets the column that was reordered.
    /// </summary>
    public TableViewColumn Column { get; }

    /// <summary>
    /// Gets the new index of the column.
    /// </summary>
    public int Index { get; }
}
