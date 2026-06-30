using System;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the <see cref="TableView.StateChanged"/> event.
/// </summary>
public class TableViewStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="kind">The kind of change that triggered the event.</param>
    public TableViewStateChangedEventArgs(TableViewStateChangedKind kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// Gets the kind of change that triggered the event.
    /// </summary>
    public TableViewStateChangedKind Kind { get; }
}
