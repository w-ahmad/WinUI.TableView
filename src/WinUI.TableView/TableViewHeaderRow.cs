using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView;

public class TableViewHeaderRow : ItemsControl
{
    private TableView? _tableView;
    private Button? _selectAllButton;
    private CheckBox? _selectAllCheckBox;

    public TableViewHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewHeaderRow);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>();
        _selectAllButton = GetTemplateChild("SelectAllButton") as Button;
        _selectAllCheckBox = GetTemplateChild("SelectAllCheckBox") as CheckBox;
        _tableView?.RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, delegate { OnTableViewSelectionModeChanged(); });
        _tableView?.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, delegate { OnTableViewSelectionChanged(); });

        if (_tableView is not null)
        {
            _tableView.SelectionChanged += delegate { OnTableViewSelectionChanged(); };
            _tableView.Items.VectorChanged += delegate { OnTableViewSelectionChanged(); };
        }

        if (_selectAllButton is not null)
        {
            _selectAllButton.Click += OnSelectAllButtonClick;
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
        if (_selectAllButton is not null)
        {
            _selectAllButton.Visibility = _tableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                _tableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Visibility = _tableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                _tableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnSelectAllButtonClick(object sender, RoutedEventArgs e)
    {
        _tableView?.SelectAllSafe();
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
}
