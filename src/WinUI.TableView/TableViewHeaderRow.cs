using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics.CodeAnalysis;
using Windows.System;

namespace WinUI.TableView;

public class TableViewHeaderRow : ItemsControl
{
    private TableView? _tableView;
    private Button? _optionsButton;
    private CheckBox? _selectAllCheckBox;

    public TableViewHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewHeaderRow);

        InitializeCommands();
    }

    [MemberNotNull(nameof(SelectAllCommand), nameof(DeselectAllCommand), nameof(CopyCommand), nameof(CopyWithHeadersCommand))]
    private void InitializeCommands()
    {
        SelectAllCommand = new(StandardUICommandKind.SelectAll)
        {
            Description = "Select all rows."
        };
        SelectAllCommand.ExecuteRequested += OnSelectAllCommand;

        DeselectAllCommand = new()
        {
            IconSource = new SymbolIconSource { Symbol = Symbol.ClearSelection },
            Label = "Deselect All",
            Description = "Deselect all rows."
        };
        DeselectAllCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
        {
            Key = VirtualKey.A,
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
        });
        DeselectAllCommand.ExecuteRequested += OnDeselectAllCommand;

        CopyCommand = new(StandardUICommandKind.Copy)
        {
            Description = "Copy the selected row's content to clipboard."
        };
        CopyCommand.ExecuteRequested += OnCopyCommand;

        CopyWithHeadersCommand = new()
        {
            Label = "Copy with Headers",
            Description = "Copy the selected row's content including column headers to clipboard."
        };
        CopyWithHeadersCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
        {
            Key = VirtualKey.C,
            Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
        });
        CopyWithHeadersCommand.ExecuteRequested += OnCopyWithHeadersCommand;
    }

    private void OnSelectAllCommand(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        _tableView?.SelectAllSafe();
    }

    private void OnDeselectAllCommand(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        _tableView?.DeselectAll();
    }

    private void OnCopyCommand(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        _tableView?.CopyToClipboard(false);
    }

    private void OnCopyWithHeadersCommand(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        _tableView?.CopyToClipboard(true);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>();
        _optionsButton = GetTemplateChild("OptionsButton") as Button;
        _selectAllCheckBox = GetTemplateChild("SelectAllCheckBox") as CheckBox;
        _tableView?.RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, delegate { OnTableViewSelectionModeChanged(); });
        _tableView?.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, delegate { OnTableViewSelectionChanged(); });

        if (_tableView is not null)
        {
            _tableView.SelectionChanged += delegate { OnTableViewSelectionChanged(); };
            _tableView.Items.VectorChanged += delegate { OnTableViewSelectionChanged(); };
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Tapped += OnSelectAllCheckBoxTapped;
        }

        OnTableViewSelectionModeChanged();
    }

    private void OnTableViewSelectionChanged()
    {
        if (_tableView is not null && _selectAllCheckBox is not null)
        {
            if (_tableView.Items.Count == 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = false;
            }
            else if (_tableView.SelectedItems.Count == _tableView.Items.Count)
            {
                _selectAllCheckBox.IsChecked = true;
                _selectAllCheckBox.IsEnabled = true;
            }
            else if (_tableView.SelectedItems.Count > 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = true;
            }
            else
            {
                _selectAllCheckBox.IsChecked = false;
                _selectAllCheckBox.IsEnabled = true;
            }
        }
    }

    private void OnTableViewSelectionModeChanged()
    {
        if (_optionsButton is not null)
        {
            _optionsButton.Visibility = _tableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                _tableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Visibility = _tableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                _tableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnSelectAllCheckBoxTapped(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;

        if (checkBox.IsChecked == true)
        {
            _tableView?.SelectAllSafe();
        }
        else
        {
            checkBox.IsChecked = false;
            _tableView?.DeselectAll();
        }
    }

    public StandardUICommand SelectAllCommand { get; private set; }
    public StandardUICommand DeselectAllCommand { get; private set; }
    public StandardUICommand CopyCommand { get; private set; }
    public StandardUICommand CopyWithHeadersCommand { get; private set; }
}
