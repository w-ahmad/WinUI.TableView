using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Provides data for the CopyToClipboard event.
/// </summary>
public class TableViewCopyToClipboardEventArgs : HandledEventArgs
{
    /// <summary>
    /// Initializes a new instance of the TableViewCopyToClipboardEventArgs class.
    /// </summary>
    /// <param name="includeHeaders">A value indicating whether to include headers in the copied content.</param>
    public TableViewCopyToClipboardEventArgs(bool includeHeaders)
    {
        IncludeHeaders = includeHeaders;
    }

    /// <summary>
    /// Gets a value indicating whether to include headers in the copied content.
    /// </summary>
    public bool IncludeHeaders { get; }
}
