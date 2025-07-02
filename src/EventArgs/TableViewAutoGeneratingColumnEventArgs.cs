using System;
using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the AutoGeneratingColumn event.
/// </summary>
public class TableViewAutoGeneratingColumnEventArgs : CancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewAutoGeneratingColumnEventArgs class.
    /// </summary>
    /// <param name="propertyName">The name of the property for which the column is being generated.</param>
    /// <param name="propertyType">The type of the property for which the column is being generated.</param>
    /// <param name="column">The column that is being generated.</param>
    public TableViewAutoGeneratingColumnEventArgs(string propertyName, Type? propertyType, TableViewColumn column)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
        Column = column;
    }

    /// <summary>
    /// Gets the name of the property for which the column is being generated.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the type of the property for which the column is being generated.
    /// </summary>
    public Type? PropertyType { get; }

    /// <summary>
    /// Gets or sets the column that is being generated.
    /// </summary>
    public TableViewColumn Column { get; set; }
}
