using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Interface for handling column filtering in a TableView.
/// </summary>
public interface IColumnFilterHandler
{
    /// <summary>
    /// Gets or sets the selected values for the filter per column.
    /// </summary>
    IDictionary<TableViewColumn, IList<object>> SelectedValues { get; }

    /// <summary>
    /// Get the filter items for the specified column.
    /// </summary>
    /// <param name="column">The column for which to prepare filter items.</param>
    /// <param name="searchText">The search text to filter the items.</param>
    IList<TableViewFilterItem> GetFilterItems(TableViewColumn column, string? searchText);

    /// <summary>
    /// Applies the filter to the specified column.
    /// </summary>
    /// <param name="column">The column to which the filter is applied.</param>
    void ApplyFilter(TableViewColumn column);

    /// <summary>
    /// Clears the filter from the specified column.
    /// </summary>
    /// <param name="column">The column from which the filter is cleared.</param>
    void ClearFilter(TableViewColumn? column);

    /// <summary>
    /// Determines whether the specified item passes the filter for the specified column.
    /// </summary>
    /// <param name="column">The column for which the filter is applied.</param>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the item passes the filter; otherwise, false.</returns>
    bool Filter(TableViewColumn column, object? item);

    /// <summary>
    /// UNIFIED: Applies fast sorting to the specified column with the given direction.
    /// </summary>
    /// <param name="column">The column to sort.</param>
    /// <param name="direction">The sort direction.</param>
    void ApplyUnifiedSort(TableViewColumn column, SortDirection direction);

    /// <summary>
    /// UNIFIED: Clears fast sorting from the specified column.
    /// </summary>
    /// <param name="column">The column to clear sorting from.</param>
    void ClearUnifiedSort(TableViewColumn column);
}
