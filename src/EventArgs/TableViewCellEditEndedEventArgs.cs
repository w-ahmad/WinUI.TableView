using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.CellEditEnded"/> event.
/// </summary>
public class TableViewCellEditEndedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewCellEditEndedEventArgs"/> class.
    /// </summary>
    /// <param name="cell">The cell being edited.</param>
    /// <param name="dataItem">The data item behind the row.</param>
    /// <param name="column">The column of the cell.</param>
    /// <param name="editAction">The edit action.</param>
    public TableViewCellEditEndedEventArgs(TableViewCell cell, object? dataItem, TableViewColumn column, TableViewEditAction editAction)
    {
        Cell = cell;
        DataItem = dataItem;
        Column = column;
        EditAction = editAction;
    }

    /// <summary>
    /// Gets the cell being edited.
    /// </summary>
    public TableViewCell Cell { get; }

    /// <summary>
    /// Gets the data item behind the row.
    /// </summary>
    public object? DataItem { get; }

    /// <summary>
    /// Gets the column of the cell.
    /// </summary>
    public TableViewColumn Column { get; }

    /// <summary>
    /// Gets the edit action.
    /// </summary>
    public TableViewEditAction EditAction { get; }
}