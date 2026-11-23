using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Globalization;
using Windows.UI;
using Windows.UI.Text;

namespace WinUI.TableView.SampleApp.Showcases;

/// <summary>
/// Shared brushes for the showcase pages. Cell templates call these through x:Bind function bindings,
/// so a model only carries data and never a brush.
/// </summary>
public static class Palette
{
    public static readonly Color Blue = Color.FromArgb(255, 0, 120, 212);
    public static readonly Color Teal = Color.FromArgb(255, 0, 153, 153);
    public static readonly Color Green = Color.FromArgb(255, 16, 137, 62);
    public static readonly Color Amber = Color.FromArgb(255, 218, 139, 0);
    public static readonly Color Orange = Color.FromArgb(255, 218, 89, 15);
    public static readonly Color Red = Color.FromArgb(255, 209, 52, 56);
    public static readonly Color Pink = Color.FromArgb(255, 194, 57, 179);
    public static readonly Color Purple = Color.FromArgb(255, 135, 100, 184);
    public static readonly Color Indigo = Color.FromArgb(255, 79, 107, 237);
    public static readonly Color Gray = Color.FromArgb(255, 122, 132, 144);

    private static readonly Color[] _avatarColors = [Blue, Purple, Teal, Orange, Green, Pink, Indigo, Amber, Red, Gray];
    private static readonly Dictionary<Color, SolidColorBrush> _brushes = [];

    public static SolidColorBrush Solid(Color color)
    {
        if (!_brushes.TryGetValue(color, out var brush))
        {
            _brushes[color] = brush = new SolidColorBrush(color);
        }

        return brush;
    }

    /// <summary>
    /// A translucent version of a color, readable under text in both light and dark themes.
    /// </summary>
    public static SolidColorBrush Tint(Color color, byte alpha = 44)
    {
        return Solid(Color.FromArgb(alpha, color.R, color.G, color.B));
    }

    public static SolidColorBrush Avatar(int seed)
    {
        return Solid(_avatarColors[Math.Abs(seed) % _avatarColors.Length]);
    }
}

public static class Format
{
    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    public static string Currency(decimal value) => value.ToString("C0", _culture);

    public static string Price(double value) => value.ToString("N2", _culture);

    public static string Percent(int value) => $"{value}%";

    public static string Date(DateOnly value) => value.ToString("MMM d, yyyy", _culture);

    public static string Count(int value) => value.ToString("N0", _culture);

    public static string Compact(decimal value)
    {
        return Compact((double)value, "$");
    }

    public static string CompactNumber(double value) => Compact(value);

    public static string CompactMoney(double value) => Compact(value, "$");

    public static string Compact(double value, string prefix = "")
    {
        var abs = Math.Abs(value);

        return abs switch
        {
            >= 1_000_000_000_000 => $"{prefix}{(value / 1_000_000_000_000).ToString("0.##", _culture)}T",
            >= 1_000_000_000 => $"{prefix}{(value / 1_000_000_000).ToString("0.#", _culture)}B",
            >= 1_000_000 => $"{prefix}{(value / 1_000_000).ToString("0.#", _culture)}M",
            >= 1_000 => $"{prefix}{(value / 1_000).ToString("0.#", _culture)}K",
            _ => $"{prefix}{value.ToString("0", _culture)}"
        };
    }

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    public static Visibility VisibleIf(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility CollapsedIf(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static FontWeight BoldIf(bool value) => value ? FontWeights.SemiBold : FontWeights.Normal;
}
