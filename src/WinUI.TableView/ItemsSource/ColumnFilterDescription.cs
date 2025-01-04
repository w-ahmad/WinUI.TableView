using System;

namespace WinUI.TableView;

/// <summary>
/// Represents a filter description for a specific column in TableView.
/// </summary>
internal class ColumnFilterDescription : FilterDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnFilterDescription"/> class.
    /// </summary>
    /// <param name="column">The column to filter.</param>
    /// <param name="propertyName">The name of the property to filter by.</param>
    /// <param name="predicate">The predicate to apply for filtering.</param>
    public ColumnFilterDescription(TableViewColumn column,
                                   string? propertyName,
                                   Predicate<object?> predicate)
        : base(propertyName!, predicate)
    {
        Column = column;
    }

    /// <summary>
    /// Gets the column associated with this filter description.
    /// </summary>
    public TableViewColumn Column { get; }
}
