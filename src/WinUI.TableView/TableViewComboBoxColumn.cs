using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView;

public class TableViewComboBoxColumn : TableViewBoundColumn
{
    public override FrameworkElement GenerateElement()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 0),
            TextTrimming = TextTrimming.Clip
        };
        textBlock.SetBinding(TextBlock.TextProperty, Binding);
        return textBlock;
    }

    public override FrameworkElement GenerateEditingElement()
    {
        var comboBox = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        comboBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = this, Path = new PropertyPath(nameof(ItemsSource)) });
        comboBox.SetBinding(Selector.SelectedValuePathProperty, new Binding { Source = this, Path = new PropertyPath(nameof(SelectedValuePath)) });
        comboBox.SetBinding(ItemsControl.DisplayMemberPathProperty, new Binding { Source = this, Path = new PropertyPath(nameof(DisplayMemberPath)) });
        comboBox.SetBinding(Selector.SelectedItemProperty, Binding);

        return comboBox;
    }

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string SelectedValuePath
    {
        get => (string)GetValue(SelectedValuePathProperty);
        set => SetValue(SelectedValuePathProperty, value);
    }

    public static readonly DependencyProperty SelectedValuePathProperty = DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));
}
