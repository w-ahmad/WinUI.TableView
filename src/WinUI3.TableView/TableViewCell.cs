using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;

namespace WinUI3.TableView;

public class TableViewCell : ContentControl
{
    private TableViewRowPresenter? _tableViewRow;
    private TableViewColumn? _column;
    private Lazy<FrameworkElement> _element = null!;
    private Lazy<FrameworkElement> _editingElement = null!;

    public TableViewCell()
    {
        DefaultStyleKey = typeof(TableViewCell);
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        if (_column is { IsReadOnly: false })
        {
            PrepareForEdit();
        }
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        _tableViewRow ??= this.FindAscendant<TableViewRowPresenter>();

        if (e.Key is VirtualKey.Tab or VirtualKey.Enter && _tableViewRow is not null && !IsAnyFlyoutOpen(this))
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
            OnEdititingElementLostFocus(Content ?? ContentTemplateRoot, default!);
        }
    }

    internal async void PrepareForEdit()
    {
        SetEditingElement();

        await Task.Delay(20);

        if ((Content ?? ContentTemplateRoot) is UIElement editingElement)
        {
            editingElement.Focus(FocusState.Programmatic);
            editingElement.LostFocus += OnEdititingElementLostFocus;
        }
    }

    private async void OnEdititingElementLostFocus(object sender, RoutedEventArgs e)
    {
        await Task.Delay(20);

        if (sender is FrameworkElement element && IsAnyFlyoutOpen(element))
        {
            return;
        }

        SetElement();

        ((UIElement)sender).LostFocus -= OnEdititingElementLostFocus;
    }

    private static bool IsAnyFlyoutOpen(FrameworkElement element)
    {
        if (FlyoutBase.GetAttachedFlyout(element) is { IsOpen: true })
        {
            return true;
        }

        foreach (var child in element.FindDescendants().OfType<FrameworkElement>())
        {
            if (FlyoutBase.GetAttachedFlyout(child) is { IsOpen: true })
            {
                return true;
            }
        }

        return false;
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

    private static void OnColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewCell cell && e.NewValue is TableViewColumn column)
        {
            cell._column = column;
            cell._element = new Lazy<FrameworkElement>(column.GenerateElement());
            cell._editingElement = new Lazy<FrameworkElement>(column.GenerateEditingElement());
            cell.SetElement();
        }

    }

    public bool IsReadOnly => _column is TableViewTemplateColumn { EditingTemplate: null } || _column is { IsReadOnly: true };

    public TableViewColumn Column
    {
        get { return (TableViewColumn)GetValue(ColumnProperty); }
        set { SetValue(ColumnProperty, value); }
    }

    public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register(nameof(Column), typeof(TableViewColumn), typeof(TableViewCell), new PropertyMetadata(default, OnColumnChanged));
}