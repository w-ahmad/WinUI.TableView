using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Hides the cells and column headers that are scrolled out of the horizontal viewport from the RENDERER, without
/// touching layout.
/// </summary>
/// <remarks>
/// <para>Every row realises a cell for every visible column, in a <see cref="StackPanel"/> that is as wide as all
/// the columns together and clipped to the viewport. On Uno's Skia renderer (6.7) a frame walks EVERY visual in the
/// tree and computes a clip and damage path for each one (<c>Visual.Render</c>, <c>TryGetPaintDamageRegion</c>); it
/// does not skip a visual that lies wholly outside its clip. A 177-column table therefore paid for ~1,239 cells and
/// their template parts on every frame while 15 of its columns were on screen: 0.3-1.5 s per frame on a desktop,
/// and 8-10 s to swap the table out of the page.</para>
/// <para>The renderer DOES skip a visual whose opacity is 0, with its whole subtree (<c>IsRenderSuppressed</c>), and
/// opacity is not a layout property. So an off-screen cell is given opacity 0 and keeps its size and position: the
/// panel measures and arranges exactly as before, a column resize or a scroll re-runs the same arrange, and the cell
/// is given back its own opacity (the local value is cleared) as soon as any part of it comes within
/// <see cref="Margin"/> of the viewport.</para>
/// <para>The viewport is the TABLE's width. A row is arranged as wide as all its cells, so its own arranged size says
/// nothing about what is on screen; the table's <c>ActualWidth</c> is 0 on the first pass (nothing is culled then) and
/// changes without re-arranging rows whose size did not, so <c>TableView.RefreshCulling</c> re-arranges the header row
/// and every row whenever the table's width changes.</para>
/// <para>WinAppSDK's compositor culls clipped content on its own, so there this saves little; it runs on every
/// platform anyway, so there is one code path and the library's own (WinAppSDK) tests exercise it.</para>
/// </remarks>
internal static class HorizontalCulling
{
    /// <summary>How far outside the viewport, in DIPs, a cell is still drawn, so a small scroll never shows a gap.</summary>
    internal const double Margin = 240;

    private static readonly DependencyProperty IsCulledProperty = DependencyProperty.RegisterAttached(
        "IsCulled", typeof(bool), typeof(HorizontalCulling), new PropertyMetadata(false));

    /// <summary>
    /// Whether an element spanning <paramref name="left"/> .. <paramref name="left"/> + <paramref name="width"/> (in
    /// the panel's own coordinates) comes within <paramref name="margin"/> of the viewport, which starts at
    /// <paramref name="viewLeft"/> in those coordinates and is <paramref name="viewWidth"/> wide.
    /// </summary>
    internal static bool IsInView(double left, double width, double viewLeft, double viewWidth, double margin = Margin) =>
        left + width >= viewLeft - margin && left <= viewLeft + viewWidth + margin;

    /// <summary>Whether <paramref name="element"/> is currently hidden from the renderer by <see cref="Apply"/>.</summary>
    internal static bool IsCulled(UIElement element) => (bool)element.GetValue(IsCulledProperty);

    /// <summary>
    /// Shows every child of <paramref name="panel"/> that is in view and hides the rest. <paramref name="viewLeft"/> is
    /// where the viewport starts in the panel's coordinates; a viewport that has no width yet (not laid out) culls
    /// nothing. Returns how many children are culled.
    /// </summary>
    internal static int Apply(Panel? panel, double viewLeft, double viewWidth, bool enabled = true)
    {
        if (panel is null)
            return 0;

        var cull = enabled && viewWidth > 0 && !double.IsNaN(viewLeft) && !double.IsInfinity(viewWidth);
        var culled = 0;
        foreach (var child in panel.Children)
        {
            if (child is not FrameworkElement element)
                continue;

            var hide = cull && element.ActualWidth > 0 && !IsInView(element.ActualOffset.X, element.ActualWidth, viewLeft, viewWidth);
            if (hide)
                culled++;
            Set(element, hide);
        }

        return culled;
    }

    /// <summary>Gives every child of <paramref name="panel"/> back to the renderer.</summary>
    internal static void Reset(Panel? panel)
    {
        if (panel is null)
            return;
        foreach (var child in panel.Children)
            Set(child, false);
    }

    private static void Set(UIElement element, bool hide)
    {
        if (IsCulled(element) == hide)
            return;

        if (hide)
        {
            element.SetValue(IsCulledProperty, true);
            element.Opacity = 0;
        }
        else
        {
            element.ClearValue(IsCulledProperty);
            element.ClearValue(UIElement.OpacityProperty);
        }
    }
}
