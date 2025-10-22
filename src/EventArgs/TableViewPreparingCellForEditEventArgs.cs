using Microsoft.UI.Xaml;
using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.PreparingCellForEdit"/> event.
/// </summary>
public class TableViewPreparingCellForEditEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewPreparingCellForEditEventArgs"/> class.
    /// </summary>
    /// <param name="cell">The cell that is being prepared for edit.</param>
    /// <param name="item">The data item associated with the cell.</param>
    /// <param name="column">The column that contains the cell.</param>
    /// <param name="editingElement">The element that will be used for editing the cell.</param>
    /// <param name="editingArgs">The original routed event args that triggered the edit request.</param>
    public TableViewPreparingCellForEditEventArgs(TableViewCell cell, object? item, TableViewColumn? column, FrameworkElement? editingElement, RoutedEventArgs editingArgs)
    {
        Cell = cell;
        Item = item;
        Column = column;
        EditingElement = editingElement;
        EditingArgs = editingArgs;
    }

    /// <summary>
    /// Gets the <see cref="TableViewCell"/> that will be edited.
    /// </summary>
    public TableViewCell Cell { get; }

    /// <summary>
    /// Gets the data item associated with the cell being edited.
    /// </summary>
    public object? Item { get; }

    /// <summary>
    /// Gets the column that owns the cell being edited, if available.
    /// </summary>
    public TableViewColumn? Column { get; }

    /// <summary>
    /// Gets the element that will be used for editing the cell.
    /// </summary>
    public FrameworkElement? EditingElement { get; }

    /// <summary>
    /// Gets the original <see cref="RoutedEventArgs"/> that triggered the edit operation.
    /// </summary>
    public RoutedEventArgs EditingArgs { get; }
}
