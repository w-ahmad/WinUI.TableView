using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using WinUI.TableView.Extensions;
using SD = WinUI.TableView.SortDirection;

namespace WinUI.TableView;

/// <summary>
/// Represents the header of a column in a TableView.
/// </summary>
[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePointerOver, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePressed, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateFocused, GroupName = VisualStates.GroupFocus)]
[TemplateVisualState(Name = VisualStates.StateUnfocused, GroupName = VisualStates.GroupFocus)]
[TemplateVisualState(Name = VisualStates.StateUnsorted, GroupName = VisualStates.GroupSort)]
[TemplateVisualState(Name = VisualStates.StateSortAscending, GroupName = VisualStates.GroupSort)]
[TemplateVisualState(Name = VisualStates.StateSortDescending, GroupName = VisualStates.GroupSort)]
[TemplateVisualState(Name = VisualStates.StateFiltered, GroupName = VisualStates.GroupFilter)]
[TemplateVisualState(Name = VisualStates.StateUnfiltered, GroupName = VisualStates.GroupFilter)]
public partial class TableViewColumnHeader : ContentControl
{
    private TableView? _tableView;
    private TableViewHeaderRow? _headerRow;
    private Button? _optionsButton;
    private MenuFlyout? _optionsFlyout;
    private ContentPresenter? _contentPresenter;
    private Rectangle? _v_gridLine;
    private CheckBox? _selectAllCheckBox;
    private OptionsFlyoutViewModel _optionsFlyoutViewModel = default!;
    private SD? _sortDirection;
    private bool _isFiltered;
    private bool _resizeStarted;
    private bool _resizePreviousStarted;

    /// <summary>
    /// Initializes a new instance of the TableViewColumnHeader class.
    /// </summary>
    public TableViewColumnHeader()
    {
        DefaultStyleKey = typeof(TableViewColumnHeader);
        ManipulationMode = ManipulationModes.TranslateX;
        RegisterPropertyChangedCallback(WidthProperty, OnWidthChanged);
    }

