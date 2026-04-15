using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.RowExpanded"/> and <see cref="TableView.RowCollapsed"/> events.
/// </summary>
public sealed class TableViewRowExpansionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowExpansionChangedEventArgs"/> class.
    /// </summary>
    public TableViewRowExpansionChangedEventArgs(object item, int index, bool isExpanded)
    {
        Item = item;
        Index = index;
        IsExpanded = isExpanded;
    }

    /// <summary>
    /// Gets the item whose expansion state changed.
    /// </summary>
    public object Item { get; }

    /// <summary>
    /// Gets the index of the item in the display list.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets a value indicating whether the item is now expanded.
    /// </summary>
    public bool IsExpanded { get; }
}
