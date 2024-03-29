﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace WinUI.TableView;

public class TableViewCheckBoxColumn : TableViewBoundColumn
{
    internal override FrameworkElement GenerateElement()
    {
        var checkBox = new CheckBox
        {
            MinWidth = 20,
            MaxWidth = 20,
            IsEnabled = !IsReadOnly,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        checkBox.SetBinding(ToggleButton.IsCheckedProperty, Binding);

        return checkBox;
    }

    internal override FrameworkElement GenerateEditingElement()
    {
        return GenerateElement();
    }
}
