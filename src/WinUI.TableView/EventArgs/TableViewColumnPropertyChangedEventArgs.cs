using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the ColumnPropertyChanged event.
/// </summary>
internal class TableViewColumnPropertyChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewColumnPropertyChanged class.
    /// </summary>
    /// <param name="column">The column that changed.</param>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="index">The index of the column in the collection.</param>
    public TableViewColumnPropertyChangedEventArgs(TableViewColumn column, string propertyName, int index)
    {
        Column = column;
        PropertyName = propertyName;
        Index = index;
    }

    /// <summary>
    /// Gets the column that changed.
    /// </summary>
    public TableViewColumn Column { get; }

    /// <summary>
    /// Gets the name of the property that changed.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the index of the column in the collection.
    /// </summary>
    public int Index { get; }
}
