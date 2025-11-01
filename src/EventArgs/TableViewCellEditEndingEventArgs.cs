using Microsoft.UI.Xaml;
using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.CellEditEnding"/> event.
/// </summary>
public class TableViewCellEditEndingEventArgs : CancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewCellEditEndingEventArgs"/> class.
    /// </summary>
    /// <param name="cell">The cell being edited.</param>
    /// <param name="dataItem">The data item behind the row.</param>
    /// <param name="column">The column of the cell.</param>
    /// <param name="editingElement">The editing element.</param>
    /// <param name="editAction">The edit action.</param>
    public TableViewCellEditEndingEventArgs(TableViewCell cell, object? dataItem, TableViewColumn column, FrameworkElement editingElement, TableViewEditAction editAction)
    {
        Cell = cell;
        DataItem = dataItem;
        Column = column;
        EditingElement = editingElement;
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
    /// Gets the editing element.
    /// </summary>
    public FrameworkElement EditingElement { get; }

    /// <summary>
    /// Gets the edit action.
    /// </summary>
    public TableViewEditAction EditAction { get; }
}