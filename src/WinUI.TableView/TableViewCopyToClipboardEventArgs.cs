using System.ComponentModel;

namespace WinUI.TableView;

public class TableViewCopyToClipboardEventArgs : HandledEventArgs
{
    public TableViewCopyToClipboardEventArgs(bool includeHeaders)
    {
        IncludeHeaders = includeHeaders;
    }

    public bool IncludeHeaders { get; }
}