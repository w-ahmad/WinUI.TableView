using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace WinUI.TableView.Converters
{
    public class EqualityToVisibilityConverter : IValueConverter
    {
        public object ExpectedValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Equals(value, ExpectedValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
