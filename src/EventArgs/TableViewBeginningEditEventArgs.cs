using Microsoft.UI.Xaml;
using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.BeginningEdit"/> event.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TableViewBeginningEditEventArgs class.
/// </remarks>
/// <param name="cell">The cell being edited.</param>
/// <param name="dataItem">The data item associated with the cell.</param>
/// <param name="column">The column containing the cell.</param>
/// <param name="editingArgs">The event args for the editing event.</param>
public class TableViewBeginningEditEventArgs(TableViewCell cell,
                                             object? dataItem,
                                             TableViewColumn column,
                                             RoutedEventArgs editingArgs) 
    : CancelEventArgs
{
    /// <summary>
    /// Gets the cell that is entering edit mode.
    /// </summary>
    public TableViewCell Cell { get; } = cell;

    /// <summary>
    /// Gets the data item associated with the cell being edited.
    /// </summary>
    public object? DataItem { get; } = dataItem;

    /// <summary>
    /// Gets the column that contains the cell being edited, if available.
    /// </summary>
    public TableViewColumn Column { get; } = column;

    /// <summary>
    /// Gets the input event args that triggered the edit (e.g. double-tap).
    /// </summary>
    public RoutedEventArgs EditingArgs { get; } = editingArgs;
}