using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using WinUI.TableView.Collections;
using WinUI.TableView.Controls;
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
    private TableViewFilterMenuFlyout? _optionsFlyout;
    private ContentPresenter? _contentPresenter;
    private Rectangle? _v_gridLine;
    private bool _resizeStarted;
    private double _resizeStartingWidth;
    private double _resizeStartPointerX;
    private bool _resizeWidthChanged;
    private bool _resizePreviousStarted;
    private TableViewColumn? _resizingColumn;
    private TableViewColumnHeader? _resizeTargetHeader;
    private TableViewColumnResizeMode _activeResizeMode;
    private double _reorderStartingPosition;
    private bool _reorderStarted;
    private RenderTargetBitmap? _dragVisuals;

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
        // While a resize-drag preview is active for this column, Width tracks the pointer live on
        // just this one header element (cheap), but must NOT cascade into Column.ActualWidth — that
        // would push a width change into every row's cell on every frame, which is exactly what the
        // preview mechanism (TableView.Begin/Update/EndColumnResizePreview) exists to avoid. The real
        // commit happens once, in CommitResize, when the drag ends.
        if (!double.IsNaN(Width) && Column?.IsResizing != true)
        {
            Column?.ActualWidth = Width;
        }
    }

    /// <summary>
    /// Handles the RightTapped event.
    /// </summary>
    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Check if right-click is enabled via TableView or column is currently resizing
        if (_tableView?.UseRightClickForColumnFilter != true || IsSizingCursor)
        {
            return;
        }

        // Shows the button's flyout if options button is available and filtering is enabled
        if (_optionsButton is not null && CanFilter)
        {
            _optionsButton.Flyout?.ShowAt(_optionsButton);
            e.Handled = true;
        }
    }

