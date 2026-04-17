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

    /// <summary>
    /// Gets the sort value for the specified item, preferring the TableView member value provider when available.
    /// </summary>
    /// <param name="item">The item to extract a sort value from.</param>
    /// <returns>The resolved sort value, or <see langword="null"/> when no value is available.</returns>
    public override object? GetPropertyValue(object? item)
    {
        // Use reflection-based property access when SortMemberPath is explicitly provided; otherwise, fall back to column cell content.
        if (!string.IsNullOrEmpty(Column.SortMemberPath))
        {
            if (Column.TableView?.MemberValueProvider is { } provider
                && provider.TryGetSortMemberValue(Column.SortMemberPath, item, out var value))
            {
                return value;
            }

            return base.GetPropertyValue(item);
        }

        return Column.GetCellContent(item);
    }

    /// <summary>
    /// Gets the column associated with this sort description.
    /// </summary>
    public TableViewColumn Column { get; }
}
