using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;

namespace WinUI.TableView;

public class TableViewCell : ContentControl
{
    private Lazy<FrameworkElement> _element = null!;
    private Lazy<FrameworkElement> _editingElement = null!;

    public TableViewCell()
    {
        DefaultStyleKey = typeof(TableViewCell);
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        if (!IsReadOnly)
        {
            PrepareForEdit();
        }
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is VirtualKey.Tab or VirtualKey.Enter &&
            Row is not null &&
            !VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any())
        {
            var shiftKey = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            var isShiftKeyDown = shiftKey is CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);

            if (isShiftKeyDown)
            {
                Row.SelectPreviousCell(this);
            }
            else
            {
                Row.SelectNextCell(this);
            }
        }
        else if (e.Key == VirtualKey.Escape)
        {
            if (!VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any())
            {
                SetElement();
            }
        }
    }

    internal async void PrepareForEdit()
    {
        SetEditingElement();

        await Task.Delay(20);

        if ((Content ?? ContentTemplateRoot) is UIElement editingElement)
        {
            editingElement.Focus(FocusState.Programmatic);
        }
    }

    protected override async void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        await Task.Delay(20);

        if (VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any())
        {
            return;
        }

        SetElement();
    }

    private void SetElement()
    {
        if (Column is TableViewTemplateColumn templateColumn)
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
        if (Column is TableViewTemplateColumn templateColumn)
        {
            ContentTemplate = templateColumn.EditingTemplate ?? templateColumn.CellTemplate;
        }
        else if (Column is not null)
        {
            Content = _editingElement.Value;
        }
    }

    private static void OnColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewCell cell && e.NewValue is TableViewColumn column)
        {
            cell._element = new Lazy<FrameworkElement>(column.GenerateElement());
            cell._editingElement = new Lazy<FrameworkElement>(column.GenerateEditingElement());
            cell.SetElement();
        }
    }

    public bool IsReadOnly => TableView.IsReadOnly || Column is TableViewTemplateColumn { EditingTemplate: null } or { IsReadOnly: true };

    public TableViewColumn Column
    {
        get => (TableViewColumn)GetValue(ColumnProperty);
        set => SetValue(ColumnProperty, value);
    }

    public TableViewRow Row
    {
        get => (TableViewRow)GetValue(TableViewRowProperty);
        set => SetValue(TableViewRowProperty, value);
    }

    public TableView TableView
    {
        get => (TableView)GetValue(TableViewProperty);
        set => SetValue(TableViewProperty, value);
    }

    public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register(nameof(Column), typeof(TableViewColumn), typeof(TableViewCell), new PropertyMetadata(default, OnColumnChanged));
    public static readonly DependencyProperty TableViewRowProperty = DependencyProperty.Register(nameof(Row), typeof(TableViewRow), typeof(TableViewCell), new PropertyMetadata(default));
    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewCell), new PropertyMetadata(default));
}