#if WINDOWS
    /// <summary>
    /// Gets a value indicating whether the column is currently grouped.
    /// </summary>
    private bool IsGrouped =>
        Column is not null &&
        _tableView?.CollectionView is CollectionView { } collectionView &&
        collectionView.GroupDescriptions.Any(x => x is ColumnGroupDescription columnGroup && columnGroup.Column == Column);

    /// <summary>
    /// Gets a value indicating whether this grouped column still has an independent <see cref="ColumnSortDescription"/>
    /// left over from before it was grouped - i.e. its order is still being mirrored from that description
    /// rather than owned outright by its <see cref="ColumnGroupDescription"/>. See <see cref="SortGroupDescription"/>
    /// and <see cref="HandOffToGroupDescription"/>.
    /// </summary>
    private bool HasGroupSortCompanion =>
        Column is not null &&
        _tableView?.CollectionView is CollectionView { } collectionView &&
        collectionView.SortDescriptions.Any(x => x is ColumnSortDescription columnSort && columnSort.Column == Column);

    /// <summary>
    /// Groups the column, or removes its grouping if it's already grouped.
    /// </summary>
    private void Group()
    {
        if (!CanGroup || Column is null || _tableView is not { CollectionView: CollectionView { } collectionView })
        {
            return;
        }

        if (IsGrouped)
        {
            using var ungroupDefer = collectionView.DeferRefresh();
            collectionView.GroupDescriptions.RemoveWhere(x => x is ColumnGroupDescription columnGroup && columnGroup.Column == Column);
            collectionView.SortDescriptions.RemoveWhere(x => x is ColumnSortDescription columnSort && columnSort.Column == Column);
            Column.SortDirection = null;
            return;
        }

        var eventArgs = new TableViewGroupingEventArgs(Column);
        _tableView.OnGrouping(eventArgs);

        if (eventArgs.Handled) return;

        var boundColumn = Column as TableViewBoundColumn;

        // Prefer explicit SortMemberPath if provided, otherwise use bound column's property path
        var sortPath = Column.SortMemberPath ?? boundColumn?.PropertyPath;

        using var defer = collectionView.DeferRefresh();

        // If the column already had its own sort applied, keep that ColumnSortDescription in place and
        // seed the group with its direction, rather than discarding it and resetting to Ascending - see
        // SortGroupDescription/HandOffToGroupDescription for how the two are kept in sync afterwards.
        var direction = Column.SortDirection ?? SortDirection.Ascending;

        var groupDescription = new ColumnGroupDescription(Column, sortPath, direction);
        collectionView.GroupDescriptions.Add(groupDescription);
        Column.SortDirection = direction;
    }

    /// <summary>
    /// Changes the direction driving a grouped column's order. If the column still has an independent
    /// <see cref="ColumnSortDescription"/> left over from before it was grouped (<see cref="HasGroupSortCompanion"/>),
    /// that description is updated too, keeping it mirrored; otherwise only the <see cref="ColumnGroupDescription"/>
    /// itself changes. Either way, the group's order always needs a direction, so this is the only way to
    /// change it while grouped - there's no cycling to a cleared/unsorted state.
    /// </summary>
    private void SortGroupDescription(SD direction, CollectionView collectionView)
    {
        var groupDescription = collectionView.GroupDescriptions
            .OfType<ColumnGroupDescription>()
            .FirstOrDefault(x => x.Column == Column);

        if (groupDescription is null || groupDescription.Direction == direction) return;

        var sortDescription = collectionView.SortDescriptions
            .OfType<ColumnSortDescription>()
            .FirstOrDefault(x => x.Column == Column);

        if (sortDescription is not null)
        {
            sortDescription.Direction = direction;
        }

        groupDescription.Direction = direction;
        Column!.SortDirection = direction;
        collectionView.RefreshGrouping();
    }

    /// <summary>
    /// Removes a grouped column's leftover <see cref="ColumnSortDescription"/> from before it was grouped,
    /// once it's cycled to cleared (or "Clear Sorting" is invoked) - handing its order over fully to the
    /// <see cref="ColumnGroupDescription"/> that's been mirroring it (see <see cref="SortGroupDescription"/>).
    /// Its current direction carries over unchanged, since the group still needs one.
    /// </summary>
    private void HandOffToGroupDescription(CollectionView collectionView)
    {
        collectionView.SortDescriptions.RemoveWhere(x => x is ColumnSortDescription columnSort && columnSort.Column == Column);

        var groupDescription = collectionView.GroupDescriptions
            .OfType<ColumnGroupDescription>()
            .FirstOrDefault(x => x.Column == Column);

        if (groupDescription is not null)
        {
            Column!.SortDirection = groupDescription.Direction;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this grouped column currently orders its groups by item count
    /// rather than by key.
    /// </summary>
    private bool IsGroupSortedByCount =>
        Column is not null &&
        _tableView?.CollectionView is CollectionView { } collectionView &&
        collectionView.GroupDescriptions.OfType<ColumnGroupDescription>()
            .FirstOrDefault(x => x.Column == Column) is { SortMode: GroupSortMode.Count };

    /// <summary>
    /// Toggles a grouped column's <see cref="GroupDescription.SortMode"/> between key and item-count order.
    /// </summary>
    private void ToggleGroupSortMode(CollectionView collectionView)
    {
        var groupDescription = collectionView.GroupDescriptions
            .OfType<ColumnGroupDescription>()
            .FirstOrDefault(x => x.Column == Column);

        if (groupDescription is null) return;

        groupDescription.SortMode = groupDescription.SortMode == GroupSortMode.Count
            ? GroupSortMode.Key
            : GroupSortMode.Count;

        collectionView.RefreshGrouping();
    }
#endif

    /// <summary>
    /// Sorts the column in the specified direction.
    /// </summary>
    private void DoSort(SD? direction, bool singleSorting = true)
    {
        if (!CanSort || Column is null || _tableView is not { CollectionView: CollectionView { } collectionView })
        {
            return;
        }

        var eventArgs = new TableViewSortingEventArgs(Column);
        _tableView.OnSorting(eventArgs);

        if (eventArgs.Handled) return;

#if WINDOWS
        if (IsGrouped)
        {
            if (direction is not null)
            {
                SortGroupDescription(direction.Value, collectionView);
            }
            else
            {
                HandOffToGroupDescription(collectionView);
            }

            return;
        }
#endif

        using var defer = collectionView.DeferRefresh();
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

            // Prefer explicit SortMemberPath if provided, otherwise use bound column's property path
            var sortPath = Column.SortMemberPath ?? boundColumn?.PropertyPath;

            _tableView.SortDescriptions.Add(
                new ColumnSortDescription(Column!, sortPath, direction.Value));
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

        if (CanSort && _tableView?.CollectionView is CollectionView { } collectionView && Column is not null)
        {
            using var defer = collectionView.DeferRefresh();
            _tableView.DeselectAll();

#if WINDOWS
            if (IsGrouped)
            {
                HandOffToGroupDescription(collectionView);
                return;
            }
#endif

            Column.SortDirection = null;
            collectionView.SortDescriptions.RemoveWhere(x => x is ColumnSortDescription columnSort && columnSort.Column == Column);
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
    internal void ApplyFilter()
    {
        var shouldApplyFilter = FilterItemsControl?.ShouldApplyFilter ?? false;

        if (!shouldApplyFilter && (Column?.IsFiltered ?? false))
        {
            ClearFilter();
        }
        else if (shouldApplyFilter && _tableView is not null)
        {
            _tableView.FilterHandler.SelectedValues[Column!] = GetSelectedValues();
            _tableView.FilterHandler?.ApplyFilter(Column!);
        }
    }

    private ICollection<object?> GetSelectedValues()
    {
        var filterItems = FilterItemsControl?.FilterItems ?? [];
        var selectedValues = filterItems.Where(x => x.IsSelected).Select(x => x.Value);
        var firstItem = selectedValues.FirstOrDefault(x => x is not null);
        var firstItemType = firstItem?.GetType();

#pragma warning disable IDE0306 // Simplify collection initialization
#pragma warning disable IDE0028 // Simplify collection initialization
        return firstItemType switch
        {
            Type t when t == typeof(int) => new ObjectBackedTypedSet<int?>(selectedValues),
            Type t when t == typeof(DateTime) => new ObjectBackedTypedSet<DateTime?>(selectedValues),
            Type t when t == typeof(bool) => new ObjectBackedTypedSet<bool?>(selectedValues),
            Type t when t == typeof(long) => new ObjectBackedTypedSet<long?>(selectedValues),
            Type t when t == typeof(double) => new ObjectBackedTypedSet<double?>(selectedValues),

            _ => [.. selectedValues],
        };
#pragma warning restore IDE0028 // Simplify collection initialization
#pragma warning restore IDE0306 // Simplify collection initialization
    }

    /// <summary>
    /// Hides the options flyout.
    /// </summary>
    internal void HideFlyout()
    {
        _optionsFlyout?.Hide();
    }

    /// <inheritdoc/>
    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        if (CanSort && Column is not null && _tableView is not null && !IsSizingCursor && !_reorderStarted)
        {
            var isCtrlButtonDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) is
                CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);

            DoSort(GetNextSortDirection(), !isCtrlButtonDown);
        }

        base.OnTapped(e);
    }

    /// <summary>
    /// Computes the next sort direction for a tap/invoke on this header. A grouped column with no
    /// independent <see cref="ColumnSortDescription"/> of its own (<see cref="HasGroupSortCompanion"/>)
    /// cycles between just <see cref="SD.Ascending"/> and <see cref="SD.Descending"/> - its order is owned
    /// entirely by its group and always needs a direction, so it never lands on the cleared/unsorted state
    /// a three-state cycle's third click does. A grouped column that still has a leftover independent sort
    /// from before it was grouped keeps three-state cycling until that description is cleared (see
    /// <see cref="HandOffToGroupDescription"/>), at which point it falls into the two-state case above.
    /// </summary>
    private SD? GetNextSortDirection()
    {
#if WINDOWS
        if (IsGrouped && !HasGroupSortCompanion)
        {
            return Column?.SortDirection == SD.Ascending ? SD.Descending : SD.Ascending;
        }
#endif

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

        _optionsButton?.Tapped -= OnOptionsButtonTaped;

        FilterItemsControl?.FilterItems = null;
        FilterItemsControl?.TableView = null;
        FilterItemsControl?.ColumnHeader = null;
        FilterItemsControl = null;
        _tableView = this.FindAscendant<TableView>();
        _headerRow = this.FindAscendant<TableViewHeaderRow>();
        _optionsButton = GetTemplateChild("OptionsButton") as Button;
        _optionsFlyout = GetTemplateChild("OptionsFlyout") as TableViewFilterMenuFlyout;
        _contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as Rectangle;

        if (_tableView is null || _optionsButton is null || _optionsFlyout is null)
        {
            return;
        }

        _optionsFlyout.TableView = _tableView;
        _optionsFlyout.ColumnHeader = this;

        _optionsButton.Tapped += OnOptionsButtonTaped;

        SetOptionCommands();
        SetFilterButtonVisibility();
        EnsureGridLines();
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
        _optionsButton?.Visibility = CanFilter ? Visibility.Visible : Visibility.Collapsed;

        _contentPresenter?.Margin = CanFilter ? new Thickness(
                Padding.Left,
                Padding.Top,
                Padding.Right + 8,
                0) : Padding;
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

        // Commit any in-progress resize first, so that the double-tap can reset the width to auto without.
        CommitResize();

        if (!IsSizingCursor || _tableView is null)
        {
            return;
        }

        var position = e.GetPosition(this);

        if (position.X <= 8 && _headerRow?.GetPreviousHeader(this) is { Column: { } } header)
        {
            header.Column.Width = GridLength.Auto;
        }
        else if (Column is not null)
        {
            Column.Width = GridLength.Auto;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);

        if ((_resizeStarted || _resizePreviousStarted) && _resizingColumn is not null
            && _resizeTargetHeader is not null && _tableView is not null)
        {
            var delta = e.GetCurrentPoint(_headerRow).Position.X - _resizeStartPointerX;
            var minWidth = _resizingColumn.MinWidth ?? _tableView.MinColumnWidth;
            var maxWidth = _resizingColumn.MaxWidth ?? _tableView.MaxColumnWidth;
            var width = ClampWidth(_resizeStartingWidth + delta, minWidth, maxWidth);

            _resizeTargetHeader.Width = width;
            _resizeWidthChanged = true;

            // Explicitly re-assert the resize cursor on every move — without this, something else
            // (layout invalidation elsewhere, a hover state change) can reset it mid-drag.
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

            if (_activeResizeMode == TableViewColumnResizeMode.Preview)
            {
                _tableView.UpdateColumnResizePreview(width);
            }
            else
            {
                _tableView.UpdateColumnResizeLive(width);
            }

            return;
        }

        if (CanResize && IsCursorInRightResizeArea(e) && !_reorderStarted)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }
        else if (CanResizePrevious && IsCursorInLeftResizeArea(e) && !_reorderStarted)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        }
        else if (!_resizeStarted && !_resizePreviousStarted)
        {
            ProtectedCursor = null;
        }
    }

    /// <summary>
    /// Clamps a candidate column width to the given bounds. Internal (not private) so it's directly
    /// unit-testable as a pure function.
    /// </summary>
    internal static double ClampWidth(double width, double minWidth, double maxWidth)
    {
        if (width < minWidth)
        {
            return minWidth;
        }

        if (width > maxWidth)
        {
            return maxWidth;
        }

        return width;
    }

    /// <inheritdoc/>
    protected override async void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (IsSizingCursor && CanResize && IsCursorInRightResizeArea(e) && Column is not null && _tableView is not null)
        {
            _resizeStarted = true;
            _resizingColumn = Column;
            _resizeTargetHeader = this;
            _resizeStartingWidth = ActualWidth;
            _resizeStartPointerX = e.GetCurrentPoint(_headerRow).Position.X;
            _activeResizeMode = _tableView.ColumnResizeMode;
            BeginResize(Column);
            CapturePointer(e.Pointer);
        }
        else if (IsSizingCursor && IsCursorInLeftResizeArea(e) && _tableView is not null
            && _headerRow?.GetPreviousHeader(this) is { Column: { } } header)
        {
            _resizePreviousStarted = true;
            _resizingColumn = header.Column;
            _resizeTargetHeader = header;
            _resizeStartingWidth = header.ActualWidth;
            _resizeStartPointerX = e.GetCurrentPoint(_headerRow).Position.X;
            _activeResizeMode = _tableView.ColumnResizeMode;
            BeginResize(header.Column);
            CapturePointer(e.Pointer);
        }
        else if (_tableView?.CanReorderColumns is true && Column?.CanReorder is true)
        {
            var position = e.GetCurrentPoint(_headerRow).Position;
            _reorderStartingPosition = position.X;
            _reorderStarted = true;
            _dragVisuals = await CreateDragVisualsAsync();
            CapturePointer(e.Pointer);
        }
    }

    /// <inheritdoc/>
    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (_reorderStarted && _dragVisuals is not null)
        {
            var position = _reorderStartingPosition + e.Cumulative.Translation.X;
            _headerRow?.ShowColumnDropIndicator(position, _dragVisuals);
        }
    }

    private async Task<RenderTargetBitmap> CreateDragVisualsAsync()
    {
        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(this);
        return rtb;
    }

    /// <inheritdoc/>
    protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
    {
        base.OnManipulationCompleted(e);
        CommitResize();
        CompleteColumnDrop(true);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        ReleasePointerCaptures();

        _reorderStarted = false;
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        CommitResize();
        CompleteColumnDrop(false);
    }

    /// <summary>
    /// Starts a resize-drag on <paramref name="column"/>, using whichever mode was captured into
    /// <see cref="_activeResizeMode"/> at the start of this gesture.
    /// </summary>
    private void BeginResize(TableViewColumn column)
    {
        if (_tableView is null)
        {
            return;
        }

        if (_activeResizeMode == TableViewColumnResizeMode.Preview)
        {
            _tableView.BeginColumnResizePreview(column);
        }
        else
        {
            _tableView.BeginColumnResizeLive(column);
        }
    }

    /// <summary>
    /// Ends an in-progress resize drag, synchronously: ends the active resize mode (which, if the
    /// width actually changed, performs the single real width commit) and resets gesture state. Safe
    /// to call more than once per drag (both <see cref="OnManipulationCompleted"/> and
    /// <see cref="OnPointerCaptureLost"/> call it, in case a manipulation gesture never started) —
    /// it no-ops if no resize is in progress.
    /// </summary>
    private void CommitResize()
    {
        if (!_resizeStarted && !_resizePreviousStarted)
        {
            return;
        }

        var finalWidth = _resizeWidthChanged ? _resizeTargetHeader?.Width : null;

        if (_activeResizeMode == TableViewColumnResizeMode.Preview)
        {
            _tableView?.EndColumnResizePreview(finalWidth);
        }
        else
        {
            _tableView?.EndColumnResizeLive(finalWidth);
        }

        _resizeStarted = false;
        _resizePreviousStarted = false;
        _resizeWidthChanged = false;
        _resizingColumn = null;
        _resizeTargetHeader = null;
    }

    private void CompleteColumnDrop(bool applyDrop)
    {
        if (_reorderStarted && Column is not null)
        {
            _headerRow?.ColumnDropCompleted(Column, applyDrop);
        }

        _reorderStarted = false;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Column is not null && _tableView is not null && !Column.IsResizing)
        {
            var autoWidthMode = Column.ColumnAutoWidthMode ?? _tableView.ColumnAutoWidthMode;
            if (autoWidthMode is TableViewColumnAutoWidthMode.Header or TableViewColumnAutoWidthMode.Both)
            {
                var desiredHeaderSize = base.MeasureOverride(new Size(double.PositiveInfinity, double.PositiveInfinity));
                CachedDesiredWidth = desiredHeaderSize.Width;
                Column.DesiredWidth = Math.Max(Column.DesiredWidth, desiredHeaderSize.Width);
            }
        }

        return base.MeasureOverride(availableSize);
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
    /// Caches this header's own natural (unconstrained) desired width, last computed in
    /// <see cref="MeasureOverride"/>.
    /// </summary>
    internal double? CachedDesiredWidth { get; private set; }

    /// <summary>
    /// Gets or sets the column associated with the header.
    /// </summary>
    public TableViewColumn? Column { get; internal set; }

#if WINDOWS
    /// <summary>
    /// Gets a value indicating whether the column can be grouped.
    /// </summary>
    private bool CanGroup => _tableView?.CanGroupColumns == true && Column?.CanGroup == true;
#endif

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

    /// <summary>
    /// Gets or sets the filter items control associated with the column header.
    /// </summary>
    internal TableViewFilterItemsControl? FilterItemsControl { get; set; }

    /// <summary>
    /// Cycles through sort directions (ascending → descending → unsorted) for automation support.
    /// </summary>
    internal void InvokeSortCycle()
    {
        if (CanSort && Column is not null)
        {
            DoSort(GetNextSortDirection());
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AutomationPeers.TableViewColumnHeaderAutomationPeer(this);
    }
}
