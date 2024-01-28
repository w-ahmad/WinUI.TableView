using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;

namespace WinUI3.TableView;

public class TableViewCell : ContentControl
{
    private TableViewRow? _tableViewRow;
    private TableViewColumn? _column;
    private Lazy<FrameworkElement> _element = null!;
    private Lazy<FrameworkElement> _editingElement = null!;

    public TableViewCell()
    {
        DefaultStyleKey = typeof(TableViewCell);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _column = (TableViewColumn)DataContext;
        _tableViewRow = this.FindAscendant<TableViewRow>();
        SetBinding(DataContextProperty, new Binding { Path = new PropertyPath(nameof(DataContext)), Source = _tableViewRow });

        if (_column is not null)
        {
            _element = new Lazy<FrameworkElement>(_column.GenerateElement());
            _editingElement = new Lazy<FrameworkElement>(_column.GenerateEditingElement());
        }

        if (_column is TableViewBoundColumn boundColumn)
        {
            SetBinding(ValueProperty, boundColumn.Binding);
        }

        SetElement();
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        if (_column is { IsReadOnly: false })
        {
            PrepareForEdit();
        }
    }

    internal async void PrepareForEdit()
    {
        SetEditingElement();

        await Task.Delay(20);

        _editingElement.Value.Focus(FocusState.Programmatic);
        _editingElement.Value.KeyDown += OnEdititingElementKeyDown;
        _editingElement.Value.LostFocus += OnEdititingElementLostFocus;
    }

    private void OnEdititingElementKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Tab or VirtualKey.Enter && _tableViewRow is not null)
        {
            var shiftKey = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            var isShiftKeyDown = shiftKey is CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);

            if (isShiftKeyDown)
            {
                _tableViewRow.SelectPreviousCell(this);
            }
            else
            {
                _tableViewRow.SelectNextCell(this);
            }
        }
        else if (e.Key == VirtualKey.Escape)
        {
            OnEdititingElementLostFocus(default!, default!);
        }
    }

    private async void OnEdititingElementLostFocus(object sender, RoutedEventArgs e)
    {
        await Task.Delay(20);

        if (sender is ComboBox comboBox && comboBox.IsDropDownOpen)
        {
            return;
        }

        SetElement();
        _editingElement!.Value.LostFocus -= OnEdititingElementLostFocus;
        _editingElement.Value.KeyDown -= OnEdititingElementKeyDown;
    }

    private void SetElement()
    {
        if (_column is TableViewTemplateColumn templateColumn)
        {
            ContentTemplate = templateColumn.CellTemplate;
        }
        else
        {
            Content = _element.Value;
        }
    }

    private void SetEditingElement()
    {
        if (_column is TableViewTemplateColumn templateColumn)
        {
            ContentTemplate = templateColumn.EditingTemplate ?? templateColumn.CellTemplate;
        }
        else if (_column is not null)
        {
            Content = _editingElement.Value;
        }
    }

    public bool IsReadOnly => _column is { IsReadOnly: true };

    public object Value
    {
        get => GetValue(ValueProperty);
        private set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(TableViewCell), new PropertyMetadata(default));
}