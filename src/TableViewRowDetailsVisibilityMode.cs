namespace WinUI.TableView;

/// <summary>
/// Defines constants that specify the visibility of row details in a <see cref="TableView"/>.
/// </summary>
public enum TableViewRowDetailsVisibilityMode
{
    /// <summary>
    /// Row details are never displayed.
    /// </summary>
    Collapsed,

    /// <summary>
    /// Row details are always displayed for all rows.
    /// </summary>
    Visible,

    /// <summary>
    /// Row details are displayed only for the selected row or rows.
    /// </summary>
    VisibleWhenSelected,

    /// <summary>
    /// Row details are displayed only when the user expands a row using the toggle button in the row header.
    /// </summary>
    VisibleWhenExpanded
}