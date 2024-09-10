using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace WinUI.TableView;

public class TableViewCheckBoxColumn : TableViewBoundColumn
{
    public TableViewCheckBoxColumn()
    {
        UseSingleElement = true;
    }

    public override FrameworkElement GenerateElement()
    {
        var checkBox = new CheckBox
        {
            MinWidth = 20,
            MaxWidth = 20,
            Margin = new Thickness(12, 0, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            UseSystemFocusVisuals = false,
        };

        checkBox.SetBinding(ToggleButton.IsCheckedProperty, Binding);
        UpdateCheckBoxState(checkBox);

        return checkBox;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        throw new NotImplementedException();
    }

    public override void UpdateElementState(TableViewCell cell)
    {
        if (cell?.Content is CheckBox checkBox)
        {
            UpdateCheckBoxState(checkBox);
        }
    }

    private void UpdateCheckBoxState(CheckBox checkBox)
    {
        checkBox.IsHitTestVisible = TableView?.IsReadOnly is false && !IsReadOnly;
    }
}
