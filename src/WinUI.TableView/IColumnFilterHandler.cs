using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Interface for handling column filtering in a TableView.
/// </summary>
public interface IColumnFilterHandler
{
    /// <summary>
    /// Gets or sets the filter items.
    /// </summary>
    IList<TableViewFilterItem> FilterItems { get; set; }

    /// <summary>
    /// Gets or sets the selected values for the filter.
    /// </summary>
    IList<object> SelectedValues { get; set; }

    /// <summary>
    /// Prepares the filter items for the specified column.
    /// </summary>
    /// <param name="column">The column for which to prepare filter items.</param>
    /// <param name="searchText">The search text to filter the items.</param>
    void PrepareFilterItems(TableViewColumn column, string? searchText = default);

    /// <summary>
    /// Handles the search text changed event for the specified column.
    /// </summary>
    /// <param name="column">The column for which the search text has changed.</param>
    /// <param name="searchText">The new search text.</param>
    void SearchTextChanged(TableViewColumn column, string? searchText);

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
}
