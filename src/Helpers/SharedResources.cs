using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides shared resources for the TableView control, such as brushes and bindings that are used across multiple cells and row headers.
/// </summary>
internal static class SharedResources
{
    /// <summary>
    /// Gets a transparent brush for hidden grid lines. It's per thread because XAML objects can't be used from a UI
    /// thread other than the one that created them.
    /// </summary>
    [field: ThreadStatic]
    internal static SolidColorBrush TransparentBrush => field ??= new(Colors.Transparent);

    /// <summary>
    /// Gets a binding to <see cref="FontFamily"/>, shared by every cell and row header.
    /// </summary>
    [field: ThreadStatic]
    internal static Binding FontFamilyBinding => field ??= new Binding { Path = new("TableView.FontFamily"), RelativeSource = new() { Mode = RelativeSourceMode.Self } };

    /// <summary>
    /// Gets a binding to <see cref="FontSize"/>, shared by every cell and row header.
    /// </summary>
    [field: ThreadStatic]
    internal static Binding FontSizeBinding => field ??= new Binding { Path = new("TableView.FontSize"), RelativeSource = new() { Mode = RelativeSourceMode.Self } };

    /// <summary>
    /// Gets a binding to <see cref="RowHeight"/>, shared by every cell and row header.
    /// </summary>
    [field: ThreadStatic]
    internal static Binding RowHeightBinding => field ??= new Binding { Path = new("TableView.RowHeight"), RelativeSource = new() { Mode = RelativeSourceMode.Self } };

    /// <summary>
    /// Gets a binding to <see cref="RowMinHeight"/>, shared by every cell and row header.
    /// </summary>
    [field: ThreadStatic] 
    internal static Binding RowMinHeightBinding => field ??= new Binding { Path = new("TableView.RowMinHeight"), RelativeSource = new() { Mode = RelativeSourceMode.Self } };

    /// <summary>
    /// Gets a binding to <see cref="RowMaxHeight"/>, shared by every cell and row header.
    /// </summary>
    [field: ThreadStatic] 
    internal static Binding RowMaxHeightBinding => field ??= new Binding { Path = new("TableView.RowMaxHeight"), RelativeSource = new() { Mode = RelativeSourceMode.Self } };

    /// <summary>
    /// Gets a binding to the column header, shared by every column header.
    /// </summary>
    [field: ThreadStatic]
    internal static Binding HeaderBinding => field ??= new Binding { Path = new(nameof(TableViewColumn.Header)) };
}
