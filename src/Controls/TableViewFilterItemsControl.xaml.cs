using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace WinUI.TableView.Controls;

/// <summary>
/// Represents the control that displays filter items in the filter flyout of a TableViewColumnHeader.
/// </summary>
public partial class TableViewFilterItemsControl : UserControl
{
    private bool _canSetState = true;
    private ICollection<TableViewFilterItem>? _filterItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewFilterItemsControl"/> class.
    /// </summary>
    public TableViewFilterItemsControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the state of the <see cref="TableViewFilterItemsControl"/>.
    /// </summary>
    internal async void Initialize()
    {
        FilterItems = TableView?.FilterHandler?.GetFilterItems(ColumnHeader?.Column!, null).ToList();

        if (searchBox is not null)
        {
            await Task.Delay(100);
            await FocusManager.TryFocusAsync(searchBox, FocusState.Programmatic);
        }

        if (filterItemsList is not null && filterItemsList.Items.Count > 0)
        {
            filterItemsList.ScrollIntoView(filterItemsList.Items[0]);
        }
    }

    /// <summary>
    /// Clears the search box text.
    /// </summary>
    internal void ClearSearchBox()
    {
        if (searchBox is not null)
        {
            searchBox.Text = string.Empty;
        }
    }

    private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterItems = TableView?.FilterHandler?.GetFilterItems(ColumnHeader?.Column!, searchBox!.Text);
    }

    /// <summary>
    /// Handles the KeyDown or PreviewKeyDown event for the searchBox.
    /// </summary>
    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && searchBox?.Text.Length > 0)
        {
            ColumnHeader?.ExecuteOkCommand();

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the Checked and Unchecked event for the selectAllCheckBox.
    /// </summary>
    private void OnSelectAllCheckBoxCheckChanged(object sender, RoutedEventArgs e)
    {
        SetFilterItemsState(selectAllCheckBox.IsChecked is true);
    }

    /// <summary>
    /// Sets the state of the select all checkbox.
    /// </summary>
    internal void SetSelectAllCheckBoxState()
    {
        if (selectAllCheckBox is null || !_canSetState)
        {
            return;
        }


        selectAllCheckBox.IsChecked = _filterItems?.All(x => x.IsSelected) ?? false ? true
                                      : _filterItems?.All(x => !x.IsSelected) ?? false ? false
                                      : null;
    }

    /// <summary>
    /// Sets the state of the filter items.
    /// </summary>
    /// <param name="isSelected">The state to set.</param>
    internal void SetFilterItemsState(bool isSelected)
    {
        _canSetState = false;

        foreach (var item in filterItemsList.Items.OfType<TableViewFilterItem>())
        {
            item.IsSelected = isSelected;
        }

        _canSetState = true;
    }

    /// <summary>
    /// Attaches property changed handlers to the filter items.
    /// </summary>
    private void AttachPropertyChangedHandlers()
    {
        if (_filterItems?.Count > 0)
        {
            foreach (var item in _filterItems)
            {
                item.PropertyChanged += OnFilterItemPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Detaches property changed handlers from the filter items.
    /// </summary>
    private void DetachPropertyChangedHandlers()
    {
        if (_filterItems?.Count > 0)
        {
            foreach (var item in _filterItems)
            {
                item.PropertyChanged -= OnFilterItemPropertyChanged;
            }
        }
    }

    /// <summary>
    /// Handles the PropertyChanged event for filter items.
    /// </summary>
    private void OnFilterItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SetSelectAllCheckBoxState();
    }

    /// <summary>
    /// Gets a value indicating whether to apply the filter based on the control state.
    /// </summary>
    internal bool ShouldApplyFilter => selectAllCheckBox.IsChecked is false || !string.IsNullOrEmpty(searchBox.Text);

    /// <summary>
    /// Gets or sets the filter items for the control.
    /// </summary>
    internal ICollection<TableViewFilterItem>? FilterItems
    {
        get => _filterItems;
        set
        {
            if (_filterItems == value) return;

            DetachPropertyChangedHandlers();
            _filterItems = value;
            filterItemsList.ItemsSource = _filterItems;
            AttachPropertyChangedHandlers();
            SetSelectAllCheckBoxState();
        }
    }

    /// <summary>
    /// Gets or sets the column header associated with the filter items control.
    /// </summary>
    public TableViewColumnHeader? ColumnHeader { get; internal set; }

    /// <summary>
    /// Gets or sets the TableView associated with the filter items control.
    /// </summary>
    public TableView? TableView { get; internal set; }
}
