using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Linq;
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

        if (_optionsButton is not null && _tableView is not null)
        {
            _optionsButton.DataContext = new OptionsFlyoutViewModel(_tableView);
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

    private class OptionsFlyoutViewModel
    {

        public OptionsFlyoutViewModel(TableView _tableView)
        {

            InitializeCommands();
            TableView = _tableView;
        }

        private void InitializeCommands()
        {
            SelectAllCommand.Description = "Select all rows.";
            SelectAllCommand.ExecuteRequested += delegate { TableView.SelectAllSafe(); };

            DeselectAllCommand.IconSource = new SymbolIconSource { Symbol = Symbol.ClearSelection };
            DeselectAllCommand.Description = "Deselect all rows.";
            DeselectAllCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.A,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
            });
            DeselectAllCommand.ExecuteRequested += delegate { TableView.DeselectAll(); };

            CopyCommand.Description = "Copy the selected row's content to clipboard.";
            CopyCommand.ExecuteRequested += delegate { TableView.CopyToClipboard(false); };

            CopyWithHeadersCommand.Description = "Copy the selected row's content including column headers to clipboard.";
            CopyWithHeadersCommand.KeyboardAccelerators.Add(new KeyboardAccelerator
            {
                Key = VirtualKey.C,
                Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift
            });
            CopyWithHeadersCommand.ExecuteRequested += delegate { TableView.CopyToClipboard(true); };

            ClearSortingsCommand.ExecuteRequested += delegate { ClearSortings(); };
            ClearSortingsCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.CollectionView.SortDescriptions.Count > 0;

            ClearFiltersCommand.ExecuteRequested += delegate { ClearFilters(); };
            ClearFiltersCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.ActiveFilters.Count > 0;
        }

        private void ClearSortings()
        {
            TableView.CollectionView.SortDescriptions.Clear();

            foreach (var header in TableView.Columns.Select(x => x.HeaderControl))
            {
                if (header is not null)
                {
                    header.SortDirection = null;
                }
            }
        }

        private void ClearFilters()
        {
            TableView.ActiveFilters.Clear();
            TableView.CollectionView.RefreshFilter();

            foreach (var header in TableView.Columns.Select(x => x.HeaderControl))
            {
                if (header is not null)
                {
                    header.IsFiltered = false;
                }
            }
        }

        public StandardUICommand SelectAllCommand { get; } = new(StandardUICommandKind.SelectAll);
        public StandardUICommand DeselectAllCommand { get; } = new() { Label = "Deselect All" };
        public StandardUICommand CopyCommand { get; } = new(StandardUICommandKind.Copy);
        public StandardUICommand CopyWithHeadersCommand { get; } = new() { Label = "Copy with Headers" };
        public StandardUICommand ClearSortingsCommand { get; } = new() { Label = "Clear Sortigns" };
        public StandardUICommand ClearFiltersCommand { get; } = new() { Label = "Clear Filters" };
        public TableView TableView { get; }
    }
}
