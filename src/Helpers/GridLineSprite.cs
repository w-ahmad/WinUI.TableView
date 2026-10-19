using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
#if !WINDOWS
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.UI;
#endif

namespace WinUI.TableView.Helpers;

/// <summary>
/// Draws the grid lines of rows. On Uno's Skia renderer a single-color line is a composition sprite on the element that
/// holds its place; everywhere else the element draws it.
/// </summary>
/// <remarks>
/// Uno's Skia renderer works out, on every frame, the screen area each element that moved repaints. For a shape or a
/// border that takes several path operations; a sprite only fills its bounds, so its area is a rectangle. Every row has
/// a grid line per column, and while a table scrolls every row moves. The element keeps its place in the layout, its
/// visibility and its transforms, which the sprite follows, and draws nothing itself.
/// </remarks>
internal sealed class GridLineSprite
{
    /// <summary>
    /// Fills a grid line rectangle with a brush.
    /// </summary>
    internal static void SetFill(Rectangle line, Brush? brush)
    {
        if (TryDraw(line, brush, double.NaN))
        {
            line.Fill = null;
        }
        else
        {
            line.Fill = brush;
        }
    }

    /// <summary>
    /// Draws a grid line along the bottom edge of a border.
    /// </summary>
    internal static void SetBottomEdge(Border line, Brush? brush, double thickness)
    {
        if (TryDraw(line, brush, thickness))
        {
            line.BorderBrush = null;
            line.BorderThickness = default;
        }
        else
        {
            line.BorderBrush = brush;
            line.BorderThickness = new Thickness(0, 0, 0, thickness);
        }
    }

#if WINDOWS
    /// <summary>
    /// The platform composes these elements itself, at no extra cost per frame, so the element draws the line.
    /// </summary>
    private static bool TryDraw(FrameworkElement host, Brush? brush, double bottomEdge) => false;
#else
    private static readonly ConditionalWeakTable<FrameworkElement, GridLineSprite> s_sprites = [];
    private static readonly ConditionalWeakTable<SolidColorBrush, CompositionColorBrush> s_brushes = [];

    private readonly FrameworkElement _host;
    private readonly SpriteVisual _visual;
    private double _bottomEdge = double.NaN;

    private GridLineSprite(FrameworkElement host, Compositor compositor)
    {
        _host = host;
        _visual = compositor.CreateSpriteVisual();
        ElementCompositionPreview.SetElementChildVisual(host, _visual);
        host.SizeChanged += (_, _) => UpdateBounds();
        UpdateBounds();
    }

    /// <summary>
    /// Draws the line with the element's sprite, creating it the first time, and returns whether it did. A brush other
    /// than a single color is left to the element.
    /// </summary>
    /// <param name="host">The element that holds the line's place.</param>
    /// <param name="brush">The line's brush.</param>
    /// <param name="bottomEdge">The thickness of a line along the bottom edge, or <see cref="double.NaN"/> to fill the element.</param>
    private static bool TryDraw(FrameworkElement host, Brush? brush, double bottomEdge)
    {
        if (!s_sprites.TryGetValue(host, out var sprite))
        {
            if (brush is not SolidColorBrush || ElementVisuals.Get(host) is not { } hostVisual)
            {
                return false;
            }

            sprite = new GridLineSprite(host, hostVisual.Compositor);
            s_sprites.Add(host, sprite);
        }

        if (brush is not (null or SolidColorBrush))
        {
            sprite.SetBrush(null);

            return false;
        }

        sprite.SetShape(bottomEdge);
        sprite.SetBrush(brush is SolidColorBrush solid ? GetBrush(solid, sprite._visual.Compositor) : null);

        return true;
    }

    /// <summary>
    /// Returns the composition brush that draws a color brush, shared by every line drawn with it.
    /// </summary>
    private static CompositionColorBrush GetBrush(SolidColorBrush brush, Compositor compositor)
    {
        if (s_brushes.TryGetValue(brush, out var compositionBrush) && ReferenceEquals(compositionBrush.Compositor, compositor))
        {
            return compositionBrush;
        }

        var newBrush = compositor.CreateColorBrush(GetColor(brush));
        s_brushes.AddOrUpdate(brush, newBrush);

        // The lines follow a brush that changes in place, as an element filled with it would.
        brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => newBrush.Color = GetColor(brush));
        brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => newBrush.Color = GetColor(brush));

        return newBrush;
    }

    private static Color GetColor(SolidColorBrush brush)
    {
        var color = brush.Color;
        color.A = (byte)Math.Round(color.A * Math.Clamp(brush.Opacity, 0d, 1d));

        return color;
    }

    private void SetShape(double bottomEdge)
    {
        if (!_bottomEdge.Equals(bottomEdge))
        {
            _bottomEdge = bottomEdge;
            UpdateBounds();
        }
    }

    private void SetBrush(CompositionColorBrush? brush)
    {
        if (!ReferenceEquals(_visual.Brush, brush))
        {
            _visual.Brush = brush;
        }
    }

    private void UpdateBounds()
    {
        var width = (float)_host.ActualWidth;
        var height = (float)_host.ActualHeight;

        if (double.IsNaN(_bottomEdge))
        {
            _visual.Offset = Vector3.Zero;
            _visual.Size = new Vector2(width, height);
        }
        else
        {
            // Measured from the element's own bottom, so the line stays a whole line however far down the table the
            // row is.
            var thickness = Math.Min((float)_bottomEdge, height);
            _visual.Offset = new Vector3(0f, height - thickness, 0f);
            _visual.Size = new Vector2(width, thickness);
        }
    }
#endif
}
