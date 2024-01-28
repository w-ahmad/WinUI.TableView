using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace WinUI3.TableView;

public class TableViewComboBoxColumn : TableViewBoundColumn
{
    internal override FrameworkElement GenerateElement()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 0, 0)
        };

        textBlock.SetBinding(TextBlock.TextProperty, Binding);

        return textBlock;
    }

    internal override FrameworkElement GenerateEditingElement()
    {
        var comboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        comboBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = this, Path = new PropertyPath(nameof(ItemsSource)) });
        comboBox.SetBinding(Selector.SelectedItemProperty, Binding);

        return comboBox;
    }

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TableViewComboBoxColumn), new PropertyMetadata(null));
}
