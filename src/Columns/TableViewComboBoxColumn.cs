using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays a ComboBox.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(TextBlock))]
[StyleTypedProperty(Property = nameof(EditingElementStyle), StyleTargetType = typeof(ComboBox))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewComboBoxColumn : TableViewBoundColumn
{
    private Binding? _textBinding;
    private Binding? _selectedValueBinding;

    /// <summary>
    /// Generates a TextBlock element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A TextBlock element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 0),
        };
        textBlock.SetBinding(TextBlock.TextProperty, Binding);
        return textBlock;
    }

    /// <summary>
    /// Generates a ComboBox element for editing the cell.
    /// </summary>
    /// <param name="cell">The cell for which the editing element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A ComboBox element.</returns>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
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

    /// <summary>
    /// Gets or sets the items source for the ComboBox.
    /// </summary>
    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the path to the display member for the ComboBox.
    /// </summary>
    public string? DisplayMemberPath
    {
        get => (string?)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the path to the selected value for the ComboBox.
    /// </summary>
    public string? SelectedValuePath
    {
        get => (string?)GetValue(SelectedValuePathProperty);
        set => SetValue(SelectedValuePathProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ComboBox is editable.
    /// </summary>
    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    /// <summary>
    /// Gets or sets the binding for the text property of the ComboBox.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the binding for the selected value property of the ComboBox.
    /// </summary>
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

    /// <summary>
    /// Identifies the SelectedValuePath dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedValuePathProperty = DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the DisplayMemberPath dependency property.
    /// </summary>
    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the ItemsSource dependency property.
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(TableViewComboBoxColumn), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the IsEditable dependency property.
    /// </summary>
    public static readonly DependencyProperty IsEditableProperty = DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(TableViewComboBoxColumn), new PropertyMetadata(false));
}