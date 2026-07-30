using Microsoft.UI.Xaml;
using System.Collections.Generic;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a captured snapshot of a <see cref="TableView"/>'s sort, filter, and column
/// layout state that can be persisted and restored via <see cref="TableViewStateHelper"/>.
/// </summary>
public sealed class TableViewState
{
    /// <summary>
    /// Gets or sets the sort descriptions captured from the table view.
    /// </summary>
    public List<TableViewSortDescriptionState> SortDescriptions { get; set; } = [];

    /// <summary>
    /// Gets or sets the filter descriptions captured from the table view.
    /// </summary>
    public List<TableViewFilterDescriptionState> FilterDescriptions { get; set; } = [];

    /// <summary>
    /// Gets or sets the column layout captured from the table view.
    /// </summary>
    public List<TableViewColumnState> Columns { get; set; } = [];
}

/// <summary>
/// Represents the persisted state of a sort description.
/// </summary>
public sealed class TableViewSortDescriptionState
{
    /// <summary>
    /// Gets or sets the name of the property to sort by.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection Direction { get; set; }
}

/// <summary>
/// Represents the persisted state of a column filter.
/// </summary>
/// <remarks>
/// Only the user-selected values are persisted, not the predicate. The predicate is
/// reconstructed from the selected values when the state is applied.
/// </remarks>
public sealed class TableViewFilterDescriptionState
{
    /// <summary>
    /// Gets or sets the key that identifies the column this filter applies to.
    /// </summary>
    public string? ColumnKey { get; set; }

    /// <summary>
    /// Gets or sets the user-selected filter values, serialized as strings.
    /// A <see langword="null"/> entry represents a blank (null or empty) data value.
    /// </summary>
    public List<string?> SelectedValues { get; set; } = [];
}

/// <summary>
/// Represents the persisted layout state of a single column.
/// </summary>
public sealed class TableViewColumnState
{
    /// <summary>
    /// Gets or sets the key that identifies the column.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the header text of the column as it was when captured.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Gets or sets the visibility of the column.
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Visible;

    /// <summary>
    /// Gets or sets the zero-based display index (order) of the column.
    /// </summary>
    public int DisplayIndex { get; set; }

    /// <summary>
    /// Gets or sets the numeric value of the column width.
    /// </summary>
    public double WidthValue { get; set; }

    /// <summary>
    /// Gets or sets the unit type of the column width.
    /// </summary>
    public GridUnitType WidthUnitType { get; set; } = GridUnitType.Auto;
}
