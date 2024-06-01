using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace WinUI.TableView;

public class TableViewCheckBoxColumn : TableViewBoundColumn
{
    public override FrameworkElement GenerateElement()
    {
        var checkBox = new CheckBox
        {
            MinWidth = 20,
            MaxWidth = 20,
            IsEnabled = !IsReadOnly,
            Margin = new Thickness(12, 0, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        checkBox.SetBinding(ToggleButton.IsCheckedProperty, Binding);

        return checkBox;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        return GenerateElement();
    }
}
