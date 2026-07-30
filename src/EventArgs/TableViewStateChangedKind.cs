namespace WinUI.TableView;

/// <summary>
/// Identifies the kind of change that raised a <see cref="TableView.StateChanged"/> event.
/// </summary>
public enum TableViewStateChangedKind
{
    /// <summary>
    /// One or more sort descriptions were added, removed, or cleared.
    /// </summary>
    Sort,

    /// <summary>
    /// One or more filter descriptions were added, removed, or cleared.
    /// </summary>
    Filter,

    /// <summary>
    /// A column was reordered, or its width or visibility changed.
    /// </summary>
    Column,
}
