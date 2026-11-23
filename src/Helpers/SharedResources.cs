using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides shared resources for the TableView control, such as brushes that are used across multiple cells and row headers.
/// </summary>
internal static class SharedResources
{
    /// <summary>
    /// Gets a transparent brush for hidden grid lines. It's per thread because XAML objects can't be used from a UI
    /// thread other than the one that created them.
    /// </summary>
    [field: ThreadStatic]
    internal static SolidColorBrush TransparentBrush => field ??= new(Colors.Transparent);
}
