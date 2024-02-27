using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System.Linq;
using Windows.System;

namespace WinUI.TableView;

public class TableViewHeaderRow : Control
{
    private Button? _optionsButton;
    private CheckBox? _selectAllCheckBox;

    public TableViewHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewHeaderRow);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _optionsButton = GetTemplateChild("OptionsButton") as Button;
        _selectAllCheckBox = GetTemplateChild("SelectAllCheckBox") as CheckBox;
        TableView?.RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, delegate { OnTableViewSelectionModeChanged(); });
        TableView?.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, delegate { OnTableViewSelectionChanged(); });

        if (TableView is null)
            return;

        TableView.SelectionChanged += delegate { OnTableViewSelectionChanged(); };
        TableView.Items.VectorChanged += delegate { OnTableViewSelectionChanged(); };

        if (_optionsButton is not null)
        {
            _optionsButton.DataContext = new OptionsFlyoutViewModel(TableView);
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Tapped += OnSelectAllCheckBoxTapped;
        }

        if (GetTemplateChild("HeadersStackPanel") is StackPanel stackPanel && TableView.Columns?.Count > 0)
        {
            foreach (var column in TableView.Columns)
            {
                var header = new TableViewColumnHeader { DataContext = column};
                stackPanel.Children.Add(header);

                header.SetBinding(ContentControl.ContentProperty,
                                  new Binding { Path = new PropertyPath(nameof(TableViewColumn.Header)) });
                header.SetBinding(WidthProperty,
                                  new Binding { Path = new PropertyPath(nameof(TableViewColumn.Width)), Mode = BindingMode.TwoWay });
                header.SetBinding(MinWidthProperty,
                                  new Binding { Path = new PropertyPath(nameof(TableViewColumn.MinWidth)) });
                header.SetBinding(MaxWidthProperty,
                                  new Binding { Path = new PropertyPath(nameof(TableViewColumn.MaxWidth)) });
            }
        }

        OnTableViewSelectionModeChanged();
    }

    private void OnTableViewSelectionChanged()
    {
        if (TableView is not null && _selectAllCheckBox is not null)
        {
            if (TableView.Items.Count == 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = false;
            }
            else if (TableView.SelectedItems.Count == TableView.Items.Count)
            {
                _selectAllCheckBox.IsChecked = true;
                _selectAllCheckBox.IsEnabled = true;
            }
            else if (TableView.SelectedItems.Count > 0)
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
            _optionsButton.Visibility = TableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                TableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Visibility = TableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                TableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnSelectAllCheckBoxTapped(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;

        if (checkBox.IsChecked == true)
        {
            TableView?.SelectAllSafe();
        }
        else
        {
            checkBox.IsChecked = false;
            TableView?.DeselectAll();
        }
    }

    public TableView TableView
    {
        get { return (TableView)GetValue(TableViewProperty); }
        set { SetValue(TableViewProperty, value); }
    }

    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewHeaderRow), new PropertyMetadata(null));

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

            ClearSortingCommand.ExecuteRequested += delegate { ClearSorting(); };
            ClearSortingCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.CollectionView.SortDescriptions.Count > 0;

            ClearFilterCommand.ExecuteRequested += delegate { ClearFilters(); };
            ClearFilterCommand.CanExecuteRequested += (_, e) => e.CanExecute = TableView.ActiveFilters.Count > 0;
        }

        private void ClearSorting()
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
        public StandardUICommand ClearSortingCommand { get; } = new() { Label = "Clear Sorting" };
        public StandardUICommand ClearFilterCommand { get; } = new() { Label = "Clear Filter" };
        public TableView TableView { get; }
    }
}
