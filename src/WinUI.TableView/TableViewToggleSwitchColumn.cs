using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

public class TableViewToggleSwitchColumn : TableViewBoundColumn
{
    public override FrameworkElement GenerateElement()
    {
        var toggleSwitch = new ToggleSwitch
        {
            OnContent = OnContent,
            OffContent = OffContent,
            IsEnabled = !IsReadOnly,
            Margin = new Thickness(12, 0, 12, 0)
        };

        toggleSwitch.SetBinding(ToggleSwitch.IsOnProperty, Binding);

        return toggleSwitch;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        return GenerateElement();
    }

    public object OnContent
    {
        get => GetValue(OnContentProperty);
        set => SetValue(OnContentProperty, value);
    }
    public object OffContent
    {
        get => GetValue(OffContentProperty);
        set => SetValue(OffContentProperty, value);
    }

    public static readonly DependencyProperty OnContentProperty = DependencyProperty.Register(nameof(OnContent), typeof(object), typeof(TableViewToggleSwitchColumn), new PropertyMetadata(null));
    public static readonly DependencyProperty OffContentProperty = DependencyProperty.Register(nameof(OffContent), typeof(object), typeof(TableViewToggleSwitchColumn), new PropertyMetadata(null));

}
