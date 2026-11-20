#if !WINDOWS
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Reaches the composition visuals of elements on a renderer that composes them itself, which is Uno's Skia renderer.
/// </summary>
/// <remarks>
/// That renderer walks every element of every realized row on every frame, draws it, and works out the screen area it
/// repaints; while a table scrolls, all of them move. What is left out of that walk, or made cheaper for it, is most of
/// what a scrolling frame costs there.
/// </remarks>
internal static class ElementVisuals
{
    private static bool? s_areComposed;

    /// <summary>
    /// Returns the visual that draws an element, or <see langword="null"/> when the renderer only hands back a stand-in
    /// for it, as Uno's native renderers do.
    /// </summary>
    internal static ContainerVisual? Get(UIElement element)
    {
        if (s_areComposed is false)
        {
            return null;
        }

        var visual = ElementCompositionPreview.GetElementVisual(element) as ContainerVisual;
        s_areComposed ??= visual is not null;

        return visual;
    }

    /// <summary>
    /// Gets a value indicating whether elements are known not to have visuals of their own.
    /// </summary>
    internal static bool AreKnownNotComposed => s_areComposed is false;

    /// <summary>
    /// Leaves an element out of rendering while it is out of view, the way the renderer leaves out a collapsed element,
    /// without changing its layout.
    /// </summary>
    internal static void SetInView(UIElement element, bool isInView)
    {
        if (Get(element) is not { } visual)
        {
            return;
        }

        // The renderer shows the element's own opacity, and nothing while it is collapsed; it sets this again whenever
        // either changes.
        var opacity = isInView && element.Visibility is Visibility.Visible ? (float)element.Opacity : 0f;

        if (visual.Opacity != opacity)
        {
            visual.Opacity = opacity;
        }
    }
}
#endif
