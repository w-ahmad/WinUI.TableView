namespace WinUI.TableView;

/// <summary>
/// Represents a sort description for a specific column in TableView.
/// </summary>
internal class ColumnSortDescription : SortDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnSortDescription"/> class.
    /// </summary>
    /// <param name="column">The column to sort.</param>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <param name="direction">The direction of the sort.</param>
    public ColumnSortDescription(TableViewColumn column,
                                 string? propertyName,
                                 SortDirection direction)
        : base(propertyName!, direction, null, null)
    {
        Column = column;
    }

    public override object? GetPropertyValue(object? item)
    {
        return Column.GetCellContent(item);
    }

    /// <summary>
    /// Gets the column associated with this sort description.
    /// </summary>
    public TableViewColumn Column { get; }
}
