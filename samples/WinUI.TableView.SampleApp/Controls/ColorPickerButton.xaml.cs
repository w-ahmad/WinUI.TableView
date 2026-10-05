using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinUI.TableView.SampleApp.Controls;

/// <summary>
/// A drop-down button that shows <see cref="SelectedBrush"/> and lets the user change its color with a <see cref="ColorPicker"/>.
/// </summary>
/// <remarks>
/// Used instead of the CommunityToolkit <c>ColorPickerButton</c>, whose resources fail to load when the app is
/// published with Native AOT. It works with a <see cref="Brush"/> rather than a <see cref="Windows.UI.Color"/> so it
/// binds straight to brush properties: a boxed <c>Color</c> passed through a converter can't be marshaled under Native AOT.
/// </remarks>
public sealed partial class ColorPickerButton : UserControl
{
    public ColorPickerButton()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the selected brush.
    /// </summary>
    public Brush? SelectedBrush
    {
        get => (Brush?)GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    // ColorPicker adjusts the colors it's given (HSV round-trip), so binding it two-way would keep updating back
    // and forth; sync the picker when the flyout opens and push its changes back instead.
    private void OnFlyoutOpening(object sender, object e)
    {
        if (SelectedBrush is SolidColorBrush brush)
        {
            colorPicker.Color = brush.Color;
        }
    }

    private void OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        SelectedBrush = new SolidColorBrush(args.NewColor);
    }

    public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(nameof(SelectedBrush), typeof(Brush), typeof(ColorPickerButton), new PropertyMetadata(null));
}
