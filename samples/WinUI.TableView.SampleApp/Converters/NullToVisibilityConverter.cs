using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.SampleApp.Converters;
public class NullToVisibilityConverter : IValueConverter
{
    public Visibility NullValue { get; set; } = Visibility.Collapsed;
    public Visibility NonNullValue { get; set; } = Visibility.Visible;


    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is null ? NullValue : NonNullValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
