using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace WinUI.TableView;

public class TableViewToggleSwitchColumn : TableViewBoundColumn
{
    public TableViewToggleSwitchColumn()
    {
        UseSingleElement = true;
    }

    public override FrameworkElement GenerateElement()
    {
        var toggleSwitch = new ToggleSwitch
        {
            OnContent = OnContent,
            OffContent = OffContent,
            UseSystemFocusVisuals = false,
            Margin = new Thickness(12, 0, 12, 0)
        };

        toggleSwitch.SetBinding(ToggleSwitch.IsOnProperty, Binding);
        UpdateToggleButtonState(toggleSwitch);

        return toggleSwitch;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        throw new NotImplementedException();
    }

    public override void UpdateElementState(TableViewCell cell)
    {
        if (cell?.Content is ToggleSwitch toggleSwitch)
        {
            UpdateToggleButtonState(toggleSwitch);
        }
    }

    private void UpdateToggleButtonState(ToggleSwitch toggleSwitch)
    {
        toggleSwitch.IsHitTestVisible = TableView?.IsReadOnly is false && !IsReadOnly;
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
