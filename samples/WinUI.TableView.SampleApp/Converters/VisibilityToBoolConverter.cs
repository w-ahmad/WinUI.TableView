using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.SampleApp.Converters;

public partial class VisibilityToBoolConverter : IValueConverter
{
    public bool VisibleValue { get; set; } = true;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var collapesValue = !VisibleValue;

        return value is Visibility.Visible ? VisibleValue : collapesValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
