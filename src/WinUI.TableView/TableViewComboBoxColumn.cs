using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView;

public class TableViewComboBoxColumn : TableViewBoundColumn
{
    private Binding? _textBinding;
    private Binding? _selectedValueBinding;

    public override FrameworkElement GenerateElement()
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 0),
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
        comboBox.SetBinding(ComboBox.IsEditableProperty, new Binding { Source = this, Path = new PropertyPath(nameof(IsEditable)) });

        if (TextBinding is not null)
        {
            comboBox.SetBinding(ComboBox.TextProperty, TextBinding);
        }

        if (SelectedValueBinding is not null)
        {
            comboBox.SetBinding(Selector.SelectedValueProperty, SelectedValueBinding);
        }

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

    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    public virtual Binding TextBinding
    {
        get => _textBinding!;
        set
        {
            _textBinding = value;
            if (_textBinding is not null)
            {
                _textBinding.Mode = BindingMode.TwoWay;
            }
        }
    }

    public virtual Binding SelectedValueBinding
    {
        get => _selectedValueBinding!;
        set
        {
            _selectedValueBinding = value;
            if (_selectedValueBinding is not null)
            {
                _selectedValueBinding.Mode = BindingMode.TwoWay;
            }
        }
    }

    public static readonly DependencyProperty SelectedValuePathProperty = DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));
    public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(TableViewComboBoxColumn), new PropertyMetadata(false));
}
