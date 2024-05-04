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

[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateSelectAllButton, GroupName = VisualStates.GroupSelectAllButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllCheckBox, GroupName = VisualStates.GroupSelectAllButton)]
[TemplateVisualState(Name = VisualStates.StateOptionsButton, GroupName = VisualStates.GroupSelectAllButton)]
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

        _optionsButton = GetTemplateChild("optionsButton") as Button;
        _selectAllCheckBox = GetTemplateChild("selectAllCheckBox") as CheckBox;
        TableView?.RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, delegate { SetSelectAllButtonState(); });
        TableView?.RegisterPropertyChangedCallback(TableView.ShowOptionsButtonProperty, delegate { SetSelectAllButtonState(); });
        TableView?.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, delegate { OnTableViewSelectionChanged(); });

        if (TableView is null)
        {
            return;
        }

        TableView.SelectionChanged += delegate { OnTableViewSelectionChanged(); };
        TableView.Items.VectorChanged += delegate { OnTableViewSelectionChanged(); };

        if (_optionsButton is not null)
        {
            _optionsButton.DataContext = new OptionsFlyoutViewModel(TableView);

            // Hack: this will allow keyboard accelerators to get work
            ShowAndHidOptionsFlyout();
            async void ShowAndHidOptionsFlyout()
            {
                if (_optionsButton.Visibility == Visibility.Visible)
                {
                    _optionsButton.Flyout.ShowAt(_optionsButton);
                    await Task.Delay(5);
                    _optionsButton.Flyout.Hide();
                }
            }
        }

        if (GetTemplateChild("selectAllButton") is Button selectAllButton)
        {
            selectAllButton.Tapped += delegate { TableView.SelectAllSafe(); };
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
        SetSelectAllButtonState();
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

    private void SetSelectAllButtonState()
    {
        if (TableView.SelectionMode == ListViewSelectionMode.Multiple)
        {
            VisualStates.GoToState(this, false, VisualStates.StateSelectAllCheckBox);
        }
        else if (TableView.ShowOptionsButton)
        {
            VisualStates.GoToState(this, false, VisualStates.StateOptionsButton);
        }
        else
        {
            VisualStates.GoToState(this, false, VisualStates.StateSelectAllButton);
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

    private IList<TableViewColumnHeader> Headers => _headersStackPanel!.Children.OfType<TableViewColumnHeader>().ToList();

    internal TableViewColumnHeader? GetPreviousHeader(TableViewColumnHeader? currentHeader)
    {
        var previousCellIndex = currentHeader is null ? Headers.Count - 1 : Headers.IndexOf(currentHeader) - 1;
        return previousCellIndex >= 0 ? Headers[previousCellIndex] : default;
    }

    public TableView TableView
    {
        get => (TableView)GetValue(TableViewProperty);
        set => SetValue(TableViewProperty, value);
    }

    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewHeaderRow), new PropertyMetadata(null));
}
