using System.Diagnostics;

namespace WinUI.TableView.Helpers;

internal static class TableViewTrace
{
    [Conditional("DEBUG")]
    public static void Write(string message)
    {
        Debug.WriteLine($"[TableView] {message}");
    }
}