using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
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
    private bool _resizeStarted;
    private double _resizeStartingWidth;
    private bool _resizePreviousStarted;
    private TextBox? _searchBox;

    /// <summary>
    /// Initializes a new instance of the TableViewColumnHeader class.
    /// </summary>
    public TableViewColumnHeader()
    {
        DefaultStyleKey = typeof(TableViewColumnHeader);
        ManipulationMode = ManipulationModes.TranslateX;
        RegisterPropertyChangedCallback(WidthProperty, OnWidthChanged);
        RightTapped += OnRightTapped;
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
    /// Handles the RightTapped event.
    /// </summary>
    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Check if right-click is enabled via TableView or colmun is currently resizing
        if (_tableView?.UseRightClickForColumnFilter != true || IsSizingCursor)
        {
            return;
        }

        // Shows the button's flyout if options button is available and either filtering or sorting is enabled
        if (_optionsButton is not null && (CanFilter || CanSort))
        {
            _optionsButton.Flyout?.ShowAt(_optionsButton);            
            e.Handled = true;
        }
    }

    /// <summary>
    /// Sorts the column in the specified direction.
    /// </summary>
    private void DoSort(SD? direction, bool singleSorting = true)
    {
        if (CanSort && Column is not null && _tableView is { CollectionView: CollectionView { } collectionView })
        {
            var defer = collectionView.DeferRefresh();
            {
                if (singleSorting)
                {
                    _tableView.ClearAllSortingWithEvent();
                }
                else
                {
                    ClearSortingWithEvent();
                }

                if (direction is not null)
                {
                    var boundColumn = Column as TableViewBoundColumn;
                    Column.SortDirection = direction;
                    _tableView.SortDescriptions.Add(
                        new ColumnSortDescription(Column!, boundColumn?.PropertyPath, direction.Value));

                    _tableView.EnsureAlternateRowColors();
                }
            }
            defer.Complete();
        }
    }

    /// <summary>
    /// Clears the sorting for the column.
    /// </summary>
    private void ClearSortingWithEvent()
    {
        var eventArgs = new TableViewClearSortingEventArgs();
        _tableView?.OnClearSorting(eventArgs);

        if (eventArgs.Handled)
        {
            return;
        }

        if (CanSort && _tableView is not null && Column is not null)
        {
            _tableView.DeselectAll();
            Column.SortDirection = null;
            _tableView.SortDescriptions.RemoveWhere(x => x is ColumnSortDescription columnSort && columnSort.Column == Column);
        }
    }

    /// <summary>
    /// Clears the filter for the column.
    /// </summary>
    private void ClearFilter()
    {
        _tableView?.FilterHandler?.ClearFilter(Column!);
    }

    /// <summary>
    /// Applies the filter for the column.
    /// </summary>
    private void ApplyFilter()
    {
        if (_selectAllCheckBox?.IsChecked is true && string.IsNullOrEmpty(_searchBox?.Text))
        {
            ClearFilter();
        }
        else if (_tableView is not null)
        {
            _optionsFlyoutViewModel.SelectedValues = [.. _optionsFlyoutViewModel.FilterItems.Where(x => x.IsSelected).Select(x => x.Value)];
            {
                _tableView.FilterHandler.SelectedValues[Column!] = _optionsFlyoutViewModel.SelectedValues;
                _tableView.FilterHandler?.ApplyFilter(Column!);
            }
        }
    }

    /// <summary>
    /// Hides the options flyout.
    /// </summary>
    private void HideFlyout()
    {
        _optionsFlyout?.Hide();
    }

    /// <inheritdoc/>
    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        if (CanSort && Column is not null && _tableView is not null && !IsSizingCursor)
        {
            var isCtrlButtonDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) is
                CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);

            var eventArgs = new TableViewSortingEventArgs(Column);
            _tableView.OnSorting(eventArgs);

            if (!eventArgs.Handled)
            {
                DoSort(GetNextSortDirection(), !isCtrlButtonDown);
            }
        }

        base.OnTapped(e);
    }

    private SD? GetNextSortDirection()
    {
        return Column?.SortDirection switch
        {
            SD.Ascending => SD.Descending,
            SD.Descending => null,
            _ => SD.Ascending,
        };
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>();
        _headerRow = this.FindAscendant<TableViewHeaderRow>();
        _optionsButton = GetTemplateChild("OptionsButton") as Button;
        _optionsFlyout = GetTemplateChild("OptionsFlyout") as MenuFlyout;
        _contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;

        if (_tableView is null || _optionsButton is null || _optionsFlyout is null)
        {
            return;
        }

        _optionsFlyout.Opening += OnOptionsFlyoutOpening;
        _optionsFlyout.Closed += OnOptionsFlyoutClosed;
        _optionsButton.Tapped += OnOptionsButtonTaped;
        _optionsButton.DataContext = _optionsFlyoutViewModel = new OptionsFlyoutViewModel(_tableView, this);

        var menuItem = _optionsFlyout.Items.FirstOrDefault(x => x.Name == "ItemsCheckFlyoutItem");
        menuItem?.ApplyTemplate();

        if (menuItem?.FindDescendant<CheckBox>(x => x.Name == "SelectAllCheckBox") is { } checkBox)
        {
            _selectAllCheckBox = checkBox;
            _selectAllCheckBox.Content = TableViewLocalizedStrings.SelectAllParenthesized;
            _selectAllCheckBox.Checked += OnSelectAllCheckBoxChecked;
            _selectAllCheckBox.Unchecked += OnSelectAllCheckBoxUnchecked;
        }

#if !WINDOWS
        if (menuItem?.FindDescendant<ListView>(x => x.Name is "FilterItemsList") is { } filterItemsList)
        {
            filterItemsList.Margin = new Thickness(12, 0, 0, 0);
        }
#endif

        if (menuItem?.FindDescendant<TextBox>(x => x.Name == "SearchBox") is { } searchBox)
        {
            _searchBox = searchBox;
            _searchBox.PlaceholderText = TableViewLocalizedStrings.SearchBoxPlaceholder;
            _searchBox.TextChanged += OnSearchBoxTextChanged;
#if WINDOWS
            _searchBox.PreviewKeyDown += OnSearchBoxKeyDown;
#else
            _searchBox.KeyDown += OnSearchBoxKeyDown;
#endif
        }

        SetFilterButtonVisibility();
        EnsureGridLines();
    }

    /// <summary>
    /// Handles the TextChanged event for the search box.
    /// </summary>
    private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_tableView?.FilterHandler is not null)
        {
            _optionsFlyoutViewModel.FilterItems = _tableView.FilterHandler.GetFilterItems(Column!, _searchBox!.Text);
        }
    }

    /// <summary>
    /// Handles the KeyDown event for the search box.
    /// </summary>
    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && _searchBox?.Text.Length > 0)
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
    private async void OnOptionsFlyoutOpening(object? sender, object e)
    {
        if (_tableView?.FilterHandler is not null)
        {
            _optionsFlyoutViewModel.FilterItems = _tableView.FilterHandler.GetFilterItems(Column!, null);
        }

        if (_searchBox is not null)
        {
            await Task.Delay(100);
            await FocusManager.TryFocusAsync(_searchBox, FocusState.Programmatic);
        }
    }

    /// <summary>
    /// Handles the Closed event for the options flyout.
    /// </summary>
    private void OnOptionsFlyoutClosed(object? sender, object e)
    {
        if (_searchBox is not null)
        {
            _searchBox.Text = string.Empty;
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
    internal void OnSortDirectionChanged()
    {
        if (Column?.SortDirection == SD.Ascending)
        {
            VisualStates.GoToState(this, false, VisualStates.StateSortAscending);
        }
        else if (Column?.SortDirection == SD.Descending)
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
    internal void OnIsFilteredChanged()
    {
        if (Column?.IsFiltered is true)
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
    internal void SetFilterButtonVisibility()
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (IsSizingCursor && CanResize && IsCursorInRightResizeArea(e))
        {
            _resizeStarted = true;
            _resizeStartingWidth = ActualWidth;
            CapturePointer(e.Pointer);
        }
        else if (IsSizingCursor && IsCursorInLeftResizeArea(e) && _headerRow?.GetPreviousHeader(this) is { Column: { } } header)
        {
            _resizePreviousStarted = true;
            _resizeStartingWidth = header.ActualWidth;
            CapturePointer(e.Pointer);
        }
    }

    /// <inheritdoc/>
    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (Column is null || _tableView is null)
        {
            return;
        }

        if (_resizeStarted)
        {
            var width = _resizeStartingWidth + e.Cumulative.Translation.X;

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
            var width = _resizeStartingWidth + e.Cumulative.Translation.X;

            width = width < minWidth ? minWidth : width;
            width = width > maxWidth ? maxWidth : width;

            header.Column.Width = new GridLength(width, GridUnitType.Pixel);
        }
    }

    /// <inheritdoc/>
    protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
    {
        base.OnManipulationCompleted(e);

        _resizeStarted = false;
        _resizePreviousStarted = false;
    }

    /// <inheritdoc/>
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
    private bool CanSort => _tableView?.CanSortColumns is true && Column?.CanSort is true;

    /// <summary>
    /// Gets a value indicating whether the column can be filtered.
    /// </summary>
    private bool CanFilter => _tableView?.CanFilterColumns is true && Column?.CanFilter is true;

    /// <summary>
    /// Gets a value indicating whether the previous column can be resized.
    /// </summary>
    private bool CanResizePrevious => _headerRow?.GetPreviousHeader(this)?.CanResize == true;

    /// <summary>
    /// Gets a value indicating whether the cursor is in the sizing area.
    /// </summary>
    private bool IsSizingCursor => ProtectedCursor is InputSystemCursor { CursorShape: InputSystemCursorShape.SizeWestEast };
}