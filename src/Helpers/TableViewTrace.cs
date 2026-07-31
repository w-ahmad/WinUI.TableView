using System.Diagnostics;

namespace WinUI.TableView.Helpers;

internal static class TableViewTrace
{
    [Conditional("DEBUG")]
    public static void Write(string message)
    {
        if (Debugger.IsAttached)
        {
            Debug.WriteLine($"[TableView] {message}"); 
        }
    }
}