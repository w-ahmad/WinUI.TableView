using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommunityToolkit.WinUI.Animations.Expressions.ExpressionValues;

namespace WinUI.TableView.SampleApp.Converters;

public class EnumToStringConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString();
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value == null || targetType == null)
            return null;

        if (!targetType.IsEnum)
            throw new ArgumentException("Target type must be an Enum.");

        if (value is string stringValue)
        {
            try
            {
                return Enum.Parse(targetType, stringValue, ignoreCase: true);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        return null;
    }
}
