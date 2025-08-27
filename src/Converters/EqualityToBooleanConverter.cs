using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUI.TableView.Converters
{
    public class EqualityToBooleanConverter : IValueConverter
    {
        public object ExpectedValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Equals(value, ExpectedValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
