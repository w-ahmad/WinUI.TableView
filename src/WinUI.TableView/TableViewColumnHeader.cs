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
    private bool _canSort;
    private bool _canFilter;
    private TableView? _tableView;
    private Button? _optionsButton;
    private MenuFlyout? _optionsFlyout;
    private CheckBox? _selectAllCheckBox;
    private OptionsFlyoutViewModel _optionsFlyoutViewModel = default!;
    private string _propertyPath = default!;
    private (PropertyInfo, object?)[] _propertyInfos = default!;
    private SD? sortDirection;
    private bool isFiltered;
    private bool _resizeStarted;

    public TableViewColumnHeader()
    {
        DefaultStyleKey = typeof(TableViewColumnHeader);
        ManipulationMode = ManipulationModes.TranslateX;
    }

    private void DoSort(SD direction, bool singleSorting = true)
    {
        if (_canSort && _tableView is not null)
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
        if (_canSort && _tableView is not null && SortDirection is not null)
        {
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

        _optionsFlyout?.Hide();
        _tableView.ActiveFilters[_propertyPath] = Filter;
        _tableView.CollectionView.RefreshFilter();
        IsFiltered = true;
    }

    private bool Filter(object item)
    {
        var value = GetValue(item);
        value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
        return _optionsFlyoutViewModel.SelectedValues.Contains(value);
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        if (_canSort && _tableView is not null)
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
        _optionsButton = GetTemplateChild("OptionsButton") as Button;
        _optionsFlyout = GetTemplateChild("OptionsFlyout") as MenuFlyout;

        if (_tableView is null || _optionsButton is null || _optionsFlyout is null)
        {
            return;
        }

        if (Column is TableViewBoundColumn column && column.Binding.Path.Path is { Length: > 0 } path)
        {
            _propertyPath = path;
            _canSort = column.CanSort;
            _canFilter = column.CanFilter;
            _optionsButton.Visibility = _canFilter ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_canFilter)
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
                var isSelected = !string.IsNullOrEmpty(_filterText) ||
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
        if (_propertyInfos is null)
        {
            var type = _tableView!.ItemsSource?.GetType() is { } listType && listType.IsGenericType ? listType.GetGenericArguments()[0] : item?.GetType();
            item.GetValue(type, _propertyPath, out _propertyInfos);
        }

        return item.GetValue(_propertyInfos);
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

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        if (CanResize)
        {
            var point = e.GetCurrentPoint(this);
            var nearRightEdge = ActualWidth - point.Position.X <= 4 && point.Position.Y < ActualHeight;
            ProtectedCursor = nearRightEdge ? InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast) : null;
        }
    }

    protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
    {
        base.OnManipulationStarted(e);

        _resizeStarted = CanResize && ActualWidth - e.Position.X <= 4 && e.Position.Y < ActualHeight;
    }

    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (_resizeStarted)
        {
            Width = Math.Clamp(e.Position.X, MinWidth, MaxWidth);
        }
    }

    protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
    {
        base.OnManipulationCompleted(e);

        _resizeStarted = false;
    }

    public SD? SortDirection
    {
        get => sortDirection;
        internal set
        {
            sortDirection = value;
            OnSortDirectionChanged();
        }
    }

    public bool IsFiltered
    {
        get => isFiltered;
        internal set
        {
            isFiltered = value;
            OnIsFilteredChanged();
        }
    }

    public TableViewColumn? Column { get; internal set; }

    public bool CanResize => Column?.CanResize == true;
}