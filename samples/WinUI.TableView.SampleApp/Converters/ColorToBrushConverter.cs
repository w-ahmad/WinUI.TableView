using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Converters;

public class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value is SolidColorBrush brush ? brush.Color : default;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Color color ? new SolidColorBrush(color) : default;
    }
}
