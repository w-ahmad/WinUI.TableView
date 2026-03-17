using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for row expansion state changes in hierarchical mode.
/// </summary>
public sealed class TableViewRowExpansionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowExpansionChangedEventArgs"/> class.
    /// </summary>
    /// <param name="item">The row item whose expansion state changed.</param>
    /// <param name="index">The row index at the time of the change.</param>
    /// <param name="isExpanded">A value indicating whether the row is expanded.</param>
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
    /// Gets the row index at the time the event was raised.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets a value indicating whether the row is expanded.
    /// </summary>
    public bool IsExpanded { get; }
}