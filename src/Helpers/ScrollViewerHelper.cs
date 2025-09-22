using Microsoft.UI.Xaml;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides attached properties for ScrollViewer-related functionality.
/// </summary>
public class ScrollViewerHelper
{
    /// <summary>
    /// Gets the FrozenColumnScrollBarSpace attached property. This is used to reserve space for the scrollbar in frozen columns.
    /// </summary>
    public static double GetFrozenColumnScrollBarSpace(DependencyObject obj)
    {
        return (double)obj.GetValue(FrozenColumnScrollBarSpaceProperty);
    }

    /// <summary>
    /// Sets the FrozenColumnScrollBarSpace attached property. This is used to reserve space for the scrollbar in frozen columns.
    /// </summary>
    public static void SetFrozenColumnScrollBarSpace(DependencyObject obj, double value)
    {
        obj.SetValue(FrozenColumnScrollBarSpaceProperty, value);
    }

    /// <summary>
    /// Identifies the FrozenColumnScrollBarSpace attached property.
    /// </summary>
    public static readonly DependencyProperty FrozenColumnScrollBarSpaceProperty = DependencyProperty.RegisterAttached("FrozenColumnScrollBarSpace", typeof(double), typeof(ScrollViewerHelper), new PropertyMetadata(0d));
}
