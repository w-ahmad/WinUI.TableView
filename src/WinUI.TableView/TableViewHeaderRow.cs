using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Converters;

namespace WinUI.TableView;

public partial class TableViewHeaderRow : Control
{
    private Button? _optionsButton;
    private CheckBox? _selectAllCheckBox;
    private StackPanel? _headersStackPanel;

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

            // Hack: this will allow keyboard accelerators to get work
            ShowAndHidOptionsFlyout();
            async void ShowAndHidOptionsFlyout()
            {
                _optionsButton.Flyout.ShowAt(_optionsButton);
                await Task.Delay(5);
                _optionsButton.Flyout.Hide();
            }
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Tapped += OnSelectAllCheckBoxTapped;
        }

        if (TableView.Columns is not null && GetTemplateChild("HeadersStackPanel") is StackPanel stackPanel)
        {
            _headersStackPanel = stackPanel;
            AddHeaders(TableView.Columns);
            TableView.Columns.CollectionChanged += OnTableViewColumnsCollectionChanged;
        }

        SetExportOptionsVisibility();
        OnTableViewSelectionModeChanged();

    }

    private void OnTableViewColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> newItems)
        {
            AddHeaders(newItems, e.NewStartingIndex);
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems?.OfType<TableViewColumn>() is IEnumerable<TableViewColumn> oldItems)
        {
            RemoveHeaders(oldItems);
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset && _headersStackPanel is not null)
        {
            _headersStackPanel.Children.Clear();
        }
    }

    private void RemoveHeaders(IEnumerable<TableViewColumn> columns)
    {
        if (_headersStackPanel is not null)
        {
            foreach (var column in columns)
            {
                var header = _headersStackPanel.Children.OfType<TableViewColumnHeader>().FirstOrDefault(x => x == column.HeaderControl);
                if (header is not null)
                {
                    _headersStackPanel.Children.Remove(header);
                }
            }
        }
    }

    private void AddHeaders(IEnumerable<TableViewColumn> columns, int index = -1)
    {
        if (_headersStackPanel is not null)
        {
            foreach (var column in columns)
            {
                var header = new TableViewColumnHeader { DataContext = column, Column = column };
                column.HeaderControl = header;

                if (index < 0)
                {
                    _headersStackPanel.Children.Add(header);
                }
                else
                {
                    index = Math.Min(index, _headersStackPanel.Children.Count);
                    _headersStackPanel.Children.Insert(index, header);
                    index++;
                }

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
    }

    private void SetExportOptionsVisibility()
    {
        var binding = new Binding
        {
            Path = new PropertyPath(nameof(TableView.ShowExportOptions)),
            Source = TableView,
            Converter = new BoolToVisibilityConverter()
        };

        if (GetTemplateChild("ExportAllMenuItem") is MenuFlyoutItem exportAll)
        {
            exportAll.SetBinding(VisibilityProperty, binding);
        }

        if (GetTemplateChild("ExportSelectedMenuItem") is MenuFlyoutItem exportSelected)
        {
            exportSelected.SetBinding(VisibilityProperty, binding);
        }
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
}