    /// <summary>
    /// Handles changes to the Width property.
    /// </summary>
    private void OnWidthChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (Column is not null)
        {
            Column.ActualWidth = Width;
        }
    }

    /// <summary>
    /// Sorts the column in the specified direction.
    /// </summary>
    private void DoSort(SD direction, bool singleSorting = true)
    {
        if (CanSort && _tableView is not null)
        {
            if (singleSorting)
            {
                _tableView.ClearSorting();
            }
            else
            {
                ClearSorting();
            }

            var boundColumn = Column as TableViewBoundColumn;
            SortDirection = direction;
            _tableView.SortDescriptions.Add(
                new ColumnSortDescription(Column!, boundColumn?.PropertyPath, SortDirection.Value));

            _tableView.EnsureAlternateRowColors();
        }
    }

    /// <summary>
    /// Clears the sorting for the column.
    /// </summary>
    private void ClearSorting()
    {
        if (CanSort && _tableView is not null && SortDirection is not null)
        {
            _tableView.DeselectAll();
            SortDirection = null;
            _tableView.SortDescriptions.RemoveWhere(x => x is ColumnSortDescription columnSort && columnSort.Column == Column);
        }
    }

    /// <summary>
    /// Clears the filter for the column.
    /// </summary>
    private void ClearFilter()
    {
        _tableView?.FilterDescriptions.RemoveWhere(x => x is ColumnFilterDescription columnFilter && columnFilter.Column == Column);
        IsFiltered = false;
        _optionsFlyoutViewModel.FilterItems.Clear();
        _tableView?.RefreshFilter();
    }

    /// <summary>
    /// Applies the filter for the column.
    /// </summary>
    private void ApplyFilter()
    {
        if (_tableView is null)
        {
            return;
        }

        _tableView.DeselectAll();

        if (IsFiltered)
        {
            _tableView.RefreshFilter();
        }
        else
        {
            var boundColumn = Column as TableViewBoundColumn;
            _tableView.FilterDescriptions.Add(new ColumnFilterDescription(Column!, boundColumn?.PropertyPath, Filter));
        }

        _tableView.RefreshFilter();
        _tableView.EnsureAlternateRowColors();

        IsFiltered = true;
    }

    /// <summary>
    /// Hides the options flyout.
    /// </summary>
    private void HideFlyout()
    {
        _optionsFlyout?.Hide();
    }

    /// <summary>
    /// Filters the items in the collection view.
    /// </summary>
    private bool Filter(object? item)
    {
        var value = Column?.GetCellContent(item);
        value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
        return _optionsFlyoutViewModel.SelectedValues.Contains(value);
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        if (CanSort && _tableView is not null && !IsSizingCursor)
        {
            var isCtrlButtonDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) is
                CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);

            DoSort(SortDirection == SD.Ascending ? SD.Descending : SD.Ascending, !isCtrlButtonDown);
        }

        base.OnTapped(e);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>();
        _tableView?.RegisterPropertyChangedCallback(TableView.CanFilterColumnsProperty, delegate { SetFilterButtonVisibility(); });
        _headerRow = this.FindAscendant<TableViewHeaderRow>();
        _optionsButton = GetTemplateChild("OptionsButton") as Button;
        _optionsFlyout = GetTemplateChild("OptionsFlyout") as MenuFlyout;
        _contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;

        if (_tableView is null || _optionsButton is null || _optionsFlyout is null)
        {
            return;
        }

        if (Column is TableViewBoundColumn column)
        {
            column?.RegisterPropertyChangedCallback(TableViewBoundColumn.CanFilterProperty, delegate { SetFilterButtonVisibility(); });
        }

        if (_optionsButton is not null && _optionsFlyout is not null)
        {
            _optionsFlyout.Opening += OnOptionsFlyoutOpening;
            _optionsButton.Tapped += OnOptionsButtonTaped;
            _optionsButton.DataContext = _optionsFlyoutViewModel = new OptionsFlyoutViewModel(_tableView, this);

            var menuItem = _optionsFlyout.Items.FirstOrDefault(x => x.Name == "ItemsCheckFlyoutItem");
            menuItem?.ApplyTemplate();
            _selectAllCheckBox = menuItem?.FindDescendant<CheckBox>(x => x.Name == "SelectAllCheckBox");

            if (_selectAllCheckBox is not null)
            {
                _selectAllCheckBox.Checked += OnSelectAllCheckBoxChecked;
                _selectAllCheckBox.Unchecked += OnSelectAllCheckBoxUnchecked;
            }

            if (menuItem?.FindDescendant<AutoSuggestBox>(x => x.Name == "SearchBox") is { } searchBox)
            {
                searchBox.PreviewKeyDown += OnSearchBoxKeyDown;
            }
        }

        SetFilterButtonVisibility();
        EnsureGridLines();
    }

    /// <summary>
    /// Handles the KeyDown event for the search box.
    /// </summary>
    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && _optionsFlyoutViewModel is { FilterText.Length: > 0 })
        {
            _optionsFlyoutViewModel.OkCommand.Execute(null);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the Checked event for the select all checkbox.
    /// </summary>
    private void OnSelectAllCheckBoxChecked(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        _optionsFlyoutViewModel.SetFilterItemsState(checkBox.IsChecked == true);
    }

    /// <summary>
    /// Handles the Unchecked event for the select all checkbox.
    /// </summary>
    private void OnSelectAllCheckBoxUnchecked(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;
        _optionsFlyoutViewModel.SetFilterItemsState(checkBox.IsChecked == true);
    }

    /// <summary>
    /// Handles the Opening event for the options flyout.
    /// </summary>
    private void OnOptionsFlyoutOpening(object? sender, object e)
    {
        _optionsFlyoutViewModel.FilterText = null;
    }

    /// <summary>
    /// Prepares the filter items based on the filter text.
    /// </summary>
    private void PrepareFilterItems(string? _filterText)
    {
        if (_tableView is { ItemsSource: { } } && Column is TableViewBoundColumn column)
        {
            var collectionView = new CollectionView(_tableView.ItemsSource);
            collectionView.FilterDescriptions.AddRange(
                _tableView.FilterDescriptions.Where(
                x => x is not ColumnFilterDescription columnFilter || columnFilter.Column != column));

            var filterItems = new SortedSet<object?>();

            foreach (var item in collectionView)
            {
                var value = Column?.GetCellContent(item);
                filterItems.Add(value);
            }

            _optionsFlyoutViewModel.FilterItems = filterItems.Select(value =>
            {
                value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
                var isSelected = !IsFiltered || !string.IsNullOrEmpty(_filterText) ||
                  (IsFiltered && _optionsFlyoutViewModel.SelectedValues.Contains(value));

                return string.IsNullOrEmpty(_filterText)
                      || value?.ToString()?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) == true
                      ? new FilterItem(isSelected, value, _optionsFlyoutViewModel)
                      : null;

            }).OfType<FilterItem>().ToList();
        }
    }

    /// <summary>
    /// Handles the Tapped event for the options button.
    /// </summary>
    private void OnOptionsButtonTaped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    /// <summary>
    /// Handles changes to the SortDirection property.
    /// </summary>
    private void OnSortDirectionChanged()
    {
        if (SortDirection == SD.Ascending)
        {
            VisualStates.GoToState(this, false, VisualStates.StateSortAscending);
        }
        else if (SortDirection == SD.Descending)
        {
            VisualStates.GoToState(this, false, VisualStates.StateSortDescending);
        }
        else
        {
            VisualStates.GoToState(this, false, VisualStates.StateUnsorted);
        }
    }

    /// <summary>
    /// Handles changes to the IsFiltered property.
    /// </summary>
    private void OnIsFilteredChanged()
    {
        if (IsFiltered)
        {
            VisualStates.GoToState(this, false, VisualStates.StateFiltered);
        }
        else
        {
            VisualStates.GoToState(this, false, VisualStates.StateUnfiltered);
        }
    }

    /// <summary>
    /// Sets the visibility of the filter button.
    /// </summary>
    private void SetFilterButtonVisibility()
    {
        if (_optionsButton is not null)
        {
            _optionsButton.Visibility = CanFilter ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_contentPresenter is not null)
        {
            _contentPresenter.Margin = CanFilter ? new Thickness(
                Padding.Left,
                Padding.Top,
                Padding.Right + 8,
                0) : Padding;
        }
    }

    /// <summary>
    /// Determines whether the cursor is in the right resize area.
    /// </summary>
    private bool IsCursorInRightResizeArea(PointerRoutedEventArgs args)
    {
        var resizeWidth = args.Pointer.PointerDeviceType == PointerDeviceType.Touch ? 8 : 4;
        var point = args.GetCurrentPoint(this);
        var resizeHeight = ActualHeight - (CanFilter ? _optionsButton?.ActualHeight ?? 0 : 0);
        return ActualWidth - point.Position.X <= resizeWidth && point.Position.Y < resizeHeight;
    }

    /// <summary>
    /// Determines whether the cursor is in the left resize area.
    /// </summary>
    private bool IsCursorInLeftResizeArea(PointerRoutedEventArgs args)
    {
        var resizeArea = args.Pointer.PointerDeviceType == PointerDeviceType.Touch ? 8 : 4;
        var point = args.GetCurrentPoint(this);
        return point.Position.X <= resizeArea && point.Position.Y < ActualHeight;
    }

    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        base.OnDoubleTapped(e);

        if (!IsSizingCursor || _tableView is null)
        {
            return;
        }

        var position = e.GetPosition(this);

        if (position.X <= 8 && _headerRow?.GetPreviousHeader(this) is { Column: { } } header)
        {
            var width = Math.Clamp(
                header.Column.DesiredWidth,
                header.Column.MinWidth ?? _tableView.MinColumnWidth,
                header.Column.MaxWidth ?? _tableView.MaxColumnWidth);
            header.Column.Width = new GridLength(width, GridUnitType.Pixel);
        }
        else if (Column is not null)
        {
            var width = Math.Clamp(
                Column.DesiredWidth,
                Column.MinWidth ?? _tableView.MinColumnWidth,
                Column.MaxWidth ?? _tableView.MaxColumnWidth);
            Column.Width = new GridLength(width, GridUnitType.Pixel);
        }
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        if (CanResize && IsCursorInRightResizeArea(e))
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }
        else if (CanResizePrevious && IsCursorInLeftResizeArea(e))
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }
        else if (!_resizeStarted && !_resizePreviousStarted)
        {
            ProtectedCursor = null;
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (IsSizingCursor && CanResize && IsCursorInRightResizeArea(e))
        {
            _resizeStarted = true;
            CapturePointer(e.Pointer);
        }
        else if (IsSizingCursor && IsCursorInLeftResizeArea(e))
        {
            _resizePreviousStarted = true;
            CapturePointer(e.Pointer);
        }
    }

    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (Column is null || _tableView is null)
        {
            return;
        }

        if (_resizeStarted)
        {
            var width = e.Position.X;
            var minWidth = Column.MinWidth ?? _tableView.MinColumnWidth;
            var maxWidth = Column.MaxWidth ?? _tableView.MaxColumnWidth;

            width = width < minWidth ? minWidth : width;
            width = width > maxWidth ? maxWidth : width;

            Column.Width = new GridLength(width, GridUnitType.Pixel);
        }
        else if (_resizePreviousStarted && _headerRow?.GetPreviousHeader(this) is { Column: { } } header)
        {
            var minWidth = header.Column.MinWidth ?? _tableView.MinColumnWidth;
            var maxWidth = header.Column.MaxWidth ?? _tableView.MaxColumnWidth;
            var width = header.Column.ActualWidth + e.Position.X;

            width = width < minWidth ? minWidth : width;
            width = width > maxWidth ? maxWidth : width;

            header.Column.Width = new GridLength(width, GridUnitType.Pixel);
        }
    }

    protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
    {
        base.OnManipulationCompleted(e);

        _resizeStarted = false;
        _resizePreviousStarted = false;
    }

    protected override async void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        _resizeStarted = false;
        _resizePreviousStarted = false;
        ReleasePointerCaptures();

        await Task.Delay(100);

        if (_tableView?.CurrentCellSlot is not null)
        {
            var cell = _tableView.GetCellFromSlot(_tableView.CurrentCellSlot.Value);
            cell?.ApplyCurrentCellState();
        }
    }

    /// <summary>
    /// Ensures grid lines are applied.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (_v_gridLine is not null && _tableView is not null)
        {
            _v_gridLine.Fill = _tableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                               ? _tableView.VerticalGridLinesStroke : new SolidColorBrush(Colors.Transparent);
            _v_gridLine.Width = _tableView.VerticalGridLinesStrokeThickness;
            _v_gridLine.Visibility = _tableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                     || _tableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                     ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Gets or sets the sort direction for the column.
    /// </summary>
    public SD? SortDirection
    {
        get => _sortDirection;
        internal set
        {
            _sortDirection = value;
            OnSortDirectionChanged();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the column is filtered.
    /// </summary>
    public bool IsFiltered
    {
        get => _isFiltered;
        internal set
        {
            _isFiltered = value;
            OnIsFilteredChanged();
        }
    }

    /// <summary>
    /// Gets or sets the column associated with the header.
    /// </summary>
    public TableViewColumn? Column { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the column can be resized.
    /// </summary>
    private bool CanResize => _tableView?.CanResizeColumns == true && Column?.CanResize == true;

    /// <summary>
    /// Gets a value indicating whether the column can be sorted.
    /// </summary>
    private bool CanSort => _tableView?.CanSortColumns == true && Column is TableViewBoundColumn { CanSort: true };

    /// <summary>
    /// Gets a value indicating whether the column can be filtered.
    /// </summary>
    private bool CanFilter => _tableView?.CanFilterColumns == true && Column is TableViewBoundColumn { CanFilter: true };

    /// <summary>
    /// Gets a value indicating whether the previous column can be resized.
    /// </summary>
    private bool CanResizePrevious => _headerRow?.GetPreviousHeader(this)?.CanResize == true;

    /// <summary>
    /// Gets a value indicating whether the cursor is in the sizing area.
    /// </summary>
    private bool IsSizingCursor => ProtectedCursor is InputSystemCursor { CursorShape: InputSystemCursorShape.SizeWestEast };
}