using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace WinUI.TableView.SampleApp.Controls;

/// <summary>
/// Describes a textual substitution in sample content.
/// If enabled (default), then $(Key) is replaced with the stringified value.
/// If disabled, then $(Key) is replaced with the empty string.
/// </summary>
public sealed partial class CodeSubstitution : DependencyObject
{
    public event TypedEventHandler<CodeSubstitution, object?>? ValueChanged;

    public string? Key { get; set; }

    public object? Value
    {
        get;
        set
        {
            field = value;
            ValueChanged?.Invoke(this, null);
        }
    }

    public bool IsEnabled
    {
        get;
        set
        {
            field = value;
            ValueChanged?.Invoke(this, null);
        }
    } = true;

    public string? ValueAsString()
    {
        if (!IsEnabled)
        {
            return string.Empty;
        }

        var value = Value;

        // For solid color brushes, use the underlying color.
        if (value is SolidColorBrush brush)
        {
            value = brush.Color;
        }

        return value?.ToString() ?? string.Empty;
    }
}