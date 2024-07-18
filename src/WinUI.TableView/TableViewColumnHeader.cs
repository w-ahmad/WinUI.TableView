using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using WinUI.TableView.Extensions;
using SD = CommunityToolkit.WinUI.Collections.SortDirection;

namespace WinUI.TableView;

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
    private CheckBox? _selectAllCheckBox;
    private OptionsFlyoutViewModel _optionsFlyoutViewModel = default!;
    private string _propertyPath = default!;
    private (PropertyInfo, object?)[] _propertyInfo = default!;
    private SD? _sortDirection;
    private bool _isFiltered;
    private bool _resizeStarted;
    private bool _resizePreviousStarted;

    public TableViewColumnHeader()
    {
        DefaultStyleKey = typeof(TableViewColumnHeader);
        ManipulationMode = ManipulationModes.TranslateX;
        RegisterPropertyChangedCallback(WidthProperty, OnWidthChanged);
    }

    private void OnWidthChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (Column is not null)
        {
            Column.ActualWidth = Width;
        }
    }

    private void DoSort(SD direction, bool singleSorting = true)
    {
        if (CanSort && _tableView is not null)
        {
            if (singleSorting)
            {
                _tableView.CollectionView.SortDescriptions.Clear();

                foreach (var header in _tableView.Columns.Select(x => x.HeaderControl))
                {
                    if (header is not null && header != this)
                    {
                        header.SortDirection = null;
                    }
                }
            }

            _tableView.DeselectAll();

            if (_tableView.CollectionView.SortDescriptions.FirstOrDefault(x => x.PropertyName == _propertyPath) is { } description)
            {
                _tableView.CollectionView.SortDescriptions.Remove(description);
            }

            SortDirection = direction;
            _tableView.CollectionView.SortDescriptions.Add(new SortDescription(_propertyPath, SortDirection.Value));
        }
    }

    private void ClearSorting()
    {
        if (CanSort && _tableView is not null && SortDirection is not null)
        {
            _tableView.DeselectAll();
            SortDirection = null;

            if (_tableView.CollectionView.SortDescriptions.FirstOrDefault(x => x.PropertyName == _propertyPath) is { } description)
            {
                _tableView.CollectionView.SortDescriptions.Remove(description);
            }
        }
    }

    private void ClearFilter()
    {
        if (_tableView?.ActiveFilters.ContainsKey(_propertyPath) == true)
        {
            _tableView.ActiveFilters.Remove(_propertyPath);
            _tableView.DeselectAll();
        }

        IsFiltered = false;
        _optionsFlyoutViewModel.FilterItems.Clear();
        _tableView?.CollectionView.RefreshFilter();
    }

    private void ApplyFilter()
    {
        if (_tableView is null)
        {
            return;
        }

        _tableView.DeselectAll();
        _tableView.ActiveFilters[_propertyPath] = Filter;
        _tableView.CollectionView.RefreshFilter();
        IsFiltered = true;
    }

    private void HideFlyout()
    {
        _optionsFlyout?.Hide();
    }

    private bool Filter(object item)
    {
        var value = GetValue(item);
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

        if (_tableView is null || _optionsButton is null || _optionsFlyout is null)
        {
            return;
        }

        if (Column is TableViewBoundColumn column && column.Binding.Path.Path is { Length: > 0 } path)
        {
            _propertyPath = path;
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
                _selectAllCheckBox.Tapped += OnSelectAllCheckBox_Tapped;
            }

            if (menuItem?.FindDescendant<AutoSuggestBox>(x => x.Name == "SearchBox") is { } searchBox)
            {
                searchBox.PreviewKeyDown += OnSearchBoxKeyDown;
            }
        }

        SetFilterButtonVisibility();
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && _optionsFlyoutViewModel is { FilterText.Length: > 0 })
        {
            _optionsFlyoutViewModel.OkCommand.Execute(null);

            e.Handled = true;
        }
    }

    private async void OnSelectAllCheckBox_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;

        if (checkBox.IsChecked is null)
        {
            await Task.Delay(5);
            checkBox.IsChecked = false;
        }

        _optionsFlyoutViewModel.SetFilterItemsState(checkBox.IsChecked == true);
    }

    private void OnOptionsFlyoutOpening(object? sender, object e)
    {
        _optionsFlyoutViewModel.FilterText = null;
    }

    private void PrepareFilterItems(string? _filterText)
    {
        if (_tableView is { ItemsSource: { } } && Column is TableViewBoundColumn column)
        {
            var collectionView = new AdvancedCollectionView(_tableView.ItemsSource)
            {
                Filter = o => _tableView.ActiveFilters.Where(x => x.Key != _propertyPath)
                                                      .All(x => x.Value(o))
            };

            var isFiltered = _tableView.ActiveFilters.ContainsKey(_propertyPath);

            _optionsFlyoutViewModel.FilterItems = collectionView.Select(item =>
            {
                var value = GetValue(item);
                value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
                var isSelected = !isFiltered || !string.IsNullOrEmpty(_filterText) ||
                  (isFiltered && _optionsFlyoutViewModel.SelectedValues.Contains(value));

                return string.IsNullOrEmpty(_filterText)
                      || value?.ToString()?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) == true
                      ? new FilterItem(isSelected, value, _optionsFlyoutViewModel)
                      : null;

            }).OfType<FilterItem>()
              .OrderBy(x => x.Value)
              .DistinctBy(x => x.Value)
              .ToList();
        }
    }

    private object? GetValue(object item)
    {
        if (_propertyInfo is null)
        {
            var type = _tableView!.ItemsSource?.GetType() is { } listType && listType.IsGenericType ? listType.GetGenericArguments()[0] : item?.GetType();
            item.GetValue(type, _propertyPath, out _propertyInfo);
        }

        return item.GetValue(_propertyInfo);
    }

    private void OnOptionsButtonTaped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

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

    private void SetFilterButtonVisibility()
    {
        if (_optionsButton is not null)
        {
            _optionsButton.Visibility = CanFilter ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_contentPresenter is not null)
        {
            _contentPresenter.Margin = CanFilter ? new Thickness(0, 0, 8, 0) : new Thickness(0);
        }
    }

    private bool IsCursorInRightResizeArea(PointerRoutedEventArgs args)
    {
        var resizeWidth = args.Pointer.PointerDeviceType == PointerDeviceType.Touch ? 8 : 4;
        var point = args.GetCurrentPoint(this);
        var resizeHeight = ActualHeight - (CanFilter ? _optionsButton?.ActualHeight ?? 0 : 0);
        return ActualWidth - point.Position.X <= resizeWidth && point.Position.Y < resizeHeight;
    }

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
            var width = Math.Clamp(header.Column.DesiredWidth, header.Column.MinWidth ?? _tableView.MinColumnWidth, header.Column.MaxWidth ?? _tableView.MaxColumnWidth);
            header.Column.Width = new GridLength(width, GridUnitType.Pixel);
        }
        else if (Column is not null)
        {
            var width = Math.Clamp(Column.DesiredWidth, Column.MinWidth ?? _tableView.MinColumnWidth, Column.MaxWidth ?? _tableView.MaxColumnWidth);
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

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        _resizeStarted = false;
        _resizePreviousStarted = false;
        ReleasePointerCaptures();
    }

    public SD? SortDirection
    {
        get => _sortDirection;
        internal set
        {
            _sortDirection = value;
            OnSortDirectionChanged();
        }
    }

    public bool IsFiltered
    {
        get => _isFiltered;
        internal set
        {
            _isFiltered = value;
            OnIsFilteredChanged();
        }
    }

    public TableViewColumn? Column { get; internal set; }

    private bool CanResize => _tableView?.CanResizeColumns == true && Column?.CanResize == true;
    private bool CanSort => _tableView?.CanSortColumns == true && Column is TableViewBoundColumn { CanSort: true };
    private bool CanFilter => _tableView?.CanFilterColumns == true && Column is TableViewBoundColumn { CanFilter: true };
    private bool CanResizePrevious => _headerRow?.GetPreviousHeader(this)?.CanResize == true;
    private bool IsSizingCursor => ProtectedCursor is InputSystemCursor { CursorShape: InputSystemCursorShape.SizeWestEast };
}