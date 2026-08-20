namespace WinUI.TableView;

/// <summary>
/// A <see cref="GroupDescription"/> created for grouping by a <see cref="TableViewColumn"/> from its header's
/// "Group" command - grouping by the column's bound property (or <see cref="TableViewColumn.SortMemberPath"/>
/// when set) if it has one, otherwise by its rendered cell content.
/// </summary>
internal class ColumnGroupDescription : GroupDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnGroupDescription"/> class.
    /// </summary>
    /// <param name="column">The column being grouped by.</param>
    /// <param name="propertyName">The property to group by, or <see langword="null"/> to group by the column's rendered cell content.</param>
    /// <param name="direction">The direction groups are ordered in.</param>
    public ColumnGroupDescription(TableViewColumn column,
                                  string? propertyName,
                                  SortDirection direction = SortDirection.Ascending)
        : base(propertyName, direction)
    {
        Column = column;
    }

    /// <inheritdoc/>
    public override object? GetPropertyValue(object? item)
    {
        // Use reflection-based property access when SortMemberPath is explicitly provided; otherwise, fall back to column cell content.
        if (!string.IsNullOrEmpty(Column.SortMemberPath))
        {
            return base.GetPropertyValue(item);
        }
        return Column.GetCellContent(item);
    }

    /// <summary>
    /// Gets the column associated with this group description.
    /// </summary>
    public TableViewColumn Column { get; }
}
